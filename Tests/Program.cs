using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using AiDesktopClient;
using AiDesktopClient.Controls;
using AiDesktopClient.Contracts;
using AiDesktopClient.Models;
using AiDesktopClient.Services;
using AiDesktopClient.ViewModels;
using AiDesktopClient.Views;

internal static class Program
{
    private static int checks;

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            var app = new App();
            app.InitializeComponent();
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            TestSseEventParser();
            TestMarkdownStreamingBoundary();
            TestBackendConnectionFlow();
            TestCardsAndStreaming();
            TestSseProtocol();
            if (args.Contains("--live")) TestLiveBackend();
            Console.WriteLine($"PASS: {checks} assertions");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    static void TestBackendConnectionFlow()
    {
        using var http = new HttpClient(new ConnectionHandler())
        {
            BaseAddress = new Uri("http://old-host/")
        };
        var service = new BackendHealthService(http);

        Check("Health check uses root endpoint", service.GetStatusAsync().GetAwaiter().GetResult().Status == BackendStatus.Connected);
        Check("Test Connection uses same client and endpoint", service.TestConnectionAsync("http://localhost:5000").GetAwaiter().GetResult());
        Check("Test Connection preserves shared client configuration", http.BaseAddress == new Uri("http://old-host/"));
    }

    static void TestSseEventParser()
    {
        var parser = new SseEventParser();
        var events = new List<string>();
        events.AddRange(parser.Append("data: {\"choices\":[{\"delta\":{\"cont"));
        events.AddRange(parser.Append("ent\":\"hello\"}}]}\r\n\r\n"));
        events.AddRange(parser.Append("data: {}\n\ndata: [DONE]\n\n"));

        Check("SSE event survives split network chunks", events[0].Contains("hello"));
        Check("SSE JSON without content is preserved", events[1] == "{}");
        Check("SSE DONE is parsed", events[2] == "[DONE]");

        var emptyParser = new SseEventParser();
        var emptyEvents = emptyParser.Append("\r\n\r\ndata: one\r\n\r\n").ToList();
        Check("Empty SSE event does not corrupt following event", emptyEvents.Single() == "one");

        var incompleteParser = new SseEventParser();
        Check("Incomplete SSE event waits for delimiter", !incompleteParser.Append("data: incomplete").Any() && !incompleteParser.Complete().Any());
    }

    static void TestMarkdownStreamingBoundary()
    {
        var renderer = new MarkdownRenderer { Markdown = "**" };
        Check("Incomplete bold markdown does not throw", renderer.FindName("Document") is not null);
    }

    static void TestCardsAndStreaming()
    {
        var service = new ControlledChatService();
        var main = new MainViewModel(service, new MockModelService(), new MockUsageService(), new MockBackendService(), new ToastService(), new AuthService());
        var vm = main.CurrentChatViewModel;
        var view = new ChatView { DataContext = vm, Width = 900, Height = 650 };
        view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        vm.InputMessage = "Hello from user";
        var send = vm.SendMessageCommand.ExecuteAsync(null);
        Layout(view);
        var cards = Descendants<ChatMessageControl>(view).ToList();
        Check("Two cards receive real messages", cards.Count == 2 && cards.All(c => c.Message is not null));
        var user = cards.Single(c => c.Message!.Role == MessageRole.User);
        var assistant = cards.Single(c => c.Message!.Role == MessageRole.Assistant);
        Check("User text rendered", Renderer(user).Markdown == "Hello from user");
        Check("Role rendered", ((TextBlock)user.FindName("RoleText")).Text == "You");
        Check("ViewModel retained for actions", ReferenceEquals(assistant.DataContext, vm));
        Check("Markdown hidden while waiting for first chunk", Renderer(assistant).Visibility == Visibility.Collapsed);
        Check("Debug overlay removed", assistant.FindName("DebugText") is null && view.FindName("DebugCountText") is null);
        Check("Actions hidden while streaming", VisibilityOf(assistant, "ActionButtons") == Visibility.Collapsed);
        Check("Compact generating indicator visible", VisibilityOf(assistant, "GeneratingPlaceholder") == Visibility.Visible);

        service.Chunks.Writer.TryWrite(new StreamChunk { Text = "Привет " });
        WaitUntil(() => assistant.Message!.Content == "Привет ", view);
        Check("First chunk rendered before response ends", Renderer(assistant).Markdown == "Привет " && !send.IsCompleted);
        Check("Generating indicator hides after first chunk", VisibilityOf(assistant, "GeneratingPlaceholder") == Visibility.Collapsed);
        service.Chunks.Writer.TryWrite(new StreamChunk { Text = "**мир**!" });
        WaitUntil(() => assistant.Message!.Content.EndsWith("**мир**!"), view);
        Check("Second chunk rendered on same card", ReferenceEquals(assistant, Descendants<ChatMessageControl>(view).Last()) && Renderer(assistant).Markdown == "Привет **мир**!");
        var document = (FlowDocument)Renderer(assistant).FindName("Document");
        Check("Markdown produces visible document text", new TextRange(document.ContentStart, document.ContentEnd).Text.Contains("Привет мир!"));
        service.Chunks.Writer.TryComplete();
        WaitUntil(() => send.IsCompleted, view);
        send.GetAwaiter().GetResult();
        Check("Completion restores send state", !vm.IsStreaming && vm.CanSend);
        Check("Completion reveals actions", VisibilityOf(assistant, "ActionButtons") == Visibility.Visible);
        Check("Completion updates token text", ((TextBlock)assistant.FindName("TokensText")).Text.Contains(assistant.Message!.TotalTokens.ToString()));
        Check("Completion updates cost and time", VisibilityOf(assistant, "CostBorder") == Visibility.Visible && VisibilityOf(assistant, "TimeBorder") == Visibility.Visible);

        var original = vm.CurrentChat!;
        var second = new Chat { Title = "Second", ModelId = "model-a" };
        vm.LoadChat(second);
        Layout(view);
        Check("Switch removes previous cards", !Descendants<ChatMessageControl>(view).Any());
        Check("Subscription follows selected chat", ReferenceEquals(Subscription(view), second.Messages));
        second.Messages.Add(new ChatMessage { Role = MessageRole.User, Content = "Second chat message" });
        Layout(view);
        Check("New chat additions displayed", Renderer(Descendants<ChatMessageControl>(view).Single()).Markdown == "Second chat message");
        vm.LoadChat(original);
        Layout(view);
        Check("Returning to chat restores answer", Renderer(Descendants<ChatMessageControl>(view).Last()).Markdown == "Привет **мир**!");
        view.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Check("Unloading detaches collection", Subscription(view) is null);
        view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        Check("Reloading reattaches collection", ReferenceEquals(Subscription(view), original.Messages));

        service.Reset();
        vm.InputMessage = "Cancel test";
        var cancel = vm.SendMessageCommand.ExecuteAsync(null);
        Layout(view);
        var canceledCard = Descendants<ChatMessageControl>(view).Last();
        service.Chunks.Writer.TryWrite(new StreamChunk { Text = "Partial text" });
        WaitUntil(() => canceledCard.Message!.Content == "Partial text", view);
        vm.StopGenerationCommand.Execute(null);
        WaitUntil(() => cancel.IsCompleted, view);
        cancel.GetAwaiter().GetResult();
        Check("Cancel renders partial text and stop marker", Renderer(canceledCard).Markdown.Contains("Partial text") && Renderer(canceledCard).Markdown.Contains("Generation stopped"));
        Check("Cancel restores actions", !vm.IsStreaming && VisibilityOf(canceledCard, "ActionButtons") == Visibility.Visible);

        service.Reset();
        vm.InputMessage = "Error test";
        var error = vm.SendMessageCommand.ExecuteAsync(null);
        Layout(view);
        service.Chunks.Writer.TryComplete(new HttpRequestException("Test failure"));
        WaitUntil(() => error.IsCompleted, view);
        error.GetAwaiter().GetResult();
        var errorCard = Descendants<ChatMessageControl>(view).Last();
        Check("Error text displayed", errorCard.Message!.IsError && Renderer(errorCard).Markdown.Contains("An error occurred"));
        Check("Generating indicator hides on error", VisibilityOf(errorCard, "GeneratingPlaceholder") == Visibility.Collapsed);

        var oldMessage = errorCard.Message;
        errorCard.Message = new ChatMessage { Role = MessageRole.User, Content = "Replacement" };
        oldMessage.Content = "Must not leak into replacement";
        Check("Replacement unsubscribes old message", Renderer(errorCard).Markdown == "Replacement" && VisibilityOf(errorCard, "ActionButtons") == Visibility.Collapsed);
        errorCard.Message.Attachments.Add(new Attachment { FileName = "test.txt" });
        Check("Attachment additions shown", VisibilityOf(errorCard, "AttachmentsList") == Visibility.Visible);
        errorCard.Message.Attachments.Clear();
        Check("Empty attachments hidden", VisibilityOf(errorCard, "AttachmentsList") == Visibility.Collapsed);
        errorCard.Message = null;
        Check("Null message hides and clears card", VisibilityOf(errorCard, "MessageBorder") == Visibility.Collapsed && Renderer(errorCard).Markdown == "");

        vm.LoadChat(second);
        var longMessage = new ChatMessage { Role = MessageRole.Assistant, Content = string.Join("\n", Enumerable.Repeat("Long answer line", 80)) };
        second.Messages.Add(longMessage);
        Layout(view);
        Layout(view);
        var scroll = (ScrollViewer)view.FindName("MessagesScroll");
        Check("Long answers are scrollable", scroll.ScrollableHeight > 0);
        Check("New messages scroll to bottom", Math.Abs(scroll.VerticalOffset - scroll.ScrollableHeight) < 2);
        longMessage.Content += "\nAnother streamed line";
        Layout(view);
        Layout(view);
        Check("Streaming follows growing answer", Math.Abs(scroll.VerticalOffset - scroll.ScrollableHeight) < 2);
        scroll.ScrollToTop();
        Layout(view);
        longMessage.Content += "\nDo not interrupt reading above";
        Layout(view);
        Layout(view);
        Check("Manual upward scroll is respected", scroll.VerticalOffset < 2);
        view.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
    }

    static void TestSseProtocol()
    {
        using var http = new HttpClient(new SseHandler()) { BaseAddress = new Uri("http://localhost/") };
        var collect = Collect(new SseChatService(http));
        WaitUntil(() => collect.IsCompleted);
        var chunks = collect.GetAwaiter().GetResult();
            Check("Actual SSE parser decodes delta, OpenAI choices content and DONE", chunks.SequenceEqual(new[] { "Привет ", "world!" }));
    }

    static void TestLiveBackend()
    {
        using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000/"), Timeout = TimeSpan.FromSeconds(10) };
        var collect = Collect(new SseChatService(http));
        WaitUntil(() => collect.IsCompleted);
        var chunks = collect.GetAwaiter().GetResult();
        Check("Local backend sends multiple nonempty chunks", chunks.Count >= 2 && string.Concat(chunks).Length > 0);
        Console.WriteLine("Live backend answer: " + string.Concat(chunks));
    }

    static async Task<List<string>> Collect(IChatService service)
    {
        var chunks = new List<string>();
        await foreach (var chunk in service.StreamResponseAsync("test", "model-a", "GUI regression test"))
        {
            if (chunk.Text is not null)
                chunks.Add(chunk.Text);
        }
        return chunks;
    }
    static object? Subscription(ChatView view) => typeof(ChatView).GetField("_subscribedMessages", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(view);
    static MarkdownRenderer Renderer(ChatMessageControl c) => (MarkdownRenderer)c.FindName("MarkdownContent");
    static Visibility VisibilityOf(ChatMessageControl c, string name) => ((FrameworkElement)c.FindName(name)).Visibility;
    static void Check(string name, bool ok) { if (!ok) throw new Exception("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
    static void WaitUntil(Func<bool> predicate, FrameworkElement? view = null)
    {
        var timer = Stopwatch.StartNew();
        while (!predicate()) { if (timer.Elapsed.TotalSeconds > 12) throw new TimeoutException("Test timed out"); Layout(view); Thread.Sleep(5); }
        Layout(view);
    }
    static void Layout(FrameworkElement? view)
    {
        if (view is not null) { view.Measure(new Size(900, 650)); view.Arrange(new Rect(0, 0, 900, 650)); view.UpdateLayout(); }
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    sealed class ControlledChatService : IChatService
    {
        public Channel<StreamChunk> Chunks { get; private set; } = Channel.CreateUnbounded<StreamChunk>();
        public void Reset() => Chunks = Channel.CreateUnbounded<StreamChunk>();
        public IAsyncEnumerable<StreamChunk> StreamResponseAsync(string chatId, string modelId, string message, IReadOnlyList<Attachment>? attachments = null, CancellationToken cancellationToken = default) => Chunks.Reader.ReadAllAsync(cancellationToken);
        public Task<AiDesktopClient.Services.ChatResponse> SendMessageAsync(string chatId, string modelId, string message, IReadOnlyList<Attachment>? attachments = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    sealed class SseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Check("SSE uses POST /stream", request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/stream");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(": keepalive\n\ndata: {\"delta\":\"Привет \"}\n\ndata: {\"choices\":[{\"delta\":{\"content\":\"world!\"}}]}\n\ndata: {\"choices\":[{\"delta\":{\"content\":null},\"finish_reason\":\"stop\"}],\"usage\":{\"input_tokens\":3,\"output_tokens\":2}}\n\ndata: [DONE]\n\ndata: {\"delta\":\"ignored\"}\n\n", Encoding.UTF8, "text/event-stream")
            });
        }
    }

    sealed class ConnectionHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Check("Connection check uses GET /", request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
