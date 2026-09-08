using System.Text;

namespace AiDesktopClient.Services;

public sealed class SseEventParser
{
    private readonly StringBuilder _buffer = new();

    public IEnumerable<string> Append(ReadOnlySpan<char> chunk)
    {
        _buffer.Append(chunk);
        return DrainCompletedEvents();
    }

    public IEnumerable<string> Complete()
    {
        return DrainCompletedEvents();
    }

    private IEnumerable<string> DrainCompletedEvents()
    {
        while (true)
        {
            var normalized = _buffer.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
            var separatorIndex = normalized.IndexOf("\n\n", StringComparison.Ordinal);
            if (separatorIndex < 0)
                yield break;

            var eventText = normalized[..separatorIndex];
            _buffer.Clear();
            _buffer.Append(normalized[(separatorIndex + 2)..]);

            var dataLines = eventText
                .Split('\n')
                .Where(line => line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                .Select(line => line.Length == 5 ? string.Empty : line[5..].TrimStart())
                .ToArray();

            if (dataLines.Length > 0)
                yield return string.Join("\n", dataLines);
        }
    }
}
