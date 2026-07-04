using Fallout.Tools.Core.AAF;
using Fallout.Tools.Core.Fonts;
using Fallout.Tools.Core.Imaging;
using SixLabors.ImageSharp.Formats.Png;

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
                "render" => RunRender(args.Skip(1).ToArray()),
                "ttf" => RunTtf(args.Skip(1).ToArray()),
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

    private static int RunRender(string[] args)
{
    if (args.Length < 3 || IsHelp(args[0]))
    {
        PrintRenderHelp();
        return args.Length < 3 ? 1 : 0;
    }

    string inputPath = args[0];
    string text = args[1];
    string outputPath = args[2];

    int scale = ReadIntOption(args, "--scale", defaultValue: 1);
    int letterSpacing = ReadIntOption(args, "--letter-spacing", defaultValue: 0, allowZero: true);
    int lineSpacing = ReadIntOption(args, "--line-spacing", defaultValue: 0, allowZero: true);
    bool forceUppercase = HasFlag(args, "--uppercase") || HasFlag(args, "--force-uppercase");
    AafPaletteKind paletteKind = ReadPaletteOption(args, "--palette", AafPaletteKind.Auto);

    AafFont font = new AafReader().Read(inputPath);
    AafRenderPalette palette = AafRenderPalette.Create(paletteKind, inputPath);
    AafTextRenderer renderer = new AafTextRenderer(palette);

    using var image = renderer.RenderText(font, text, new AafTextRenderOptions
    {
        Scale = scale,
        LetterSpacing = letterSpacing,
        LineSpacing = lineSpacing,
        ForceUppercase = forceUppercase
    });

    string? outputDirectory = Path.GetDirectoryName(outputPath);

    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    using FileStream stream = File.Create(outputPath);
    image.Save(stream, new PngEncoder());

    Console.WriteLine("Rendered text PNG:");
    Console.WriteLine(outputPath);
    Console.WriteLine($"Text: {text}");
    Console.WriteLine($"Scale: {scale}");
    Console.WriteLine($"Letter spacing: {letterSpacing}");
    Console.WriteLine($"Line spacing: {lineSpacing}");
    Console.WriteLine($"Uppercase: {forceUppercase}");

    return 0;
}

    private static int RunTtf(string[] args)
    {
        if (args.Length < 1 || IsHelp(args[0]))
        {
            PrintTtfHelp();
            return args.Length < 1 ? 1 : 0;
        }

        string inputPath = args[0];
        string outputPath = args.Length >= 2 && !args[1].StartsWith("--", StringComparison.Ordinal)
            ? args[1]
            : Path.Combine(
                Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory(),
                Path.GetFileNameWithoutExtension(inputPath) + ".ttf");

        ushort unitsPerPixel = checked((ushort)ReadIntOption(args, "--units-per-pixel", defaultValue: 64));
        string? familyName = ReadStringOption(args, "--name");

        AafFont font = new AafReader().Read(inputPath);
        TrueTypeExportOptions options = new TrueTypeExportOptions
        {
            FamilyName = familyName ?? Path.GetFileNameWithoutExtension(inputPath),
            UnitsPerPixel = unitsPerPixel
        };

        new TrueTypeFontExporter().Export(font, outputPath, options);

        Console.WriteLine("Generated TrueType font:");
        Console.WriteLine(outputPath);
        Console.WriteLine($"Family name: {options.FamilyName}");
        Console.WriteLine($"Units per pixel: {options.UnitsPerPixel}");

        return 0;
    }

    private static int ReadIntOption(string[] args, string optionName, int defaultValue, bool allowZero = false)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase)) continue;
            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {optionName}.");
            }

            int minimum = allowZero ? 0 : 1;
            if (!int.TryParse(args[i + 1], out int value) || value < minimum)
            {
                throw new ArgumentException($"Invalid value for {optionName}: {args[i + 1]}");
            }

            return value;
        }

        return defaultValue;
    }

    private static string? ReadStringOption(string[] args, string optionName)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase)) continue;
            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {optionName}.");
            }

            return args[i + 1];
        }

        return null;
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
        Console.WriteLine("  FalloutFontTool render <font.aaf> <text> <output.png> [--scale 1] [--palette auto|gray|orange|green] [--letter-spacing 0] [--line-spacing 0] [--uppercase]");
        Console.WriteLine("  FalloutFontTool ttf <font.aaf> [output.ttf] [--name FontName] [--units-per-pixel 64]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- info samples/FONT3.AAF");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- export samples/FONT4.AAF exports/FONT4 --scale 4 --palette orange");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- render samples/FONT4.AAF BARTER exports/BARTER.png --scale 1 --palette orange");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- render samples/FONT4.AAF negociação exports/NEGOCIACAO.png --scale 1 --palette orange --uppercase");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- ttf samples/FONT4.AAF exports/FONT4.ttf --name FalloutFont4");
    }

    private static void PrintExportHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool export <font.aaf> [output-dir] [--scale 4] [--palette auto|gray|orange|green] [--no-atlas]");
    }

    private static void PrintRenderHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool render <font.aaf> <text> <output.png> [--scale 1] [--palette auto|gray|orange|green] [--letter-spacing 0] [--line-spacing 0] [--uppercase]");
    }

    private static void PrintTtfHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool ttf <font.aaf> [output.ttf] [--name FontName] [--units-per-pixel 64]");
    }
}
