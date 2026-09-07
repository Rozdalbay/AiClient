using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AiDesktopClient.Controls;

// доморощенный markdown-рендер в FlowDocument: заголовки, списки, таблицы-на редаче, блоки кода с номерами строк - без внешних либ, чистая на коленке самописная хрень
public partial class MarkdownRenderer : UserControl
{
    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.Register(nameof(Markdown), typeof(string), typeof(MarkdownRenderer),
            new PropertyMetadata(string.Empty, OnMarkdownChanged));

    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    private static readonly FontFamily MonoFont = new("Cascadia Code, JetBrains Mono, Consolas");

    private SolidColorBrush PrimaryText = null!;
    private SolidColorBrush SecondaryText = null!;
    private SolidColorBrush AccentColor = null!;
    private SolidColorBrush LinkColor = null!;
    private SolidColorBrush BorderColor = null!;
    private SolidColorBrush CodeBg = null!;

    public MarkdownRenderer()
    {
        InitializeComponent();
        LoadThemeBrushes();
    }

    private void LoadThemeBrushes()
    {
        PrimaryText = (Application.Current.FindResource("PrimaryTextBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E8F0"));
        SecondaryText = (Application.Current.FindResource("SecondaryTextBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B8D9E"));
        AccentColor = (Application.Current.FindResource("PrimaryAccentBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C5CFC"));
        LinkColor = (Application.Current.FindResource("SecondaryAccentBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5CA0FC"));
        BorderColor = (Application.Current.FindResource("BorderBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2D3E"));
        CodeBg = (Application.Current.FindResource("InputBackgroundBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1D2E"));
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownRenderer renderer && e.NewValue is string md)
            renderer.RenderMarkdown(md);
    }

    private void RenderMarkdown(string markdown)
    {
        Document.Blocks.Clear();

        if (string.IsNullOrWhiteSpace(markdown))
            return;

        var paragraph = new Paragraph
        {
            Foreground = PrimaryText,
            FontFamily = new FontFamily("Segoe UI, SF Pro Display, Inter"),
            FontSize = 13,
            LineHeight = 22
        };

        var lines = markdown.Split('\n');
        bool inCodeBlock = false;
        string codeLanguage = "";
        var codeLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("```"))
            {
                if (inCodeBlock)
                {
                    AddCodeBlock(Document, string.Join("\n", codeLines), codeLanguage);
                    codeLines.Clear();
                    inCodeBlock = false;
                }
                else
                {
                    codeLanguage = trimmed.TrimStart('`').Trim();
                    inCodeBlock = true;
                }
                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(line);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                Document.Blocks.Add(paragraph);
                paragraph = new Paragraph { Foreground = PrimaryText, FontSize = 13, LineHeight = 22 };
                continue;
            }

            if (trimmed.StartsWith("### "))
            {
                Document.Blocks.Add(paragraph);
                paragraph = new Paragraph { Foreground = PrimaryText, FontSize = 15, FontWeight = FontWeights.SemiBold, LineHeight = 26 };
                AddInlineFormatted(paragraph, trimmed[4..]);
                Document.Blocks.Add(paragraph);
                paragraph = new Paragraph { Foreground = PrimaryText, FontSize = 13, LineHeight = 22 };
                continue;
            }
            if (trimmed.StartsWith("## "))
            {
                Document.Blocks.Add(paragraph);
                paragraph = new Paragraph { Foreground = PrimaryText, FontSize = 17, FontWeight = FontWeights.Bold, LineHeight = 28 };
                AddInlineFormatted(paragraph, trimmed[3..]);
                Document.Blocks.Add(paragraph);
                paragraph = new Paragraph { Foreground = PrimaryText, FontSize = 13, LineHeight = 22 };
                continue;
            }
            if (trimmed.StartsWith("# "))
            {
                Document.Blocks.Add(paragraph);
                paragraph = new Paragraph { Foreground = PrimaryText, FontSize = 20, FontWeight = FontWeights.Bold, LineHeight = 30 };
                AddInlineFormatted(paragraph, trimmed[2..]);
                Document.Blocks.Add(paragraph);
                paragraph = new Paragraph { Foreground = PrimaryText, FontSize = 13, LineHeight = 22 };
                continue;
            }

            if (trimmed.StartsWith("> "))
            {
                var bq = new Paragraph
                {
                    Foreground = SecondaryText,
                    FontSize = 13,
                    FontStyle = FontStyles.Italic,
                    LineHeight = 22,
                    Padding = new Thickness(12, 4, 0, 4),
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    BorderBrush = AccentColor,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                AddInlineFormatted(bq, trimmed[2..]);
                Document.Blocks.Add(bq);
                continue;
            }

            if (trimmed.StartsWith("---") || trimmed.StartsWith("***"))
            {
                Document.Blocks.Add(new Paragraph
                {
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    BorderBrush = BorderColor,
                    Margin = new Thickness(0, 8, 0, 8)
                });
                continue;
            }

            if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
            {
                var content = Regex.Replace(trimmed, @"^\d+\.\s", "");
                var number = Regex.Match(trimmed, @"^(\d+)\.").Groups[1].Value;
                var li = new Paragraph
                {
                    Foreground = PrimaryText,
                    FontSize = 13,
                    LineHeight = 22,
                    Padding = new Thickness(20, 2, 0, 2),
                    Margin = new Thickness(0)
                };
                li.Inlines.Add(new Run($"{number}. ") { Foreground = AccentColor, FontWeight = FontWeights.SemiBold });
                AddInlineFormatted(li, content);
                Document.Blocks.Add(li);
                continue;
            }

            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                var li = new Paragraph
                {
                    Foreground = PrimaryText,
                    FontSize = 13,
                    LineHeight = 22,
                    Padding = new Thickness(20, 2, 0, 2),
                    Margin = new Thickness(0)
                };
                li.Inlines.Add(new Run("• ") { Foreground = AccentColor });
                AddInlineFormatted(li, trimmed[2..]);
                Document.Blocks.Add(li);
                continue;
            }

            if (trimmed.StartsWith("| ") && trimmed.Contains("|"))
            {
                var cells = trimmed.Split('|', StringSplitOptions.RemoveEmptyEntries);
                var rowText = string.Join("  │  ", cells.Select(c => c.Trim()));
                var row = new Paragraph
                {
                    Foreground = SecondaryText,
                    FontFamily = MonoFont,
                    FontSize = 12,
                    LineHeight = 20,
                    Padding = new Thickness(8, 1, 0, 1),
                    Margin = new Thickness(0)
                };
                row.Inlines.Add(new Run($"  {rowText}"));
                Document.Blocks.Add(row);
                continue;
            }

            AddInlineFormatted(paragraph, line);
            paragraph.Inlines.Add(new LineBreak());
        }

        if (paragraph.Inlines.Count > 0)
            Document.Blocks.Add(paragraph);
    }

    private void AddInlineFormatted(Paragraph paragraph, string text)
    {
        var parts = Regex.Split(text, @"(\*\*.*?\*\*|\*.*?\*|`[^`]+`|\[.*?\]\(.*?\))");

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            if (part.StartsWith("**") && part.EndsWith("**"))
            {
                paragraph.Inlines.Add(new Run(part[2..^2])
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = PrimaryText
                });
            }
            else if (part.StartsWith("*") && part.EndsWith("*") && !part.StartsWith("**"))
            {
                paragraph.Inlines.Add(new Run(part[1..^1])
                {
                    FontStyle = FontStyles.Italic,
                    Foreground = SecondaryText
                });
            }
            else if (part.StartsWith("`") && part.EndsWith("`"))
            {
                paragraph.Inlines.Add(new Run(part[1..^1])
                {
                    FontFamily = MonoFont,
                    FontSize = 12,
                    Foreground = AccentColor,
                    Background = CodeBg
                });
            }
            else if (part.StartsWith("[") && part.Contains("]("))
            {
                var match = Regex.Match(part, @"\[(.*?)\]\((.*?)\)");
                if (match.Success)
                {
                    paragraph.Inlines.Add(new Run(match.Groups[1].Value)
                    {
                        Foreground = LinkColor,
                        TextDecorations = TextDecorations.Underline
                    });
                }
            }
            else
            {
                paragraph.Inlines.Add(new Run(part) { Foreground = PrimaryText });
            }
        }
    }

    private void AddCodeBlock(FlowDocument document, string code, string language)
    {
        var codeHeaderBg = (Application.Current.FindResource("CodeHeaderBackgroundBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151825"));
        var codeBodyBg = (Application.Current.FindResource("CodeBackgroundBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F1117"));
        var lineNumBrush = (Application.Current.FindResource("LineNumberBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3D4E"));

        var headerPara = new Paragraph
        {
            Foreground = SecondaryText,
            FontSize = 11,
            Padding = new Thickness(12, 6, 12, 2),
            Margin = new Thickness(0, 8, 0, 0),
            Background = codeHeaderBg
        };
        headerPara.Inlines.Add(new Run($"  {language}  ") { FontSize = 11, Foreground = SecondaryText });
        document.Blocks.Add(headerPara);

        var codeLines = code.Split('\n');
        for (int i = 0; i < codeLines.Length; i++)
        {
            var codePara = new Paragraph
            {
                Foreground = PrimaryText,
                FontFamily = MonoFont,
                FontSize = 12,
                LineHeight = 20,
                Padding = new Thickness(12, 1, 12, 1),
                Margin = new Thickness(0),
                Background = codeBodyBg
            };
            codePara.Inlines.Add(new Run($"  {(i + 1).ToString().PadLeft(3)} │ ")
            {
                Foreground = lineNumBrush,
                FontFamily = MonoFont,
                FontSize = 12
            });
            codePara.Inlines.Add(new Run(codeLines[i])
            {
                Foreground = PrimaryText,
                FontFamily = MonoFont,
                FontSize = 12
            });
            document.Blocks.Add(codePara);
        }
    }
}
