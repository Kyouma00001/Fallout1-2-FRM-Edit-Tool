using System.Text;

namespace Fallout.Tools.Core.Imaging;

public static class UiTextLayoutParser
{
    public static IReadOnlyList<UiTextPlacement> ParseFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return ParseLines(File.ReadLines(path, Encoding.UTF8));
    }

    public static IReadOnlyList<UiTextPlacement> ParseLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        List<UiTextPlacement> placements = new();
        int lineNumber = 0;

        foreach (string rawLine in lines)
        {
            lineNumber++;

            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            placements.Add(ParseLine(line, lineNumber));
        }

        return placements;
    }

    private static UiTextPlacement ParseLine(string line, int lineNumber)
    {
        string[] parts = line.Split('|', 6, StringSplitOptions.None);

        return parts.Length switch
        {
            4 => ParseSimple(parts, lineNumber),
            6 => ParseAligned(parts, lineNumber),
            _ => throw new ArgumentException(
                $"Invalid UI layout line {lineNumber}. Use NAME|X|Y|TEXT or NAME|X|Y|WIDTH|ALIGN|TEXT.")
        };
    }

    private static UiTextPlacement ParseSimple(string[] parts, int lineNumber)
    {
        string name = ReadRequired(parts[0], lineNumber, "name");
        int x = ReadInt(parts[1], lineNumber, "x");
        int y = ReadInt(parts[2], lineNumber, "y");
        string text = ReadRequired(parts[3], lineNumber, "text");

        return new UiTextPlacement(name, x, y, 0, UiTextAlignment.Left, text);
    }

    private static UiTextPlacement ParseAligned(string[] parts, int lineNumber)
    {
        string name = ReadRequired(parts[0], lineNumber, "name");
        int x = ReadInt(parts[1], lineNumber, "x");
        int y = ReadInt(parts[2], lineNumber, "y");
        int width = ReadInt(parts[3], lineNumber, "width");
        UiTextAlignment alignment = ReadAlignment(parts[4], lineNumber);
        string text = ReadRequired(parts[5], lineNumber, "text");

        if (width < 0)
        {
            throw new ArgumentException($"Invalid UI layout line {lineNumber}: width cannot be negative.");
        }

        return new UiTextPlacement(name, x, y, width, alignment, text);
    }

    private static string ReadRequired(string value, int lineNumber, string fieldName)
    {
        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException($"Invalid UI layout line {lineNumber}: {fieldName} is empty.");
        }

        return trimmed;
    }

    private static int ReadInt(string value, int lineNumber, string fieldName)
    {
        if (!int.TryParse(value.Trim(), out int result))
        {
            throw new ArgumentException($"Invalid UI layout line {lineNumber}: {fieldName} must be an integer.");
        }

        return result;
    }

    private static UiTextAlignment ReadAlignment(string value, int lineNumber)
    {
        string normalized = value.Trim();
        if (!Enum.TryParse(normalized, ignoreCase: true, out UiTextAlignment alignment))
        {
            throw new ArgumentException(
                $"Invalid UI layout line {lineNumber}: alignment must be left, center, or right.");
        }

        return alignment;
    }
}
