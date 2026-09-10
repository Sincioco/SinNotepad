using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SinNotepad.Core;

namespace SinNotepad;

internal static class TextBoxExtensions
{
    public static void ClearUndo(this TextBox editor) { editor.IsUndoEnabled = false; editor.IsUndoEnabled = true; }
}

public sealed class EditorView : Grid
{
    public TextBox Editor { get; }
    public LineNumberGutter Gutter { get; }
    public Document Document { get; }
    public EditorView(Document doc)
    {
        Document = doc;
        SetResourceReference(BackgroundProperty, "EditorBrush");
        ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        Editor = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            IsUndoEnabled = true,
            UndoLimit = 1000,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11.0 * 96 / 72 * doc.Zoom / 100,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 12, 12, 12),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Text = doc.Text,
            TextWrapping = App.Current.Preferences.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            SelectionOpacity = 0.45,
        };
        // Use the standard native WPF text surface, with no Fluent focus border or formatting features.
        var template = new ControlTemplate(typeof(TextBox));
        var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
        scroll.Name = "PART_ContentHost";
        scroll.SetValue(ScrollViewer.CanContentScrollProperty, false);
        template.VisualTree = scroll;
        Editor.Template = template;
        Editor.SetResourceReference(Control.BackgroundProperty, "EditorBrush");
        Editor.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        Editor.SetResourceReference(TextBox.CaretBrushProperty, "TextBrush");
        Editor.SetResourceReference(TextBox.SelectionBrushProperty, "AccentBrush");
        AutomationProperties.SetName(Editor, "Text editor");
        Editor.Language = System.Windows.Markup.XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
        SpellCheck.SetIsEnabled(Editor, false);
        SetColumn(Editor, 1); Children.Add(Editor);
        Gutter = new LineNumberGutter(Editor);
        Children.Add(Gutter);
        Editor.TextChanged += (_, _) => { doc.Text = TextFiles.Normalize(Editor.Text); doc.Notify(); Gutter.Rebuild(); App.Current.MarkChanged(); };
        Editor.SelectionChanged += (_, _) => { doc.Caret = TextFiles.ToNormalizedOffset(Editor.Text, Editor.SelectionStart); doc.SelectionLength = TextFiles.ToNormalizedOffset(Editor.Text, Editor.SelectionStart + Editor.SelectionLength) - doc.Caret; };
        Editor.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((_, _) => { Gutter.InvalidateVisual(); doc.Scroll = Editor.VerticalOffset; doc.HorizontalScroll = Editor.HorizontalOffset; }));
        Editor.SizeChanged += (_, _) => Gutter.InvalidateVisual();
        Loaded += (_, _) =>
        {
            int start = TextFiles.FromNormalizedOffset(Editor.Text, doc.Caret);
            int end = TextFiles.FromNormalizedOffset(Editor.Text, doc.Caret + doc.SelectionLength);
            Editor.Select(start, end - start);
            Editor.ScrollToVerticalOffset(doc.Scroll); Editor.ScrollToHorizontalOffset(doc.HorizontalScroll); Gutter.Rebuild();
        };
        Editor.ClearUndo();
    }
    public void ApplyPreferences()
    {
        Editor.TextWrapping = App.Current.Preferences.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        Editor.HorizontalScrollBarVisibility = App.Current.Preferences.WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        Editor.FontSize = 11.0 * 96 / 72 * Document.Zoom / 100;
        Gutter.Rebuild();
    }
}

public sealed class LineNumberGutter : FrameworkElement
{
    readonly TextBox editor;
    public List<int> LineStarts { get; private set; } = [0];
    public int LineCount => LineStarts.Count;
    public LineNumberGutter(TextBox editor)
    {
        this.editor = editor; ClipToBounds = true; Focusable = false;
        AutomationProperties.SetName(this, "Line numbers");
        IsHitTestVisible = false;
        Rebuild();
    }
    public void Rebuild()
    {
        var starts = new List<int> { 0 }; string text = editor.Text;
        for (int i = 0; i < text.Length; i++) { if (text[i] == '\r') { if (i + 1 < text.Length && text[i + 1] == '\n') i++; starts.Add(i + 1); } else if (text[i] == '\n') starts.Add(i + 1); }
        LineStarts = starts;
        Width = Math.Max(42, Math.Ceiling(editor.FontSize * 0.62 * Math.Max(2, starts.Count.ToString().Length) + 22));
        InvalidateVisual();
    }
    public (int Line, int Column) Position(int index)
    {
        int line = LineStarts.BinarySearch(index); if (line < 0) line = ~line - 1;
        return (line + 1, index - LineStarts[Math.Max(0, line)] + 1);
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var background = (Brush)FindResource("EditorBrush");
        var foreground = (Brush)FindResource("MutedBrush");
        dc.DrawRectangle(background, null, new Rect(0, 0, ActualWidth, ActualHeight));
        dc.DrawLine(new Pen((Brush)FindResource("LineBrush"), 1), new Point(ActualWidth - 0.5, 0), new Point(ActualWidth - 0.5, ActualHeight));
        if (!editor.IsLoaded || editor.ActualHeight <= 0) return;
        int index = editor.GetCharacterIndexFromPoint(new Point(editor.Padding.Left + 1, 1), true);
        int first = LineStarts.BinarySearch(Math.Max(0, index)); if (first < 0) first = ~first - 1;
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        // Only draw the logical lines intersecting the viewport. Wrapped continuations have no duplicate number.
        for (int line = Math.Max(0, first); line < LineStarts.Count; line++)
        {
            Rect rect = editor.GetRectFromCharacterIndex(LineStarts[line], true);
            if (rect.IsEmpty) continue;
            if (rect.Y > ActualHeight) break;
            if (rect.Bottom < 0) continue;
            var label = new FormattedText((line + 1).ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(editor.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), editor.FontSize, foreground, dpi);
            dc.DrawText(label, new Point(ActualWidth - label.Width - 10, rect.Top));
        }
    }
}
