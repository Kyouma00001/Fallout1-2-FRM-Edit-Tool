using Fallout.Tools.Core.AAF;
using Fallout.Tools.Core.Imaging;

namespace Fallout.Tools.CLI;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                PrintHelp();
                return 0;
            }

            string command = args[0].ToLowerInvariant();
            return command switch
            {
                "info" => RunInfo(args.Skip(1).ToArray()),
                "export" => RunExport(args.Skip(1).ToArray()),
                _ => Fail($"Unknown command: {args[0]}")
            };
        }
        catch (AafException ex)
        {
            Console.Error.WriteLine("AAF error: " + ex.Message);
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
            return 1;
        }
    }

    private static int RunInfo(string[] args)
    {
        if (args.Length < 1 || IsHelp(args[0]))
        {
            Console.WriteLine("Usage: FalloutFontTool info <font.aaf>");
            return args.Length < 1 ? 1 : 0;
        }

        string inputPath = args[0];
        AafFont font = new AafReader().Read(inputPath);

        Console.WriteLine("Format........ AAF");
        Console.WriteLine($"File.......... {inputPath}");
        Console.WriteLine($"Signature..... {AafFormat.Signature}");
        Console.WriteLine($"MaxHeight..... {font.MaxHeight}");
        Console.WriteLine($"Glyphs........ {font.Glyphs.Count}");
        Console.WriteLine($"WithBitmap.... {font.NonEmptyGlyphCount}");
        Console.WriteLine($"AdvanceOnly... {font.AdvanceOnlyGlyphCount}");
        Console.WriteLine($"MaxWidth...... {font.MaxGlyphWidth}");
        Console.WriteLine($"BitmapBase.... 0x{AafFormat.BitmapBaseOffset:X4}");
        Console.WriteLine($"BitmapBytes... {font.BitmapBytes}");

        return 0;
    }

    private static int RunExport(string[] args)
    {
        if (args.Length < 1 || IsHelp(args[0]))
        {
            PrintExportHelp();
            return args.Length < 1 ? 1 : 0;
        }

        string inputPath = args[0];
        string outputDirectory = args.Length >= 2 && !args[1].StartsWith("--", StringComparison.Ordinal)
            ? args[1]
            : Path.Combine(
                Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory(),
                Path.GetFileNameWithoutExtension(inputPath) + "_export");

        int scale = ReadIntOption(args, "--scale", defaultValue: 4);
        bool noAtlas = HasFlag(args, "--no-atlas");
        AafPaletteKind paletteKind = ReadPaletteOption(args, "--palette", AafPaletteKind.Auto);

        AafFont font = new AafReader().Read(inputPath);
        AafRenderPalette palette = AafRenderPalette.Create(paletteKind, inputPath);
        AafGlyphRenderer renderer = new AafGlyphRenderer(palette);
        AafImageExporter exporter = new AafImageExporter(renderer);

        exporter.ExportGlyphs(font, outputDirectory, scale, exportAtlas: !noAtlas);

        Console.WriteLine($"Exported {font.Glyphs.Count} glyph PNG files to:");
        Console.WriteLine(outputDirectory);
        if (!noAtlas)
        {
            Console.WriteLine("Atlas: " + Path.Combine(outputDirectory, "atlas.png"));
        }

        return 0;
    }

    private static int ReadIntOption(string[] args, string optionName, int defaultValue)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase)) continue;
            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {optionName}.");
            }

            if (!int.TryParse(args[i + 1], out int value) || value <= 0)
            {
                throw new ArgumentException($"Invalid value for {optionName}: {args[i + 1]}");
            }

            return value;
        }

        return defaultValue;
    }

    private static AafPaletteKind ReadPaletteOption(string[] args, string optionName, AafPaletteKind defaultValue)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase)) continue;
            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {optionName}.");
            }

            if (!Enum.TryParse(args[i + 1], ignoreCase: true, out AafPaletteKind value))
            {
                throw new ArgumentException($"Invalid palette: {args[i + 1]}. Use auto, gray, orange, or green.");
            }

            return value;
        }

        return defaultValue;
    }

    private static bool HasFlag(string[] args, string flag)
    {
        return args.Any(x => x.Equals(flag, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsHelp(string value)
    {
        return value is "-h" or "--help" or "help" or "/?";
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("FalloutFontTool - Fallout 1/2 font utilities");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  FalloutFontTool info <font.aaf>");
        Console.WriteLine("  FalloutFontTool export <font.aaf> [output-dir] [--scale 4] [--palette auto|gray|orange|green] [--no-atlas]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- info samples/FONT3.AAF");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- export samples/FONT4.AAF exports/FONT4 --scale 4 --palette orange");
    }

    private static void PrintExportHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool export <font.aaf> [output-dir] [--scale 4] [--palette auto|gray|orange|green] [--no-atlas]");
    }
}
