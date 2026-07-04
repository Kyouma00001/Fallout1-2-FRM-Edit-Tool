using System.Text;
using Fallout.Tools.Core.AAF;
using Fallout.Tools.Core.Fonts;
using Fallout.Tools.Core.Imaging;
using Fallout.Tools.Core.FRM;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

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
                "render-batch" => RunRenderBatch(args.Skip(1).ToArray()),
                "compose-ui" => RunComposeUi(args.Skip(1).ToArray()),
                "frm-info" => RunFrmInfo(args.Skip(1).ToArray()),
                "frm-export" => RunFrmExport(args.Skip(1).ToArray()),
                "frm-import" => RunFrmImport(args.Skip(1).ToArray()),
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

        AafTextRenderOptions options = ReadTextRenderOptions(args);
        AafPaletteKind paletteKind = ReadPaletteOption(args, "--palette", AafPaletteKind.Auto);

        AafFont font = new AafReader().Read(inputPath);
        AafRenderPalette palette = AafRenderPalette.Create(paletteKind, inputPath);
        AafTextRenderer renderer = new AafTextRenderer(palette);

        RenderTextToPng(renderer, font, text, outputPath, options);

        Console.WriteLine("Rendered text PNG:");
        Console.WriteLine(outputPath);
        Console.WriteLine($"Text: {text}");
        Console.WriteLine($"Scale: {options.Scale}");
        Console.WriteLine($"Letter spacing: {options.LetterSpacing}");
        Console.WriteLine($"Line spacing: {options.LineSpacing}");
        Console.WriteLine($"Uppercase: {options.ForceUppercase}");

        return 0;
    }

    private static int RunRenderBatch(string[] args)
    {
        if (args.Length < 3 || IsHelp(args[0]))
        {
            PrintRenderBatchHelp();
            return args.Length < 3 ? 1 : 0;
        }

        string inputPath = args[0];
        string manifestPath = args[1];
        string outputDirectory = args[2];

        AafTextRenderOptions options = ReadTextRenderOptions(args);
        AafPaletteKind paletteKind = ReadPaletteOption(args, "--palette", AafPaletteKind.Auto);

        AafFont font = new AafReader().Read(inputPath);
        AafRenderPalette palette = AafRenderPalette.Create(paletteKind, inputPath);
        AafTextRenderer renderer = new AafTextRenderer(palette);

        Directory.CreateDirectory(outputDirectory);

        int rendered = 0;
        int lineNumber = 0;

        foreach (string rawLine in File.ReadLines(manifestPath, Encoding.UTF8))
        {
            lineNumber++;

            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            (string name, string text) = ParseBatchLine(line, lineNumber);
            string fileName = MakeSafeFileName(name) + ".png";
            string outputPath = Path.Combine(outputDirectory, fileName);

            RenderTextToPng(renderer, font, text, outputPath, options);

            rendered++;
            Console.WriteLine($"{fileName}: {text}");
        }

        Console.WriteLine();
        Console.WriteLine($"Rendered {rendered} PNG file(s) to:");
        Console.WriteLine(outputDirectory);

        return 0;
    }

    private static int RunComposeUi(string[] args)
    {
        if (args.Length < 4 || IsHelp(args[0]))
        {
            PrintComposeUiHelp();
            return args.Length < 4 ? 1 : 0;
        }

        string inputPath = args[0];
        string backgroundPath = args[1];
        string layoutPath = args[2];
        string outputPath = args[3];

        AafTextRenderOptions options = ReadTextRenderOptions(args);
        AafPaletteKind paletteKind = ReadPaletteOption(args, "--palette", AafPaletteKind.Auto);

        AafFont font = new AafReader().Read(inputPath);
        AafRenderPalette palette = AafRenderPalette.Create(paletteKind, inputPath);
        AafTextRenderer textRenderer = new AafTextRenderer(palette);
        UiTextComposer composer = new UiTextComposer(textRenderer);
        IReadOnlyList<UiTextPlacement> placements = UiTextLayoutParser.ParseFile(layoutPath);

        using Image<Rgba32> background = Image.Load<Rgba32>(backgroundPath);
        using Image<Rgba32> composed = composer.Compose(background, font, placements, options);

        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        using FileStream stream = File.Create(outputPath);
        composed.Save(stream, new PngEncoder());

        Console.WriteLine("Composed UI PNG:");
        Console.WriteLine(outputPath);
        Console.WriteLine($"Base image: {backgroundPath}");
        Console.WriteLine($"Layout: {layoutPath}");
        Console.WriteLine($"Texts: {placements.Count}");
        Console.WriteLine($"Scale: {options.Scale}");
        Console.WriteLine($"Uppercase: {options.ForceUppercase}");

        return 0;
    }

    private static int RunFrmInfo(string[] args)
    {
        if (args.Length < 1 || IsHelp(args[0]))
        {
            PrintFrmInfoHelp();
            return args.Length < 1 ? 1 : 0;
        }

        string inputPath = args[0];
        FrmFile frm = new FrmReader().Read(inputPath);
        FrmFrame firstFrame = frm.FirstFrame;

        Console.WriteLine("Format........ FRM");
        Console.WriteLine($"File.......... {inputPath}");
        Console.WriteLine($"Version....... {frm.Version}");
        Console.WriteLine($"FPS........... {frm.FramesPerSecond}");
        Console.WriteLine($"ActionFrame... {frm.ActionFrame}");
        Console.WriteLine($"Frames/Dir.... {frm.FramesPerDirection}");
        Console.WriteLine($"ParsedFrames.. {frm.Frames.Count}");
        Console.WriteLine($"DataSize...... {frm.DataSize}");
        Console.WriteLine($"Static........ {frm.IsStaticSingleFrame}");
        Console.WriteLine($"FirstFrame.... {firstFrame.Width}x{firstFrame.Height}");
        Console.WriteLine($"Offset........ {firstFrame.OffsetX},{firstFrame.OffsetY}");
        Console.WriteLine("DirOffsets.... " + string.Join(", ", frm.DirectionOffsets.Select(x => x.ToString())));

        return 0;
    }

    private static int RunFrmExport(string[] args)
    {
        if (args.Length < 2 || IsHelp(args[0]))
        {
            PrintFrmExportHelp();
            return args.Length < 2 ? 1 : 0;
        }

        string inputPath = args[0];
        string outputPath = args[1];
        string? actPath = ReadStringOption(args, "--act");
        if (string.IsNullOrWhiteSpace(actPath))
        {
            throw new ArgumentException("Missing --act <palette.act>. Exported BMP files must use the same indexed palette expected by the FRM workflow.");
        }

        FrmFile frm = new FrmReader().Read(inputPath);
        if (!frm.IsStaticSingleFrame)
        {
            throw new FrmException("frm-export currently supports static/single-frame FRM files only.");
        }

        FrmFrame frame = frm.FirstFrame;
        ActPalette palette = ActPalette.Load(actPath);
        var image = new IndexedImage(frame.Width, frame.Height, frame.Pixels.ToArray());
        new IndexedBmp8Writer().Write(outputPath, image, palette.Colors);

        Console.WriteLine("Exported static FRM to indexed 8-bit BMP:");
        Console.WriteLine(outputPath);
        Console.WriteLine($"Size: {frame.Width}x{frame.Height}");
        Console.WriteLine($"ACT: {actPath}");

        return 0;
    }

    private static int RunFrmImport(string[] args)
    {
        if (args.Length < 3 || IsHelp(args[0]))
        {
            PrintFrmImportHelp();
            return args.Length < 3 ? 1 : 0;
        }

        string originalFrmPath = args[0];
        string editedBmpPath = args[1];
        string outputFrmPath = args[2];

        if (IsSamePath(originalFrmPath, outputFrmPath))
        {
            throw new ArgumentException("Refusing to overwrite the original FRM. Choose a different output path.");
        }

        FrmFile original = new FrmReader().Read(originalFrmPath);
        if (!original.IsStaticSingleFrame)
        {
            throw new FrmException("frm-import currently supports static/single-frame FRM files only.");
        }

        FrmFrame frame = original.FirstFrame;
        IndexedImage bmp = new IndexedBmp8Reader().Read(editedBmpPath);
        if (bmp.Width != frame.Width || bmp.Height != frame.Height)
        {
            throw new FrmException($"Edited BMP size is {bmp.Width}x{bmp.Height}, but the original FRM frame is {frame.Width}x{frame.Height}. Use the exact same dimensions.");
        }

        FrmFile output = original.CreateStaticCopyWithFirstFramePixels(bmp.Pixels);
        new FrmWriter().Write(outputFrmPath, output);

        Console.WriteLine("Imported indexed 8-bit BMP into static FRM:");
        Console.WriteLine(outputFrmPath);
        Console.WriteLine($"Size: {frame.Width}x{frame.Height}");
        Console.WriteLine("Pixel indices were copied from the BMP without color remapping.");

        return 0;
    }

    private static void RenderTextToPng(
        AafTextRenderer renderer,
        AafFont font,
        string text,
        string outputPath,
        AafTextRenderOptions options)
    {
        using var image = renderer.RenderText(font, text, options);

        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        using FileStream stream = File.Create(outputPath);
        image.Save(stream, new PngEncoder());
    }

    private static (string Name, string Text) ParseBatchLine(string line, int lineNumber)
    {
        int separatorIndex = line.IndexOf('=');

        if (separatorIndex < 0)
        {
            separatorIndex = line.IndexOf('\t');
        }

        if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
        {
            throw new ArgumentException(
                $"Invalid batch line {lineNumber}. Use NAME=Text to render or NAME<TAB>Text to render.");
        }

        string name = line[..separatorIndex].Trim();
        string text = line[(separatorIndex + 1)..].Trim();

        if (name.Length == 0)
        {
            throw new ArgumentException($"Invalid batch line {lineNumber}: output name is empty.");
        }

        if (text.Length == 0)
        {
            throw new ArgumentException($"Invalid batch line {lineNumber}: text is empty.");
        }

        return (name, text);
    }

    private static string MakeSafeFileName(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        char[] chars = value.Trim().ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (invalidChars.Contains(chars[i]) || char.IsWhiteSpace(chars[i]))
            {
                chars[i] = '_';
            }
        }

        string fileName = new string(chars).Trim('_');
        return fileName.Length == 0 ? "rendered_text" : fileName;
    }

    private static AafTextRenderOptions ReadTextRenderOptions(string[] args)
    {
        return new AafTextRenderOptions
        {
            Scale = ReadIntOption(args, "--scale", defaultValue: 1),
            LetterSpacing = ReadIntOption(args, "--letter-spacing", defaultValue: 0, allowZero: true),
            LineSpacing = ReadIntOption(args, "--line-spacing", defaultValue: 0, allowZero: true),
            ForceUppercase = HasFlag(args, "--uppercase") || HasFlag(args, "--force-uppercase")
        };
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

    private static bool IsSamePath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;

        try
        {
            string fullLeft = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullRight = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(fullLeft, fullRight, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
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
        Console.WriteLine("  FalloutFontTool render-batch <font.aaf> <texts.txt> <output-dir> [--scale 1] [--palette auto|gray|orange|green] [--letter-spacing 0] [--line-spacing 0] [--uppercase]");
        Console.WriteLine("  FalloutFontTool compose-ui <font.aaf> <base.png|base.bmp> <layout.txt> <output.png> [--scale 1] [--palette auto|gray|orange|green] [--letter-spacing 0] [--line-spacing 0] [--uppercase]");
        Console.WriteLine("  FalloutFontTool frm-info <file.frm>");
        Console.WriteLine("  FalloutFontTool frm-export <input.frm> <output.bmp> --act <palette.act>");
        Console.WriteLine("  FalloutFontTool frm-import <original.frm> <edited.bmp> <output.frm>");
        Console.WriteLine("  FalloutFontTool ttf <font.aaf> [output.ttf] [--name FontName] [--units-per-pixel 64]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- info samples/FONT3.AAF");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- export samples/FONT4.AAF exports/FONT4 --scale 4 --palette orange");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- render samples/FONT4.AAF BARTER exports/BARTER.png --scale 1 --palette orange");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- render samples/FONT4.AAF negociação exports/NEGOCIACAO.png --scale 1 --palette orange --uppercase");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- render-batch samples/FONT4.AAF samples/render-batch.example.txt exports/ui-text --scale 1 --palette orange --uppercase");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- compose-ui samples/FONT4.AAF samples/ui-compose-base.png samples/ui-compose.example.txt exports/ui-composed.png --scale 1 --palette orange --uppercase");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- frm-info path/to/INVBOX.frm");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- frm-export path/to/INVBOX.frm exports/INVBOX.bmp --act palettes/Default.act");
        Console.WriteLine("  dotnet run --project src/Fallout.Tools.CLI -- frm-import path/to/INVBOX.frm exports/INVBOX-edited.bmp exports/INVBOX-new.frm");
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

    private static void PrintRenderBatchHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool render-batch <font.aaf> <texts.txt> <output-dir> [--scale 1] [--palette auto|gray|orange|green] [--letter-spacing 0] [--line-spacing 0] [--uppercase]");
        Console.WriteLine();
        Console.WriteLine("Text file format:");
        Console.WriteLine("  OUTPUT_NAME=Text to render");
        Console.WriteLine("  # Lines starting with # are ignored");
    }

    private static void PrintComposeUiHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool compose-ui <font.aaf> <base.png|base.bmp> <layout.txt> <output.png> [--scale 1] [--palette auto|gray|orange|green] [--letter-spacing 0] [--line-spacing 0] [--uppercase]");
        Console.WriteLine();
        Console.WriteLine("Layout file format:");
        Console.WriteLine("  NAME|X|Y|TEXT");
        Console.WriteLine("  NAME|X|Y|WIDTH|left|TEXT");
        Console.WriteLine("  NAME|X|Y|WIDTH|center|TEXT");
        Console.WriteLine("  NAME|X|Y|WIDTH|right|TEXT");
        Console.WriteLine("  # Lines starting with # are ignored");
    }

    private static void PrintFrmInfoHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool frm-info <file.frm>");
    }

    private static void PrintFrmExportHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool frm-export <input.frm> <output.bmp> --act <palette.act>");
        Console.WriteLine();
        Console.WriteLine("Exports a static/single-frame FRM to an indexed 8-bit BMP using the supplied ACT palette.");
    }

    private static void PrintFrmImportHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool frm-import <original.frm> <edited.bmp> <output.frm>");
        Console.WriteLine();
        Console.WriteLine("Imports an 8-bit indexed BMP back into a static/single-frame FRM. The BMP must have the exact same size as the original frame.");
    }

    private static void PrintTtfHelp()
    {
        Console.WriteLine("Usage: FalloutFontTool ttf <font.aaf> [output.ttf] [--name FontName] [--units-per-pixel 64]");
    }
}
