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
    private readonly TextBox _widthScaleBox = new() { Text = "1.0" };
    private readonly TextBox _heightScaleBox = new() { Text = "1.0" };
    private readonly TextBox _letterSpacingBox = new() { Text = "0" };
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
    private DragMode _dragMode = DragMode.None;
    private UiTextItem? _dragItem;
    private AvaloniaPoint _dragStartCanvas;
    private int _dragStartWidth;
    private int _dragStartRenderedWidth;
    private int _dragStartRenderedHeight;
    private double _dragStartWidthScale;
    private double _dragStartHeightScale;

    private const double ResizeHandleSize = 8;

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
        panel.Children.Add(Labeled("Scale (integer)", _scaleBox));
        panel.Children.Add(Labeled("Width scale", _widthScaleBox));
        panel.Children.Add(Labeled("Height scale", _heightScaleBox));
        panel.Children.Add(Labeled("Letter spacing", _letterSpacingBox));
        panel.Children.Add(Labeled("Align", _alignBox));
        panel.Children.Add(_uppercaseBox);
        panel.Children.Add(MakeButton("Apply changes", _ => ApplyEditorFieldsToSelected()));

        panel.Children.Add(new Separator());
        panel.Children.Add(new TextBlock
        {
            Text = "Tip: drag text to move it. Drag the right handle to change the layout box width. Drag the bottom-right handle to resize the rendered text. Arrow keys move the selected text by 1 pixel; Shift+arrows move by 10 pixels.",
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

    private void SelectItem(UiTextItem item)
    {
        _selectedItem = item;
        SyncEditorFields(item);
        UpdateSelectionVisuals();
        Focus();
        SetStatus($"Selected: {item.Name}");
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
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (_selectedItem is null)
        {
            return;
        }

        int step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
        bool handled = true;

        switch (e.Key)
        {
            case Key.Left:
                _selectedItem.X = Math.Max(0, _selectedItem.X - step);
                break;
            case Key.Right:
                _selectedItem.X += step;
                break;
            case Key.Up:
                _selectedItem.Y = Math.Max(0, _selectedItem.Y - step);
                break;
            case Key.Down:
                _selectedItem.Y += step;
                break;
            default:
                handled = false;
                break;
        }

        if (!handled)
        {
            return;
        }

        SyncEditorFields(_selectedItem);
        UpdateControlPosition(_selectedItem, _selectedItem.RenderedWidth);
        e.Handled = true;
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
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : fallback;
    }

    private static double ParseDouble(string? value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        string normalized = value.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : fallback;
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

    private void SetStatus(string message)
    {
        _status.Text = message;
    }

    private enum DragMode
    {
        None,
        Move,
        ResizeWidth,
        ResizeScale
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
