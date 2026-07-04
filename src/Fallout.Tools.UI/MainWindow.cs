using AvaloniaImage = Avalonia.Controls.Image;
using AvaloniaBorder = Avalonia.Controls.Border;
using AvaloniaPoint = Avalonia.Point;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Fallout.Tools.Core.AAF;
using Fallout.Tools.Core.Imaging;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.Text.Json;

namespace Fallout.Tools.UI;

public sealed class MainWindow : Window
{
    private readonly Canvas _canvas = new();
    private readonly AvaloniaImage _baseImage = new();
    private readonly ListBox _itemsList = new();
    private readonly ListBox _eraseList = new();
    private readonly TextBox _nameBox = new() { Text = "TEXT01" };
    private readonly TextBox _textBox = new() { Text = "negociar" };
    private readonly TextBox _xBox = new() { Text = "10" };
    private readonly TextBox _yBox = new() { Text = "10" };
    private readonly TextBox _widthBox = new() { Text = "0" };
    private readonly TextBox _scaleBox = new() { Text = "1" };
    private readonly TextBox _widthScaleBox = new() { Text = "1.0" };
    private readonly TextBox _heightScaleBox = new() { Text = "1.0" };
    private readonly TextBox _letterSpacingBox = new() { Text = "0" };
    private readonly ComboBox _alignBox = new();
    private readonly CheckBox _uppercaseBox = new() { Content = "Uppercase", IsChecked = true };

    private readonly TextBox _eraseNameBox = new() { Text = "ERASE01" };
    private readonly TextBox _eraseXBox = new() { Text = "10" };
    private readonly TextBox _eraseYBox = new() { Text = "10" };
    private readonly TextBox _eraseWidthBox = new() { Text = "40" };
    private readonly TextBox _eraseHeightBox = new() { Text = "16" };
    private readonly TextBox _eraseSourceXBox = new() { Text = "10" };
    private readonly TextBox _eraseSourceYBox = new() { Text = "30" };

    private readonly TextBlock _status = new() { Text = "Open a base image and an AAF font to start." };

    private readonly List<UiTextItem> _items = new();
    private readonly List<EraseArea> _eraseAreas = new();
    private string? _baseImagePath;
    private Bitmap? _baseBitmap;
    private AafFont? _font;
    private string? _fontPath;
    private AafRenderPalette _palette = AafRenderPalette.Create(AafPaletteKind.Orange, string.Empty);
    private Rgba32[]? _exportPalette;
    private string? _exportPalettePath;
    private UiTextItem? _selectedItem;
    private EraseArea? _selectedErase;
    private AvaloniaPoint _dragOffset;
    private DragMode _dragMode = DragMode.None;
    private UiTextItem? _dragItem;
    private AvaloniaPoint _dragStartCanvas;
    private int _dragStartWidth;
    private int _dragStartHeight;
    private int _dragStartRenderedWidth;
    private int _dragStartRenderedHeight;
    private double _dragStartWidthScale;
    private double _dragStartHeightScale;

    private const double ResizeHandleSize = 8;

    private static readonly JsonSerializerOptions ProjectJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public MainWindow()
    {
        Title = "Fallout UI Text Layout Editor";
        Width = 1200;
        Height = 760;
        MinWidth = 900;
        MinHeight = 600;
        Focusable = true;
        KeyDown += OnEditorKeyDown;

        _alignBox.ItemsSource = new[] { "left", "center", "right" };
        _alignBox.SelectedIndex = 0;

        Content = BuildLayout();
        _baseImage.Stretch = Stretch.None;
        SetZIndex(_baseImage, 0);
        _canvas.Children.Add(_baseImage);
    }

    private Control BuildLayout()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            ColumnDefinitions = new ColumnDefinitions("*,300"),
            Margin = new Thickness(8)
        };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 8)
        };

        toolbar.Children.Add(MakeButton("Open image", OnOpenImageAsync));
        toolbar.Children.Add(MakeButton("Open AAF", OnOpenAafAsync));
        toolbar.Children.Add(MakeButton("Open ACT", OnOpenActAsync));
        toolbar.Children.Add(MakeButton("Add text", _ => AddTextItem()));
        toolbar.Children.Add(MakeButton("Add erase", _ => AddEraseArea()));
        toolbar.Children.Add(MakeButton("Remove", _ => RemoveSelected()));
        toolbar.Children.Add(MakeButton("Open project", OnOpenProjectAsync));
        toolbar.Children.Add(MakeButton("Save project", OnSaveProjectAsync));
        toolbar.Children.Add(MakeButton("Save layout", OnSaveLayoutAsync));
        toolbar.Children.Add(MakeButton("Export BMP 8-bit", OnExportBmp8Async));
        toolbar.Children.Add(MakeButton("Export PNG preview", OnExportPngAsync));

        Grid.SetColumnSpan(toolbar, 2);
        root.Children.Add(toolbar);

        var scroll = new ScrollViewer
        {
            Background = Brushes.Black,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _canvas
        };

        Grid.SetRow(scroll, 1);
        Grid.SetColumn(scroll, 0);
        root.Children.Add(scroll);

        var panel = BuildSidePanel();
        Grid.SetRow(panel, 1);
        Grid.SetColumn(panel, 1);
        root.Children.Add(panel);

        return root;
    }

    private Control BuildSidePanel()
    {
        var panel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(10, 0, 0, 0)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Text objects",
            FontWeight = FontWeight.Bold,
            FontSize = 16
        });

        _itemsList.Height = 160;
        _itemsList.SelectionChanged += (_, _) =>
        {
            if (_itemsList.SelectedItem is UiTextItem item)
            {
                SelectItem(item);
            }
        };
        panel.Children.Add(_itemsList);

        panel.Children.Add(new Separator());
        panel.Children.Add(Labeled("Name", _nameBox));
        panel.Children.Add(Labeled("Text", _textBox));
        panel.Children.Add(Labeled("X", _xBox));
        panel.Children.Add(Labeled("Y", _yBox));
        panel.Children.Add(Labeled("Width (0 = natural)", _widthBox));
        panel.Children.Add(Labeled("Scale (integer)", _scaleBox));
        panel.Children.Add(Labeled("Width scale", _widthScaleBox));
        panel.Children.Add(Labeled("Height scale", _heightScaleBox));
        panel.Children.Add(Labeled("Letter spacing", _letterSpacingBox));
        panel.Children.Add(Labeled("Align", _alignBox));
        panel.Children.Add(_uppercaseBox);
        panel.Children.Add(MakeButton("Apply text changes", _ => ApplyEditorFieldsToSelected()));

        panel.Children.Add(new Separator());
        panel.Children.Add(new TextBlock
        {
            Text = "Erase / clone patches",
            FontWeight = FontWeight.Bold,
            FontSize = 16
        });

        _eraseList.Height = 120;
        _eraseList.SelectionChanged += (_, _) =>
        {
            if (_eraseList.SelectedItem is EraseArea area)
            {
                SelectEraseArea(area);
            }
        };
        panel.Children.Add(_eraseList);
        panel.Children.Add(Labeled("Erase name", _eraseNameBox));
        panel.Children.Add(Labeled("Target X", _eraseXBox));
        panel.Children.Add(Labeled("Target Y", _eraseYBox));
        panel.Children.Add(Labeled("Target width", _eraseWidthBox));
        panel.Children.Add(Labeled("Target height", _eraseHeightBox));
        panel.Children.Add(Labeled("Source X", _eraseSourceXBox));
        panel.Children.Add(Labeled("Source Y", _eraseSourceYBox));
        panel.Children.Add(MakeButton("Add erase patch", _ => AddEraseArea()));
        panel.Children.Add(MakeButton("Apply erase changes", _ => ApplyEraseFieldsToSelected()));

        panel.Children.Add(new Separator());
        panel.Children.Add(new TextBlock
        {
            Text = "Tip: erase patches clone a clean area of the base image over old text before the translated text is rendered. Drag a patch to move it, drag its handle to resize it, and adjust Source X/Y to choose where the clean texture comes from.",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(_status);

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = panel
        };
    }

    private static Control Labeled(string label, Control control)
    {
        return new StackPanel
        {
            Spacing = 2,
            Children =
            {
                new TextBlock { Text = label },
                control
            }
        };
    }

    private static Button MakeButton(string text, Action<RoutedEventArgs> onClick)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(10, 4)
        };
        button.Click += (_, e) => onClick(e);
        return button;
    }

    private async void OnOpenImageAsync(RoutedEventArgs _)
    {
        string? path = await PickOpenFileAsync("Open clean UI image", new[] { "*.png", "*.bmp" });
        if (path is null) return;

        LoadBaseImage(path);
        SetStatus($"Opened image: {Path.GetFileName(path)} ({_baseBitmap?.PixelSize.Width}x{_baseBitmap?.PixelSize.Height})");
    }

    private async void OnOpenAafAsync(RoutedEventArgs _)
    {
        string? path = await PickOpenFileAsync("Open AAF font", new[] { "*.aaf" });
        if (path is null) return;

        LoadAafFont(path);
        SetStatus($"Opened font: {Path.GetFileName(path)}");
    }

    private async void OnOpenActAsync(RoutedEventArgs _)
    {
        string? path = await PickOpenFileAsync("Open ACT palette", new[] { "*.act" });
        if (path is null) return;

        try
        {
            LoadActPaletteFile(path);
            SetStatus($"Opened ACT palette: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not open ACT palette: {ex.Message}");
        }
    }

    private async void OnSaveLayoutAsync(RoutedEventArgs _)
    {
        string? path = await PickSaveFileAsync("Save UI layout", "ui-layout.txt", new[] { "*.txt" });
        if (path is null) return;

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
        File.WriteAllLines(path, _eraseAreas.Select(SerializeEraseLayoutLine).Concat(_items.Select(SerializeLayoutLine)));
        SetStatus($"Saved layout: {path}");
    }

    private async void OnSaveProjectAsync(RoutedEventArgs _)
    {
        string? path = await PickSaveFileAsync("Save Fallout UI project", "ui-project.fui.json", new[] { "*.fui.json", "*.json" });
        if (path is null) return;

        try
        {
            UiProjectDocument project = CreateProjectDocument(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
            string json = JsonSerializer.Serialize(project, ProjectJsonOptions);
            await File.WriteAllTextAsync(path, json);
            SetStatus($"Saved project: {path}");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not save project: {ex.Message}");
        }
    }

    private async void OnOpenProjectAsync(RoutedEventArgs _)
    {
        string? path = await PickOpenFileAsync("Open Fallout UI project", new[] { "*.fui.json", "*.json" });
        if (path is null) return;

        try
        {
            string json = await File.ReadAllTextAsync(path);
            UiProjectDocument? project = JsonSerializer.Deserialize<UiProjectDocument>(json, ProjectJsonOptions);
            if (project is null)
            {
                SetStatus("Could not open project: file is empty or invalid.");
                return;
            }

            LoadProjectDocument(project, path);
        }
        catch (Exception ex)
        {
            SetStatus($"Could not open project: {ex.Message}");
        }
    }

    private async void OnExportPngAsync(RoutedEventArgs _)
    {
        if (!CanExportComposition()) return;

        string? path = await PickSaveFileAsync("Export composed PNG preview", "ui-composed.png", new[] { "*.png" });
        if (path is null) return;

        using SixLabors.ImageSharp.Image<Rgba32> output = ComposeOutputImage();
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
        await using FileStream stream = File.Create(path);
        output.Save(stream, new PngEncoder());
        SetStatus($"Exported PNG preview: {path}");
    }

    private async void OnExportBmp8Async(RoutedEventArgs _)
    {
        if (!CanExportComposition()) return;

        if (_exportPalette is null)
        {
            SetStatus("Open an ACT palette first. For Fallout UI, use the same palette that will be used when converting back to FRM.");
            return;
        }

        string? path = await PickSaveFileAsync("Export 8-bit BMP", "ui-composed.bmp", new[] { "*.bmp" });
        if (path is null) return;

        using SixLabors.ImageSharp.Image<Rgba32> output = ComposeOutputImage();
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
        SaveIndexedBmp8(path, output, _exportPalette);
        SetStatus($"Exported 8-bit BMP: {path} using palette {Path.GetFileName(_exportPalettePath)}");
    }

    private bool CanExportComposition()
    {
        if (_baseImagePath is null)
        {
            SetStatus("Open a base image first.");
            return false;
        }

        if (_font is null)
        {
            SetStatus("Open an AAF font first.");
            return false;
        }

        return true;
    }

    private SixLabors.ImageSharp.Image<Rgba32> ComposeOutputImage()
    {
        if (_baseImagePath is null) throw new InvalidOperationException("Base image is not loaded.");

        SixLabors.ImageSharp.Image<Rgba32> output = SixLabors.ImageSharp.Image.Load<Rgba32>(_baseImagePath);

        using SixLabors.ImageSharp.Image<Rgba32> cleanSource = output.Clone();
        foreach (EraseArea area in _eraseAreas)
        {
            ApplyErasePatch(output, cleanSource, area);
        }

        foreach (UiTextItem item in _items)
        {
            using SixLabors.ImageSharp.Image<Rgba32> textImage = RenderItem(item);
            int drawX = GetAlignedX(item, textImage.Width);
            Composite(output, textImage, drawX, item.Y);
        }

        return output;
    }

    private UiProjectDocument CreateProjectDocument(string projectPath)
    {
        return new UiProjectDocument
        {
            Version = 1,
            BaseImagePath = MakeProjectPath(_baseImagePath, projectPath),
            AafFontPath = MakeProjectPath(_fontPath, projectPath),
            ActPalettePath = MakeProjectPath(_exportPalettePath, projectPath),
            Texts = _items.Select(item => new UiTextData
            {
                Name = item.Name,
                Text = item.Text,
                X = item.X,
                Y = item.Y,
                Width = item.Width,
                Align = item.Align,
                Scale = item.Scale,
                WidthScale = item.WidthScale,
                HeightScale = item.HeightScale,
                LetterSpacing = item.LetterSpacing,
                ForceUppercase = item.ForceUppercase
            }).ToList(),
            EraseAreas = _eraseAreas.Select(area => new EraseAreaData
            {
                Name = area.Name,
                X = area.X,
                Y = area.Y,
                Width = area.Width,
                Height = area.Height,
                SourceX = area.SourceX,
                SourceY = area.SourceY
            }).ToList()
        };
    }

    private void LoadProjectDocument(UiProjectDocument project, string projectPath)
    {
        ClearEditableObjects();

        var messages = new List<string>();

        string? baseImagePath = ResolveProjectPath(project.BaseImagePath, projectPath);
        if (!string.IsNullOrWhiteSpace(baseImagePath))
        {
            if (File.Exists(baseImagePath))
            {
                LoadBaseImage(baseImagePath);
                messages.Add($"image {Path.GetFileName(baseImagePath)}");
            }
            else
            {
                messages.Add($"missing image {project.BaseImagePath}");
            }
        }

        string? fontPath = ResolveProjectPath(project.AafFontPath, projectPath);
        if (!string.IsNullOrWhiteSpace(fontPath))
        {
            if (File.Exists(fontPath))
            {
                LoadAafFont(fontPath);
                messages.Add($"font {Path.GetFileName(fontPath)}");
            }
            else
            {
                messages.Add($"missing font {project.AafFontPath}");
            }
        }

        string? actPath = ResolveProjectPath(project.ActPalettePath, projectPath);
        if (!string.IsNullOrWhiteSpace(actPath))
        {
            if (File.Exists(actPath))
            {
                LoadActPaletteFile(actPath);
                messages.Add($"ACT {Path.GetFileName(actPath)}");
            }
            else
            {
                messages.Add($"missing ACT {project.ActPalettePath}");
            }
        }

        foreach (EraseAreaData area in project.EraseAreas)
        {
            _eraseNameBox.Text = string.IsNullOrWhiteSpace(area.Name) ? $"ERASE{_eraseAreas.Count + 1:00}" : area.Name;
            _eraseXBox.Text = area.X.ToString(CultureInfo.InvariantCulture);
            _eraseYBox.Text = area.Y.ToString(CultureInfo.InvariantCulture);
            _eraseWidthBox.Text = Math.Max(1, area.Width).ToString(CultureInfo.InvariantCulture);
            _eraseHeightBox.Text = Math.Max(1, area.Height).ToString(CultureInfo.InvariantCulture);
            _eraseSourceXBox.Text = area.SourceX.ToString(CultureInfo.InvariantCulture);
            _eraseSourceYBox.Text = area.SourceY.ToString(CultureInfo.InvariantCulture);
            AddEraseArea();
        }

        foreach (UiTextData text in project.Texts)
        {
            _nameBox.Text = string.IsNullOrWhiteSpace(text.Name) ? $"TEXT{_items.Count + 1:00}" : text.Name;
            _textBox.Text = text.Text ?? string.Empty;
            _xBox.Text = text.X.ToString(CultureInfo.InvariantCulture);
            _yBox.Text = text.Y.ToString(CultureInfo.InvariantCulture);
            _widthBox.Text = text.Width.ToString(CultureInfo.InvariantCulture);
            _scaleBox.Text = Math.Max(1, text.Scale).ToString(CultureInfo.InvariantCulture);
            _widthScaleBox.Text = Math.Max(0.1, text.WidthScale <= 0 ? 1.0 : text.WidthScale).ToString("0.###", CultureInfo.InvariantCulture);
            _heightScaleBox.Text = Math.Max(0.1, text.HeightScale <= 0 ? 1.0 : text.HeightScale).ToString("0.###", CultureInfo.InvariantCulture);
            _letterSpacingBox.Text = text.LetterSpacing.ToString(CultureInfo.InvariantCulture);
            _alignBox.SelectedItem = string.IsNullOrWhiteSpace(text.Align) ? "left" : text.Align;
            _uppercaseBox.IsChecked = text.ForceUppercase;
            AddTextItem();
        }

        _itemsList.ItemsSource = null;
        _itemsList.ItemsSource = _items;
        _eraseList.ItemsSource = null;
        _eraseList.ItemsSource = _eraseAreas;

        if (_items.Count > 0)
        {
            _itemsList.SelectedItem = _items[0];
        }
        else if (_eraseAreas.Count > 0)
        {
            _eraseList.SelectedItem = _eraseAreas[0];
        }

        string suffix = messages.Count > 0 ? $" ({string.Join(", ", messages)})" : string.Empty;
        SetStatus($"Loaded project: {Path.GetFileName(projectPath)}{suffix}");
    }

    private void ClearEditableObjects()
    {
        foreach (UiTextItem item in _items)
        {
            _canvas.Children.Remove(item.ImageControl);
            _canvas.Children.Remove(item.SelectionBorder);
            _canvas.Children.Remove(item.WidthHandle);
            _canvas.Children.Remove(item.ScaleHandle);
        }

        foreach (EraseArea area in _eraseAreas)
        {
            _canvas.Children.Remove(area.ImageControl);
            _canvas.Children.Remove(area.TargetBorder);
            _canvas.Children.Remove(area.SourceBorder);
            _canvas.Children.Remove(area.ResizeHandle);
        }

        _items.Clear();
        _eraseAreas.Clear();
        _selectedItem = null;
        _selectedErase = null;
        _itemsList.SelectedItem = null;
        _eraseList.SelectedItem = null;
        _itemsList.ItemsSource = null;
        _eraseList.ItemsSource = null;
    }

    private void LoadBaseImage(string path)
    {
        _baseImagePath = path;
        using FileStream stream = File.OpenRead(path);
        _baseBitmap = new Bitmap(stream);
        _baseImage.Source = _baseBitmap;
        _canvas.Width = _baseBitmap.PixelSize.Width;
        _canvas.Height = _baseBitmap.PixelSize.Height;
        Canvas.SetLeft(_baseImage, 0);
        Canvas.SetTop(_baseImage, 0);

        foreach (EraseArea area in _eraseAreas)
        {
            UpdateEraseVisual(area);
        }
    }

    private void LoadAafFont(string path)
    {
        _fontPath = path;
        _font = new AafReader().Read(path);
        _palette = AafRenderPalette.Create(AafPaletteKind.Orange, path);

        foreach (UiTextItem item in _items)
        {
            UpdateTextBitmap(item);
        }
    }

    private void LoadActPaletteFile(string path)
    {
        _exportPalette = LoadActPalette(path);
        _exportPalettePath = path;
    }

    private static string? MakeProjectPath(string? path, string projectPath)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            string? directory = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return Path.GetRelativePath(directory, path);
            }
        }
        catch
        {
            // Fall back to the original path if it cannot be converted.
        }

        return path;
    }

    private static string? ResolveProjectPath(string? path, string projectPath)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (Path.IsPathRooted(path)) return path;

        string? directory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        return Path.GetFullPath(Path.Combine(directory, path));
    }

    private async Task<string?> PickOpenFileAsync(string title, IReadOnlyList<string> patterns)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(title) { Patterns = patterns }
            }
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickSaveFileAsync(string title, string suggestedName, IReadOnlyList<string> patterns)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(title) { Patterns = patterns }
            }
        });

        return file?.TryGetLocalPath();
    }

    private void AddTextItem()
    {
        var item = new UiTextItem
        {
            Name = string.IsNullOrWhiteSpace(_nameBox.Text) ? $"TEXT{_items.Count + 1:00}" : _nameBox.Text.Trim(),
            Text = _textBox.Text ?? string.Empty,
            X = ParseInt(_xBox.Text, 10),
            Y = ParseInt(_yBox.Text, 10),
            Width = ParseInt(_widthBox.Text, 0),
            Scale = Math.Max(1, ParseInt(_scaleBox.Text, 1)),
            WidthScale = Math.Max(0.1, ParseDouble(_widthScaleBox.Text, 1.0)),
            HeightScale = Math.Max(0.1, ParseDouble(_heightScaleBox.Text, 1.0)),
            LetterSpacing = ParseInt(_letterSpacingBox.Text, 0),
            Align = (_alignBox.SelectedItem as string) ?? "left",
            ForceUppercase = _uppercaseBox.IsChecked == true,
            ImageControl = new AvaloniaImage { Stretch = Stretch.None },
            SelectionBorder = CreateSelectionBorder(),
            WidthHandle = CreateResizeHandle(),
            ScaleHandle = CreateResizeHandle()
        };

        item.ImageControl.PointerPressed += (_, e) => StartDrag(item, e);
        item.ImageControl.PointerMoved += (_, e) => ContinueDrag(item, e);
        item.ImageControl.PointerReleased += (_, e) => EndDrag(e);

        item.WidthHandle.PointerPressed += (_, e) => StartResizeWidth(item, e);
        item.WidthHandle.PointerMoved += (_, e) => ContinueResize(item, e);
        item.WidthHandle.PointerReleased += (_, e) => EndDrag(e);

        item.ScaleHandle.PointerPressed += (_, e) => StartResizeScale(item, e);
        item.ScaleHandle.PointerMoved += (_, e) => ContinueResize(item, e);
        item.ScaleHandle.PointerReleased += (_, e) => EndDrag(e);

        SetZIndex(item.ImageControl, 10);
        SetZIndex(item.SelectionBorder, 30);
        SetZIndex(item.WidthHandle, 40);
        SetZIndex(item.ScaleHandle, 40);

        _items.Add(item);
        _canvas.Children.Add(item.ImageControl);
        _canvas.Children.Add(item.SelectionBorder);
        _canvas.Children.Add(item.WidthHandle);
        _canvas.Children.Add(item.ScaleHandle);
        _itemsList.ItemsSource = null;
        _itemsList.ItemsSource = _items;
        _itemsList.SelectedItem = item;
        UpdateTextBitmap(item);
        SelectItem(item);
    }

    private void RemoveSelected()
    {
        if (_selectedItem is not null)
        {
            RemoveSelectedText();
            return;
        }

        if (_selectedErase is not null)
        {
            RemoveSelectedErase();
        }
    }

    private void RemoveSelectedText()
    {
        if (_selectedItem is null) return;

        _canvas.Children.Remove(_selectedItem.ImageControl);
        _canvas.Children.Remove(_selectedItem.SelectionBorder);
        _canvas.Children.Remove(_selectedItem.WidthHandle);
        _canvas.Children.Remove(_selectedItem.ScaleHandle);
        _items.Remove(_selectedItem);
        _selectedItem = null;
        UpdateSelectionVisuals();
        _itemsList.ItemsSource = null;
        _itemsList.ItemsSource = _items;
        SetStatus("Removed selected text item.");
    }

    private void RemoveSelectedErase()
    {
        if (_selectedErase is null) return;

        _canvas.Children.Remove(_selectedErase.ImageControl);
        _canvas.Children.Remove(_selectedErase.TargetBorder);
        _canvas.Children.Remove(_selectedErase.SourceBorder);
        _canvas.Children.Remove(_selectedErase.ResizeHandle);
        _eraseAreas.Remove(_selectedErase);
        _selectedErase = null;
        UpdateSelectionVisuals();
        _eraseList.ItemsSource = null;
        _eraseList.ItemsSource = _eraseAreas;
        SetStatus("Removed selected erase patch.");
    }

    private void SelectItem(UiTextItem item)
    {
        _selectedErase = null;
        if (_eraseList.SelectedItem is not null)
        {
            _eraseList.SelectedItem = null;
        }

        _selectedItem = item;
        SyncEditorFields(item);
        UpdateSelectionVisuals();
        Focus();
        SetStatus($"Selected text: {item.Name}");
    }

    private void SyncEditorFields(UiTextItem item)
    {
        _nameBox.Text = item.Name;
        _textBox.Text = item.Text;
        _xBox.Text = item.X.ToString(CultureInfo.InvariantCulture);
        _yBox.Text = item.Y.ToString(CultureInfo.InvariantCulture);
        _widthBox.Text = item.Width.ToString(CultureInfo.InvariantCulture);
        _scaleBox.Text = item.Scale.ToString(CultureInfo.InvariantCulture);
        _widthScaleBox.Text = item.WidthScale.ToString("0.###", CultureInfo.InvariantCulture);
        _heightScaleBox.Text = item.HeightScale.ToString("0.###", CultureInfo.InvariantCulture);
        _letterSpacingBox.Text = item.LetterSpacing.ToString(CultureInfo.InvariantCulture);
        _alignBox.SelectedItem = item.Align;
        _uppercaseBox.IsChecked = item.ForceUppercase;
    }

    private void ApplyEditorFieldsToSelected()
    {
        if (_selectedItem is null)
        {
            AddTextItem();
            return;
        }

        _selectedItem.Name = string.IsNullOrWhiteSpace(_nameBox.Text) ? _selectedItem.Name : _nameBox.Text.Trim();
        _selectedItem.Text = _textBox.Text ?? string.Empty;
        _selectedItem.X = ParseInt(_xBox.Text, _selectedItem.X);
        _selectedItem.Y = ParseInt(_yBox.Text, _selectedItem.Y);
        _selectedItem.Width = ParseInt(_widthBox.Text, _selectedItem.Width);
        _selectedItem.Scale = Math.Max(1, ParseInt(_scaleBox.Text, _selectedItem.Scale));
        _selectedItem.WidthScale = Math.Max(0.1, ParseDouble(_widthScaleBox.Text, _selectedItem.WidthScale));
        _selectedItem.HeightScale = Math.Max(0.1, ParseDouble(_heightScaleBox.Text, _selectedItem.HeightScale));
        _selectedItem.LetterSpacing = ParseInt(_letterSpacingBox.Text, _selectedItem.LetterSpacing);
        _selectedItem.Align = (_alignBox.SelectedItem as string) ?? "left";
        _selectedItem.ForceUppercase = _uppercaseBox.IsChecked == true;

        UpdateTextBitmap(_selectedItem);
        _itemsList.ItemsSource = null;
        _itemsList.ItemsSource = _items;
        _itemsList.SelectedItem = _selectedItem;
    }

    private void AddEraseArea()
    {
        var area = new EraseArea
        {
            Name = string.IsNullOrWhiteSpace(_eraseNameBox.Text) ? $"ERASE{_eraseAreas.Count + 1:00}" : _eraseNameBox.Text.Trim(),
            X = Math.Max(0, ParseInt(_eraseXBox.Text, 10)),
            Y = Math.Max(0, ParseInt(_eraseYBox.Text, 10)),
            Width = Math.Max(1, ParseInt(_eraseWidthBox.Text, 40)),
            Height = Math.Max(1, ParseInt(_eraseHeightBox.Text, 16)),
            SourceX = Math.Max(0, ParseInt(_eraseSourceXBox.Text, 10)),
            SourceY = Math.Max(0, ParseInt(_eraseSourceYBox.Text, 30)),
            ImageControl = new AvaloniaImage { Stretch = Stretch.None },
            TargetBorder = CreateEraseTargetBorder(),
            SourceBorder = CreateEraseSourceBorder(),
            ResizeHandle = CreateResizeHandle()
        };

        area.ImageControl.PointerPressed += (_, e) => StartEraseMove(area, e);
        area.ImageControl.PointerMoved += (_, e) => ContinueEraseMove(area, e);
        area.ImageControl.PointerReleased += (_, e) => EndDrag(e);

        area.ResizeHandle.PointerPressed += (_, e) => StartEraseResize(area, e);
        area.ResizeHandle.PointerMoved += (_, e) => ContinueEraseResize(area, e);
        area.ResizeHandle.PointerReleased += (_, e) => EndDrag(e);

        area.SourceBorder.PointerPressed += (_, e) => StartEraseSourceMove(area, e);
        area.SourceBorder.PointerMoved += (_, e) => ContinueEraseSourceMove(area, e);
        area.SourceBorder.PointerReleased += (_, e) => EndDrag(e);

        SetZIndex(area.ImageControl, 1);
        SetZIndex(area.TargetBorder, 25);
        SetZIndex(area.SourceBorder, 24);
        SetZIndex(area.ResizeHandle, 40);

        _eraseAreas.Add(area);
        _canvas.Children.Add(area.ImageControl);
        _canvas.Children.Add(area.TargetBorder);
        _canvas.Children.Add(area.SourceBorder);
        _canvas.Children.Add(area.ResizeHandle);
        _eraseList.ItemsSource = null;
        _eraseList.ItemsSource = _eraseAreas;
        _eraseList.SelectedItem = area;
        UpdateEraseVisual(area);
        SelectEraseArea(area);
    }

    private void SelectEraseArea(EraseArea area)
    {
        _selectedItem = null;
        if (_itemsList.SelectedItem is not null)
        {
            _itemsList.SelectedItem = null;
        }

        _selectedErase = area;
        SyncEraseEditorFields(area);
        UpdateSelectionVisuals();
        Focus();
        SetStatus($"Selected erase patch: {area.Name}");
    }

    private void SyncEraseEditorFields(EraseArea area)
    {
        _eraseNameBox.Text = area.Name;
        _eraseXBox.Text = area.X.ToString(CultureInfo.InvariantCulture);
        _eraseYBox.Text = area.Y.ToString(CultureInfo.InvariantCulture);
        _eraseWidthBox.Text = area.Width.ToString(CultureInfo.InvariantCulture);
        _eraseHeightBox.Text = area.Height.ToString(CultureInfo.InvariantCulture);
        _eraseSourceXBox.Text = area.SourceX.ToString(CultureInfo.InvariantCulture);
        _eraseSourceYBox.Text = area.SourceY.ToString(CultureInfo.InvariantCulture);
    }

    private void ApplyEraseFieldsToSelected()
    {
        if (_selectedErase is null)
        {
            AddEraseArea();
            return;
        }

        _selectedErase.Name = string.IsNullOrWhiteSpace(_eraseNameBox.Text) ? _selectedErase.Name : _eraseNameBox.Text.Trim();
        _selectedErase.X = Math.Max(0, ParseInt(_eraseXBox.Text, _selectedErase.X));
        _selectedErase.Y = Math.Max(0, ParseInt(_eraseYBox.Text, _selectedErase.Y));
        _selectedErase.Width = Math.Max(1, ParseInt(_eraseWidthBox.Text, _selectedErase.Width));
        _selectedErase.Height = Math.Max(1, ParseInt(_eraseHeightBox.Text, _selectedErase.Height));
        _selectedErase.SourceX = Math.Max(0, ParseInt(_eraseSourceXBox.Text, _selectedErase.SourceX));
        _selectedErase.SourceY = Math.Max(0, ParseInt(_eraseSourceYBox.Text, _selectedErase.SourceY));

        UpdateEraseVisual(_selectedErase);
        _eraseList.ItemsSource = null;
        _eraseList.ItemsSource = _eraseAreas;
        _eraseList.SelectedItem = _selectedErase;
    }

    private void UpdateEraseVisual(EraseArea area)
    {
        UpdateErasePreview(area);

        area.TargetBorder.Width = area.Width;
        area.TargetBorder.Height = area.Height;
        Canvas.SetLeft(area.ImageControl, area.X);
        Canvas.SetTop(area.ImageControl, area.Y);
        Canvas.SetLeft(area.TargetBorder, area.X);
        Canvas.SetTop(area.TargetBorder, area.Y);
        Canvas.SetLeft(area.ResizeHandle, area.X + area.Width - ResizeHandleSize / 2);
        Canvas.SetTop(area.ResizeHandle, area.Y + area.Height - ResizeHandleSize / 2);

        area.SourceBorder.Width = area.Width;
        area.SourceBorder.Height = area.Height;
        Canvas.SetLeft(area.SourceBorder, area.SourceX);
        Canvas.SetTop(area.SourceBorder, area.SourceY);

        UpdateSelectionVisuals();
    }

    private void UpdateErasePreview(EraseArea area)
    {
        if (_baseImagePath is null)
        {
            area.ImageControl.Source = null;
            return;
        }

        using SixLabors.ImageSharp.Image<Rgba32> baseImage = SixLabors.ImageSharp.Image.Load<Rgba32>(_baseImagePath);
        using var patch = new SixLabors.ImageSharp.Image<Rgba32>(area.Width, area.Height);

        for (int y = 0; y < patch.Height; y++)
        {
            int sourceY = Clamp(area.SourceY + y, 0, baseImage.Height - 1);
            for (int x = 0; x < patch.Width; x++)
            {
                int sourceX = Clamp(area.SourceX + x, 0, baseImage.Width - 1);
                patch[x, y] = baseImage[sourceX, sourceY];
            }
        }

        using var memory = new MemoryStream();
        patch.Save(memory, new PngEncoder());
        memory.Position = 0;
        area.ImageControl.Source = new Bitmap(memory);
    }

    private void UpdateTextBitmap(UiTextItem item)
    {
        if (_font is null)
        {
            item.ImageControl.Source = null;
            UpdateControlPosition(item, 0);
            return;
        }

        using SixLabors.ImageSharp.Image<Rgba32> rendered = RenderItem(item);
        item.RenderedWidth = rendered.Width;
        item.RenderedHeight = rendered.Height;

        using var memory = new MemoryStream();
        rendered.Save(memory, new PngEncoder());
        memory.Position = 0;
        item.ImageControl.Source = new Bitmap(memory);
        UpdateControlPosition(item, item.RenderedWidth);
    }

    private SixLabors.ImageSharp.Image<Rgba32> RenderItem(UiTextItem item)
    {
        if (_font is null) throw new InvalidOperationException("AAF font is not loaded.");

        var renderer = new AafTextRenderer(_palette);
        SixLabors.ImageSharp.Image<Rgba32> image = renderer.RenderText(_font, item.Text, new AafTextRenderOptions
        {
            Scale = item.Scale,
            LetterSpacing = item.LetterSpacing,
            LineSpacing = 0,
            ForceUppercase = item.ForceUppercase
        });

        int newWidth = Math.Max(1, (int)Math.Round(image.Width * item.WidthScale));
        int newHeight = Math.Max(1, (int)Math.Round(image.Height * item.HeightScale));

        if (newWidth != image.Width || newHeight != image.Height)
        {
            image.Mutate(context => context.Resize(newWidth, newHeight, KnownResamplers.NearestNeighbor));
        }

        return image;
    }

    private void UpdateControlPosition(UiTextItem item, int renderedWidth)
    {
        int imageX = GetAlignedX(item, renderedWidth);
        Canvas.SetLeft(item.ImageControl, imageX);
        Canvas.SetTop(item.ImageControl, item.Y);

        int boxWidth = Math.Max(1, item.Width > 0 ? item.Width : item.RenderedWidth);
        int boxHeight = Math.Max(1, item.RenderedHeight);

        item.SelectionBorder.Width = boxWidth;
        item.SelectionBorder.Height = boxHeight;
        Canvas.SetLeft(item.SelectionBorder, item.X);
        Canvas.SetTop(item.SelectionBorder, item.Y);

        Canvas.SetLeft(item.WidthHandle, item.X + boxWidth - ResizeHandleSize / 2);
        Canvas.SetTop(item.WidthHandle, item.Y + boxHeight / 2.0 - ResizeHandleSize / 2);

        Canvas.SetLeft(item.ScaleHandle, item.X + boxWidth - ResizeHandleSize / 2);
        Canvas.SetTop(item.ScaleHandle, item.Y + boxHeight - ResizeHandleSize / 2);

        UpdateSelectionVisuals();
    }

    private static int GetAlignedX(UiTextItem item, int renderedWidth)
    {
        if (item.Width <= 0) return item.X;

        return item.Align.ToLowerInvariant() switch
        {
            "center" => item.X + Math.Max(0, (item.Width - renderedWidth) / 2),
            "right" => item.X + Math.Max(0, item.Width - renderedWidth),
            _ => item.X
        };
    }

    private void StartEraseMove(EraseArea area, PointerPressedEventArgs e)
    {
        SelectEraseArea(area);
        _dragMode = DragMode.MoveErase;
        _dragStartCanvas = e.GetPosition(_canvas);
        _dragOffset = e.GetPosition(area.ImageControl);
        e.Pointer.Capture(area.ImageControl);
        e.Handled = true;
    }

    private void ContinueEraseMove(EraseArea area, PointerEventArgs e)
    {
        if (_dragMode != DragMode.MoveErase || _selectedErase != area || e.Pointer.Captured != area.ImageControl)
        {
            return;
        }

        AvaloniaPoint canvasPosition = e.GetPosition(_canvas);
        area.X = Math.Max(0, (int)Math.Round(canvasPosition.X - _dragOffset.X));
        area.Y = Math.Max(0, (int)Math.Round(canvasPosition.Y - _dragOffset.Y));
        SyncEraseEditorFields(area);
        UpdateEraseVisual(area);
        e.Handled = true;
    }

    private void StartEraseSourceMove(EraseArea area, PointerPressedEventArgs e)
    {
        SelectEraseArea(area);
        _dragMode = DragMode.MoveEraseSource;
        _dragOffset = e.GetPosition(area.SourceBorder);
        e.Pointer.Capture(area.SourceBorder);
        SetStatus($"Dragging erase source: {area.Name}");
        e.Handled = true;
    }

    private void ContinueEraseSourceMove(EraseArea area, PointerEventArgs e)
    {
        if (_dragMode != DragMode.MoveEraseSource || _selectedErase != area || e.Pointer.Captured != area.SourceBorder)
        {
            return;
        }

        AvaloniaPoint canvasPosition = e.GetPosition(_canvas);
        int newSourceX = (int)Math.Round(canvasPosition.X - _dragOffset.X);
        int newSourceY = (int)Math.Round(canvasPosition.Y - _dragOffset.Y);

        if (_baseBitmap is not null)
        {
            newSourceX = Clamp(newSourceX, 0, Math.Max(0, _baseBitmap.PixelSize.Width - area.Width));
            newSourceY = Clamp(newSourceY, 0, Math.Max(0, _baseBitmap.PixelSize.Height - area.Height));
        }
        else
        {
            newSourceX = Math.Max(0, newSourceX);
            newSourceY = Math.Max(0, newSourceY);
        }

        area.SourceX = newSourceX;
        area.SourceY = newSourceY;
        SyncEraseEditorFields(area);
        UpdateEraseVisual(area);
        e.Handled = true;
    }

    private void StartEraseResize(EraseArea area, PointerPressedEventArgs e)
    {
        SelectEraseArea(area);
        _dragMode = DragMode.ResizeErase;
        _dragStartCanvas = e.GetPosition(_canvas);
        _dragStartWidth = area.Width;
        _dragStartHeight = area.Height;
        e.Pointer.Capture(area.ResizeHandle);
        e.Handled = true;
    }

    private void ContinueEraseResize(EraseArea area, PointerEventArgs e)
    {
        if (_dragMode != DragMode.ResizeErase || _selectedErase != area || e.Pointer.Captured != area.ResizeHandle)
        {
            return;
        }

        AvaloniaPoint canvasPosition = e.GetPosition(_canvas);
        area.Width = Math.Max(1, (int)Math.Round(_dragStartWidth + canvasPosition.X - _dragStartCanvas.X));
        area.Height = Math.Max(1, (int)Math.Round(_dragStartHeight + canvasPosition.Y - _dragStartCanvas.Y));
        SyncEraseEditorFields(area);
        UpdateEraseVisual(area);
        e.Handled = true;
    }

    private void StartDrag(UiTextItem item, PointerPressedEventArgs e)
    {
        SelectItem(item);
        _dragMode = DragMode.Move;
        _dragItem = item;
        _dragOffset = e.GetPosition(item.ImageControl);
        e.Pointer.Capture(item.ImageControl);
        e.Handled = true;
    }

    private void ContinueDrag(UiTextItem item, PointerEventArgs e)
    {
        if (_dragMode != DragMode.Move || _dragItem != item || e.Pointer.Captured != item.ImageControl)
        {
            return;
        }

        AvaloniaPoint canvasPosition = e.GetPosition(_canvas);
        item.X = Math.Max(0, (int)Math.Round(canvasPosition.X - _dragOffset.X));
        item.Y = Math.Max(0, (int)Math.Round(canvasPosition.Y - _dragOffset.Y));
        SyncEditorFields(item);
        UpdateControlPosition(item, item.RenderedWidth);
        e.Handled = true;
    }

    private void StartResizeWidth(UiTextItem item, PointerPressedEventArgs e)
    {
        SelectItem(item);
        _dragMode = DragMode.ResizeWidth;
        _dragItem = item;
        _dragStartCanvas = e.GetPosition(_canvas);
        _dragStartWidth = Math.Max(1, item.Width > 0 ? item.Width : item.RenderedWidth);
        e.Pointer.Capture(item.WidthHandle);
        e.Handled = true;
    }

    private void StartResizeScale(UiTextItem item, PointerPressedEventArgs e)
    {
        SelectItem(item);
        _dragMode = DragMode.ResizeScale;
        _dragItem = item;
        _dragStartCanvas = e.GetPosition(_canvas);
        _dragStartRenderedWidth = Math.Max(1, item.RenderedWidth);
        _dragStartRenderedHeight = Math.Max(1, item.RenderedHeight);
        _dragStartWidthScale = item.WidthScale;
        _dragStartHeightScale = item.HeightScale;
        e.Pointer.Capture(item.ScaleHandle);
        e.Handled = true;
    }

    private void ContinueResize(UiTextItem item, PointerEventArgs e)
    {
        if (_dragItem != item)
        {
            return;
        }

        if (_dragMode == DragMode.ResizeWidth && e.Pointer.Captured == item.WidthHandle)
        {
            AvaloniaPoint canvasPosition = e.GetPosition(_canvas);
            int newWidth = Math.Max(1, (int)Math.Round(_dragStartWidth + canvasPosition.X - _dragStartCanvas.X));
            item.Width = newWidth;
            SyncEditorFields(item);
            UpdateControlPosition(item, item.RenderedWidth);
            e.Handled = true;
            return;
        }

        if (_dragMode == DragMode.ResizeScale && e.Pointer.Captured == item.ScaleHandle)
        {
            AvaloniaPoint canvasPosition = e.GetPosition(_canvas);
            int newWidth = Math.Max(1, (int)Math.Round(_dragStartRenderedWidth + canvasPosition.X - _dragStartCanvas.X));
            int newHeight = Math.Max(1, (int)Math.Round(_dragStartRenderedHeight + canvasPosition.Y - _dragStartCanvas.Y));

            item.WidthScale = Math.Max(0.1, _dragStartWidthScale * newWidth / Math.Max(1, _dragStartRenderedWidth));
            item.HeightScale = Math.Max(0.1, _dragStartHeightScale * newHeight / Math.Max(1, _dragStartRenderedHeight));

            UpdateTextBitmap(item);
            SyncEditorFields(item);
            e.Handled = true;
        }
    }

    private void EndDrag(PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
        _dragMode = DragMode.None;
        _dragItem = null;
        e.Handled = true;
    }

    private static AvaloniaBorder CreateSelectionBorder()
    {
        return new AvaloniaBorder
        {
            BorderBrush = Brushes.Cyan,
            BorderThickness = new Thickness(1),
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            IsVisible = false
        };
    }

    private static AvaloniaBorder CreateEraseTargetBorder()
    {
        return new AvaloniaBorder
        {
            BorderBrush = Brushes.Orange,
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(48, 255, 128, 0)),
            IsHitTestVisible = false,
            IsVisible = false
        };
    }

    private static AvaloniaBorder CreateEraseSourceBorder()
    {
        return new AvaloniaBorder
        {
            BorderBrush = Brushes.Lime,
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(28, 0, 255, 0)),
            IsHitTestVisible = true,
            IsVisible = false
        };
    }

    private static AvaloniaBorder CreateResizeHandle()
    {
        return new AvaloniaBorder
        {
            Width = ResizeHandleSize,
            Height = ResizeHandleSize,
            Background = Brushes.Cyan,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            IsVisible = false
        };
    }

    private void UpdateSelectionVisuals()
    {
        foreach (UiTextItem item in _items)
        {
            bool isSelected = ReferenceEquals(item, _selectedItem);
            item.SelectionBorder.IsVisible = isSelected;
            item.WidthHandle.IsVisible = isSelected;
            item.ScaleHandle.IsVisible = isSelected;
        }

        foreach (EraseArea area in _eraseAreas)
        {
            bool isSelected = ReferenceEquals(area, _selectedErase);
            area.TargetBorder.IsVisible = isSelected;
            area.SourceBorder.IsVisible = isSelected;
            area.ResizeHandle.IsVisible = isSelected;
        }
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (_selectedItem is null && _selectedErase is null)
        {
            return;
        }

        int step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
        int dx = 0;
        int dy = 0;

        switch (e.Key)
        {
            case Key.Left:
                dx = -step;
                break;
            case Key.Right:
                dx = step;
                break;
            case Key.Up:
                dy = -step;
                break;
            case Key.Down:
                dy = step;
                break;
            default:
                return;
        }

        if (_selectedItem is not null)
        {
            _selectedItem.X = Math.Max(0, _selectedItem.X + dx);
            _selectedItem.Y = Math.Max(0, _selectedItem.Y + dy);
            SyncEditorFields(_selectedItem);
            UpdateControlPosition(_selectedItem, _selectedItem.RenderedWidth);
        }
        else if (_selectedErase is not null)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                _selectedErase.SourceX = Math.Max(0, _selectedErase.SourceX + dx);
                _selectedErase.SourceY = Math.Max(0, _selectedErase.SourceY + dy);
            }
            else
            {
                _selectedErase.X = Math.Max(0, _selectedErase.X + dx);
                _selectedErase.Y = Math.Max(0, _selectedErase.Y + dy);
            }

            SyncEraseEditorFields(_selectedErase);
            UpdateEraseVisual(_selectedErase);
        }

        e.Handled = true;
    }

    private static void ApplyErasePatch(SixLabors.ImageSharp.Image<Rgba32> destination, SixLabors.ImageSharp.Image<Rgba32> source, EraseArea area)
    {
        for (int y = 0; y < area.Height; y++)
        {
            int targetY = area.Y + y;
            if (targetY < 0 || targetY >= destination.Height) continue;

            int sourceY = Clamp(area.SourceY + y, 0, source.Height - 1);
            for (int x = 0; x < area.Width; x++)
            {
                int targetX = area.X + x;
                if (targetX < 0 || targetX >= destination.Width) continue;

                int sourceX = Clamp(area.SourceX + x, 0, source.Width - 1);
                destination[targetX, targetY] = source[sourceX, sourceY];
            }
        }
    }

    private static void Composite(SixLabors.ImageSharp.Image<Rgba32> destination, SixLabors.ImageSharp.Image<Rgba32> source, int offsetX, int offsetY)
    {
        for (int y = 0; y < source.Height; y++)
        {
            int targetY = offsetY + y;
            if (targetY < 0 || targetY >= destination.Height) continue;

            for (int x = 0; x < source.Width; x++)
            {
                int targetX = offsetX + x;
                if (targetX < 0 || targetX >= destination.Width) continue;

                Rgba32 pixel = source[x, y];
                if (pixel.A == 0) continue;

                destination[targetX, targetY] = pixel;
            }
        }
    }

    private static Rgba32[] LoadActPalette(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < 256 * 3)
        {
            throw new InvalidDataException("ACT palette must contain at least 768 bytes.");
        }

        var palette = new Rgba32[256];
        for (int i = 0; i < palette.Length; i++)
        {
            int offset = i * 3;
            palette[i] = new Rgba32(data[offset], data[offset + 1], data[offset + 2], 255);
        }

        return palette;
    }

    private static void SaveIndexedBmp8(string path, SixLabors.ImageSharp.Image<Rgba32> image, IReadOnlyList<Rgba32> palette)
    {
        if (palette.Count < 256)
        {
            throw new ArgumentException("The export palette must contain 256 colors.", nameof(palette));
        }

        int width = image.Width;
        int height = image.Height;
        int stride = ((width + 3) / 4) * 4;
        int pixelDataSize = stride * height;
        const int fileHeaderSize = 14;
        const int infoHeaderSize = 40;
        const int paletteSize = 256 * 4;
        int pixelDataOffset = fileHeaderSize + infoHeaderSize + paletteSize;
        int fileSize = pixelDataOffset + pixelDataSize;

        var indexCache = new Dictionary<uint, byte>();
        byte[] row = new byte[stride];

        using FileStream stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(pixelDataOffset);

        writer.Write(infoHeaderSize);
        writer.Write(width);
        writer.Write(height);
        writer.Write((ushort)1);
        writer.Write((ushort)8);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(256);
        writer.Write(0);

        for (int i = 0; i < 256; i++)
        {
            Rgba32 color = palette[i];
            writer.Write(color.B);
            writer.Write(color.G);
            writer.Write(color.R);
            writer.Write((byte)0);
        }

        for (int y = height - 1; y >= 0; y--)
        {
            Array.Clear(row, 0, row.Length);
            for (int x = 0; x < width; x++)
            {
                Rgba32 pixel = image[x, y];
                row[x] = FindNearestPaletteIndex(pixel, palette, indexCache);
            }

            writer.Write(row);
        }
    }

    private static byte FindNearestPaletteIndex(Rgba32 pixel, IReadOnlyList<Rgba32> palette, Dictionary<uint, byte> cache)
    {
        uint key = ((uint)pixel.R << 16) | ((uint)pixel.G << 8) | pixel.B;
        if (cache.TryGetValue(key, out byte cached))
        {
            return cached;
        }

        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < 256; i++)
        {
            Rgba32 color = palette[i];
            int dr = pixel.R - color.R;
            int dg = pixel.G - color.G;
            int db = pixel.B - color.B;
            int distance = dr * dr + dg * dg + db * db;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;

                if (distance == 0)
                {
                    break;
                }
            }
        }

        byte result = (byte)bestIndex;
        cache[key] = result;
        return result;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (max < min) return min;
        return Math.Min(max, Math.Max(min, value));
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : fallback;
    }

    private static double ParseDouble(string? value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        string normalized = value.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : fallback;
    }

    private static string SerializeEraseLayoutLine(EraseArea area)
    {
        return string.Join('|',
            "ERASE",
            area.Name,
            area.X.ToString(CultureInfo.InvariantCulture),
            area.Y.ToString(CultureInfo.InvariantCulture),
            area.Width.ToString(CultureInfo.InvariantCulture),
            area.Height.ToString(CultureInfo.InvariantCulture),
            area.SourceX.ToString(CultureInfo.InvariantCulture),
            area.SourceY.ToString(CultureInfo.InvariantCulture));
    }

    private static string SerializeLayoutLine(UiTextItem item)
    {
        return string.Join('|',
            item.Name,
            item.X.ToString(CultureInfo.InvariantCulture),
            item.Y.ToString(CultureInfo.InvariantCulture),
            item.Width.ToString(CultureInfo.InvariantCulture),
            item.Align,
            item.Scale.ToString(CultureInfo.InvariantCulture),
            item.WidthScale.ToString("0.###", CultureInfo.InvariantCulture),
            item.HeightScale.ToString("0.###", CultureInfo.InvariantCulture),
            item.LetterSpacing.ToString(CultureInfo.InvariantCulture),
            item.ForceUppercase ? "uppercase" : "normal",
            item.Text);
    }

    private static void SetZIndex(Control control, int zIndex)
    {
        control.ZIndex = zIndex;
    }

    private void SetStatus(string message)
    {
        _status.Text = message;
    }

    private sealed class UiProjectDocument
    {
        public int Version { get; set; } = 1;
        public string? BaseImagePath { get; set; }
        public string? AafFontPath { get; set; }
        public string? ActPalettePath { get; set; }
        public List<UiTextData> Texts { get; set; } = new();
        public List<EraseAreaData> EraseAreas { get; set; } = new();
    }

    private sealed class UiTextData
    {
        public string Name { get; set; } = "TEXT";
        public string Text { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public string Align { get; set; } = "left";
        public int Scale { get; set; } = 1;
        public double WidthScale { get; set; } = 1.0;
        public double HeightScale { get; set; } = 1.0;
        public int LetterSpacing { get; set; }
        public bool ForceUppercase { get; set; }
    }

    private sealed class EraseAreaData
    {
        public string Name { get; set; } = "ERASE";
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 40;
        public int Height { get; set; } = 16;
        public int SourceX { get; set; }
        public int SourceY { get; set; }
    }

    private enum DragMode
    {
        None,
        Move,
        ResizeWidth,
        ResizeScale,
        MoveErase,
        MoveEraseSource,
        ResizeErase
    }

    private sealed class EraseArea
    {
        public string Name { get; set; } = "ERASE";
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 40;
        public int Height { get; set; } = 16;
        public int SourceX { get; set; }
        public int SourceY { get; set; }
        public required AvaloniaImage ImageControl { get; init; }
        public required AvaloniaBorder TargetBorder { get; init; }
        public required AvaloniaBorder SourceBorder { get; init; }
        public required AvaloniaBorder ResizeHandle { get; init; }

        public override string ToString()
        {
            return $"{Name}: target {X},{Y} source {SourceX},{SourceY}";
        }
    }

    private sealed class UiTextItem
    {
        public string Name { get; set; } = "TEXT";
        public string Text { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public string Align { get; set; } = "left";
        public int Scale { get; set; } = 1;
        public double WidthScale { get; set; } = 1.0;
        public double HeightScale { get; set; } = 1.0;
        public int LetterSpacing { get; set; }
        public bool ForceUppercase { get; set; }
        public int RenderedWidth { get; set; }
        public int RenderedHeight { get; set; }
        public required AvaloniaImage ImageControl { get; init; }
        public required AvaloniaBorder SelectionBorder { get; init; }
        public required AvaloniaBorder WidthHandle { get; init; }
        public required AvaloniaBorder ScaleHandle { get; init; }

        public override string ToString()
        {
            return $"{Name}: {Text}";
        }
    }
}
