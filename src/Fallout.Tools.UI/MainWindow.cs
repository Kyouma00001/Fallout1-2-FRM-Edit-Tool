using AvaloniaImage = Avalonia.Controls.Image;
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

namespace Fallout.Tools.UI;

public sealed class MainWindow : Window
{
    private readonly Canvas _canvas = new();
    private readonly AvaloniaImage _baseImage = new();
    private readonly ListBox _itemsList = new();
    private readonly TextBox _nameBox = new() { Text = "TEXT01" };
    private readonly TextBox _textBox = new() { Text = "negociar" };
    private readonly TextBox _xBox = new() { Text = "10" };
    private readonly TextBox _yBox = new() { Text = "10" };
    private readonly TextBox _widthBox = new() { Text = "0" };
    private readonly TextBox _scaleBox = new() { Text = "1" };
    private readonly ComboBox _alignBox = new();
    private readonly CheckBox _uppercaseBox = new() { Content = "Uppercase", IsChecked = true };
    private readonly TextBlock _status = new() { Text = "Open a base image and an AAF font to start." };

    private readonly List<UiTextItem> _items = new();
    private string? _baseImagePath;
    private Bitmap? _baseBitmap;
    private AafFont? _font;
    private string? _fontPath;
    private AafRenderPalette _palette = AafRenderPalette.Create(AafPaletteKind.Orange, string.Empty);
    private UiTextItem? _selectedItem;
    private AvaloniaPoint _dragOffset;

    public MainWindow()
    {
        Title = "Fallout UI Text Layout Editor";
        Width = 1200;
        Height = 760;
        MinWidth = 900;
        MinHeight = 600;

        _alignBox.ItemsSource = new[] { "left", "center", "right" };
        _alignBox.SelectedIndex = 0;

        Content = BuildLayout();
        _baseImage.Stretch = Stretch.None;
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
        toolbar.Children.Add(MakeButton("Add text", _ => AddTextItem()));
        toolbar.Children.Add(MakeButton("Remove", _ => RemoveSelected()));
        toolbar.Children.Add(MakeButton("Save layout", OnSaveLayoutAsync));
        toolbar.Children.Add(MakeButton("Export PNG", OnExportPngAsync));

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
        panel.Children.Add(Labeled("Scale", _scaleBox));
        panel.Children.Add(Labeled("Align", _alignBox));
        panel.Children.Add(_uppercaseBox);
        panel.Children.Add(MakeButton("Apply changes", _ => ApplyEditorFieldsToSelected()));

        panel.Children.Add(new Separator());
        panel.Children.Add(new TextBlock
        {
            Text = "Tip: drag text with the mouse. X/Y are the box position. Width + center/right control alignment inside that box.",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(_status);

        return panel;
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

        _baseImagePath = path;
        await using FileStream stream = File.OpenRead(path);
        _baseBitmap = new Bitmap(stream);
        _baseImage.Source = _baseBitmap;
        _canvas.Width = _baseBitmap.PixelSize.Width;
        _canvas.Height = _baseBitmap.PixelSize.Height;
        Canvas.SetLeft(_baseImage, 0);
        Canvas.SetTop(_baseImage, 0);

        SetStatus($"Opened image: {Path.GetFileName(path)} ({_baseBitmap.PixelSize.Width}x{_baseBitmap.PixelSize.Height})");
    }

    private async void OnOpenAafAsync(RoutedEventArgs _)
    {
        string? path = await PickOpenFileAsync("Open AAF font", new[] { "*.aaf" });
        if (path is null) return;

        _fontPath = path;
        _font = new AafReader().Read(path);
        _palette = AafRenderPalette.Create(AafPaletteKind.Orange, path);
        SetStatus($"Opened font: {Path.GetFileName(path)}");

        foreach (UiTextItem item in _items)
        {
            UpdateTextBitmap(item);
        }
    }

    private async void OnSaveLayoutAsync(RoutedEventArgs _)
    {
        string? path = await PickSaveFileAsync("Save UI layout", "ui-layout.txt", new[] { "*.txt" });
        if (path is null) return;

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
        File.WriteAllLines(path, _items.Select(SerializeLayoutLine));
        SetStatus($"Saved layout: {path}");
    }

    private async void OnExportPngAsync(RoutedEventArgs _)
    {
        if (_baseImagePath is null)
        {
            SetStatus("Open a base image first.");
            return;
        }

        if (_font is null)
        {
            SetStatus("Open an AAF font first.");
            return;
        }

        string? path = await PickSaveFileAsync("Export composed PNG", "ui-composed.png", new[] { "*.png" });
        if (path is null) return;

        using SixLabors.ImageSharp.Image<Rgba32> output = SixLabors.ImageSharp.Image.Load<Rgba32>(_baseImagePath);

        foreach (UiTextItem item in _items)
        {
            using SixLabors.ImageSharp.Image<Rgba32> textImage = RenderItem(item);
            int drawX = GetAlignedX(item, textImage.Width);
            Composite(output, textImage, drawX, item.Y);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
        await using FileStream stream = File.Create(path);
        output.Save(stream, new PngEncoder());
        SetStatus($"Exported PNG: {path}");
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
            Align = (_alignBox.SelectedItem as string) ?? "left",
            ForceUppercase = _uppercaseBox.IsChecked == true,
            ImageControl = new AvaloniaImage { Stretch = Stretch.None }
        };

        item.ImageControl.PointerPressed += (_, e) => StartDrag(item, e);
        item.ImageControl.PointerMoved += (_, e) => ContinueDrag(item, e);
        item.ImageControl.PointerReleased += (_, e) => EndDrag(e);

        _items.Add(item);
        _canvas.Children.Add(item.ImageControl);
        _itemsList.ItemsSource = null;
        _itemsList.ItemsSource = _items;
        _itemsList.SelectedItem = item;
        UpdateTextBitmap(item);
        SelectItem(item);
    }

    private void RemoveSelected()
    {
        if (_selectedItem is null) return;

        _canvas.Children.Remove(_selectedItem.ImageControl);
        _items.Remove(_selectedItem);
        _selectedItem = null;
        _itemsList.ItemsSource = null;
        _itemsList.ItemsSource = _items;
        SetStatus("Removed selected text item.");
    }

    private void SelectItem(UiTextItem item)
    {
        _selectedItem = item;
        _nameBox.Text = item.Name;
        _textBox.Text = item.Text;
        _xBox.Text = item.X.ToString();
        _yBox.Text = item.Y.ToString();
        _widthBox.Text = item.Width.ToString();
        _scaleBox.Text = item.Scale.ToString();
        _alignBox.SelectedItem = item.Align;
        _uppercaseBox.IsChecked = item.ForceUppercase;
        SetStatus($"Selected: {item.Name}");
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
        _selectedItem.Align = (_alignBox.SelectedItem as string) ?? "left";
        _selectedItem.ForceUppercase = _uppercaseBox.IsChecked == true;

        UpdateTextBitmap(_selectedItem);
        _itemsList.ItemsSource = null;
        _itemsList.ItemsSource = _items;
        _itemsList.SelectedItem = _selectedItem;
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
        return renderer.RenderText(_font, item.Text, new AafTextRenderOptions
        {
            Scale = item.Scale,
            LetterSpacing = 0,
            LineSpacing = 0,
            ForceUppercase = item.ForceUppercase
        });
    }

    private void UpdateControlPosition(UiTextItem item, int renderedWidth)
    {
        int x = GetAlignedX(item, renderedWidth);
        Canvas.SetLeft(item.ImageControl, x);
        Canvas.SetTop(item.ImageControl, item.Y);
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

    private void StartDrag(UiTextItem item, PointerPressedEventArgs e)
    {
        SelectItem(item);
        _dragOffset = e.GetPosition(item.ImageControl);
        e.Pointer.Capture(item.ImageControl);
    }

    private void ContinueDrag(UiTextItem item, PointerEventArgs e)
    {
        if (e.Pointer.Captured != item.ImageControl) return;

        AvaloniaPoint canvasPosition = e.GetPosition(_canvas);
        item.X = Math.Max(0, (int)Math.Round(canvasPosition.X - _dragOffset.X));
        item.Y = Math.Max(0, (int)Math.Round(canvasPosition.Y - _dragOffset.Y));
        _xBox.Text = item.X.ToString();
        _yBox.Text = item.Y.ToString();
        UpdateControlPosition(item, item.RenderedWidth);
    }

    private static void EndDrag(PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
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

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, out int result) ? result : fallback;
    }

    private static string SerializeLayoutLine(UiTextItem item)
    {
        return $"{item.Name}|{item.X}|{item.Y}|{item.Width}|{item.Align}|{item.Text}";
    }

    private void SetStatus(string message)
    {
        _status.Text = message;
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
        public bool ForceUppercase { get; set; }
        public int RenderedWidth { get; set; }
        public int RenderedHeight { get; set; }
        public required AvaloniaImage ImageControl { get; init; }

        public override string ToString()
        {
            return $"{Name}: {Text}";
        }
    }
}
