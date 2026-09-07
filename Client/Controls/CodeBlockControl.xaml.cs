using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using AiDesktopClient.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiDesktopClient.Controls;

// блок кода в сообщении: нумерация строк, кнопки copy/save; расширение файла выводится по языку, без сео
public partial class CodeBlockControl : UserControl
{
    public static readonly DependencyProperty CodeProperty =
        DependencyProperty.Register(nameof(Code), typeof(string), typeof(CodeBlockControl),
            new PropertyMetadata(string.Empty, OnCodeChanged));

    public static readonly DependencyProperty CodeLanguageProperty =
        DependencyProperty.Register(nameof(CodeLanguage), typeof(string), typeof(CodeBlockControl),
            new PropertyMetadata("code", OnLanguageChanged));

    private readonly IToastService _toastService;

    public string Code
    {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public string CodeLanguage
    {
        get => (string)GetValue(CodeLanguageProperty);
        set => SetValue(CodeLanguageProperty, value);
    }

    public CodeBlockControl()
    {
        InitializeComponent();
        _toastService = App.ServiceProvider!.GetRequiredService<IToastService>();
    }

    private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CodeBlockControl control && e.NewValue is string code)
            control.UpdateCodeDisplay(code);
    }

    private static void OnLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CodeBlockControl control && e.NewValue is string lang)
            control.LanguageText.Text = lang;
    }

    private void UpdateCodeDisplay(string code)
    {
        var lines = code.Split('\n');
        var lineNumbers = new ObservableCollection<string>();

        for (int i = 1; i <= lines.Length; i++)
            lineNumbers.Add(i.ToString());

        LineNumbersControl.ItemsSource = lineNumbers;
        CodeContent.Text = code;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(Code);
        _toastService.ShowSuccess("Code copied");
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = $"All files (*{GetExtensionForLanguage(CodeLanguage)})|*{GetExtensionForLanguage(CodeLanguage)}",
                DefaultExt = GetExtensionForLanguage(CodeLanguage)
            };

            if (dialog.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dialog.FileName, Code);
                _toastService.ShowSuccess("File saved");
            }
        }
        catch (Exception)
        {
            _toastService.ShowError("Failed to save file");
        }
    }

    private static string GetExtensionForLanguage(string language) => language.ToLowerInvariant() switch
    {
        "csharp" or "c#" => ".cs",
        "javascript" or "js" => ".js",
        "typescript" or "ts" => ".ts",
        "python" or "py" => ".py",
        "java" => ".java",
        "cpp" or "c++" or "c" => ".cpp",
        "html" => ".html",
        "css" => ".css",
        "json" => ".json",
        "xml" => ".xml",
        "sql" => ".sql",
        "rust" or "rs" => ".rs",
        "go" => ".go",
        "ruby" or "rb" => ".rb",
        "php" => ".php",
        "swift" => ".swift",
        "kotlin" or "kt" => ".kt",
        "bash" or "sh" => ".sh",
        "powershell" or "ps1" => ".ps1",
        "yaml" or "yml" => ".yaml",
        "markdown" or "md" => ".md",
        _ => ".txt"
    };
}
