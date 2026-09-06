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

    static void TestCardsAndStreaming()
    {
        var service = new ControlledChatService();
        var main = new MainViewModel(service, new MockModelService(), new MockUsageService(), new MockBackendService(), new ToastService());
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
        Check("Markdown visible", Renderer(assistant).Visibility == Visibility.Visible);
        Check("Debug overlay removed", assistant.FindName("DebugText") is null && view.FindName("DebugCountText") is null);
        Check("Actions hidden while streaming", VisibilityOf(assistant, "ActionButtons") == Visibility.Collapsed);

        service.Chunks.Writer.TryWrite("Привет ");
        WaitUntil(() => assistant.Message!.Content == "Привет ", view);
        Check("First chunk rendered before response ends", Renderer(assistant).Markdown == "Привет " && !send.IsCompleted);
        service.Chunks.Writer.TryWrite("**мир**!");
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
        service.Chunks.Writer.TryWrite("Partial text");
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
        Check("Actual SSE parser decodes JSON, Cyrillic, comments and DONE", collect.GetAwaiter().GetResult().SequenceEqual(new[] { "Привет ", "world!" }));
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
        await foreach (var chunk in service.StreamResponseAsync("test", "model-a", "GUI regression test")) chunks.Add(chunk);
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
        public Channel<string> Chunks { get; private set; } = Channel.CreateUnbounded<string>();
        public void Reset() => Chunks = Channel.CreateUnbounded<string>();
        public IAsyncEnumerable<string> StreamResponseAsync(string chatId, string modelId, string message, IReadOnlyList<Attachment>? attachments = null, CancellationToken cancellationToken = default) => Chunks.Reader.ReadAllAsync(cancellationToken);
        public Task<AiDesktopClient.Services.ChatResponse> SendMessageAsync(string chatId, string modelId, string message, IReadOnlyList<Attachment>? attachments = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    sealed class SseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Check("SSE uses POST /stream", request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/stream");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(": keepalive\n\ndata: {\"chunk\":\"Привет \"}\n\ndata: {\"chunk\":\"world!\"}\n\ndata: [DONE]\n\ndata: {\"chunk\":\"ignored\"}\n\n", Encoding.UTF8, "text/event-stream")
            });
        }
    }
}
