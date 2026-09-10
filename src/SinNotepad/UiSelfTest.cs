using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SinNotepad.Core;

namespace SinNotepad;

internal static class UiSelfTest
{
    public static async Task Run(App app)
    {
        var results = new List<string>();
        string report = Path.Combine(app.Store.DirectoryPath, "ui-test-results.txt");
        try
        {
            void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); results.Add("PASS " + name); }
            app.Preferences.NextDocumentNumber = 1;
            app.Preferences.AutoSaveDirectory = "";
            var window = new MainWindow(); window.Show();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            foreach (MenuItem item in window.MainMenu.Items)
            {
                results.Add($"INFO menu {item.Header}: {item.ActualWidth} × {item.ActualHeight}, visible={item.IsVisible}, foreground={item.Foreground}");
                Check(item.ActualHeight > 10 && item.ActualWidth > 20, $"Menu {item.Header} has visible layout bounds");
            }
            Capture(window, Path.Combine(app.Store.DirectoryPath, "horizontal.png"));
            Check(Descendants(window.MainMenu).OfType<AccessText>().All(t => t.ActualHeight >= 16), "Menu labels have enough height to render");
            Check(window.ActiveDocument!.Name == "Text 1", "First document is Text 1");
            Check(window.Title == "SinNotePad - Text 1" && System.Windows.Shell.WindowChrome.GetWindowChrome(window) == null, "Standard native title bar shows the new document name");
            var first = window.ActiveDocument;
            var editor = window.Editor!;
            editor.Text = "first line\nsecond line\nthird line";
            editor.ClearUndo(); editor.CaretIndex = editor.Text.Length; editor.SelectedText = " edited";
            Check(first.Dirty, "Editing marks the document modified");
            Check(first.Name == "Text 1", "Editing does not rename numbered documents");
            Check(window.CurrentView!.Gutter.LineCount == 3, "Gutter counts logical lines");
            Check(window.GoToLine(2), "Go to logical line succeeds");
            Check(window.CurrentView.Gutter.Position(editor.CaretIndex) == (2, 1), "Caret status identifies line 2 column 1");
            Check(!window.GoToLine(99), "Out-of-range line rejected");
            window.SetDocumentList(true); window.UpdateLayout();
            Check(window.IsDocumentList && window.DocumentPane.IsVisible, "Vertical Document List is visible");
            Capture(window, Path.Combine(app.Store.DirectoryPath, "vertical.png"));
            window.ResizeDocumentList(310); window.UpdateLayout();
            Check(Math.Abs(window.ListColumn.ActualWidth - 310) < 2, "Sidebar resizes to requested width");
            window.SetDocumentList(false); window.SetDocumentList(true); window.UpdateLayout();
            Check(Math.Abs(window.ListColumn.ActualWidth - 310) < 2, "Sidebar width survives view toggle");
            Check(ReferenceEquals(editor, window.Editor), "View toggles retain the original editor and undo stack");
            window.Editor!.Undo(); Check(!window.Editor.Text.EndsWith(" edited"), "Undo survives navigation layout changes");
            window.Editor.Redo(); Check(window.Editor.Text.EndsWith(" edited"), "Redo restores edit");
            double oldSize = editor.FontSize;
            window.ChangeZoom(40); Check(editor.FontSize > oldSize && first.Zoom == 140, "Zoom enlarges text");
            window.ChangeZoom(-20); Check(first.Zoom == 120, "Zoom reduces text");
            window.ChangeZoom(100, true); Check(Math.Abs(editor.FontSize - oldSize) < 0.01, "Default zoom restores text size");
            var second = window.NewDocument(); Check(second.Name == "Text 2", "Second document is sequential");
            window.Editor!.Text = "a separate document";
            window.ActiveDocument = first;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(window.Editor == editor && editor.CanUndo, "Switching documents preserves editor and undo history");
            Check(second.Text == "a separate document", "Documents retain independent text");
            editor.Text = "first\r\nsecond\r\nthird";
            editor.Select(editor.Text.IndexOf("second", StringComparison.Ordinal) + 3, 2);
            var caretRestored = new MainWindow(window.Snapshot()); caretRestored.Show();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(caretRestored.CurrentView!.Gutter.Position(caretRestored.Editor!.SelectionStart) == (2, 4) && caretRestored.Editor.SelectedText == "on", "Session caret and selection survive CRLF normalization");
            caretRestored.Close();
            editor.Text = "cat Cat scatter\ncat"; editor.ClearUndo();
            Check(window.ReplaceAll("cat", "$value", false, true) == 3, "Replace all honors whole words and literal replacement");
            editor.Undo(); Check(TextFiles.Normalize(editor.Text) == "cat Cat scatter\ncat", "Replace all is one undo operation");
            window.ShowFind(); window.FindBox.Text = "cat"; editor.Select(0, 0); Check(window.FindNext(), "Find locates text");
            Check(editor.SelectionStart == 0 && editor.SelectionLength == 3, "Find selects the exact occurrence");
            window.MoveDocument(second, 0); Check(window.Documents[0] == second, "Document reordering updates collection");
            window.ActiveDocument = first;
            editor.Text = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"Line {i}: editable plain text"));
            window.UpdateLayout(); editor.ScrollToEnd();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            window.CurrentView!.Gutter.InvalidateVisual(); window.UpdateLayout();
            Check(window.CurrentView.Gutter.LineCount == 200 && editor.VerticalOffset > 0, "Long text scrolls with 200 logical line numbers");
            var snapshot = window.Snapshot(); app.Store.Write("test-session.json", snapshot);
            var restored = new MainWindow(app.Store.Read<WindowSession>("test-session.json")); restored.Show();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(restored.Documents.Count == 2 && restored.ActiveDocument!.Text == first.Text, "Session restores unsaved text and tab order");
            Check(restored.IsDocumentList && restored.ActiveDocument!.Id == first.Id, "Session restores navigation mode and selected document");
            app.Preferences.AutoSaveDirectory = Path.Combine(app.Store.DirectoryPath, "auto-save");
            app.Preferences.NextDocumentNumber = 50;
            var auto = window.NewDocument();
            Check(auto.Path != null && File.Exists(auto.Path) && new FileInfo(auto.Path).Length == 0, "New numbered document exists on disk immediately");
            Check(window.Title == "SinNotePad - " + auto.Path, "Title bar shows full path for a saved document");
            window.Editor!.Text = "Unicode café 中文 😀\nsecond line";
            window.FlushAutoSaves(true);
            Check(!auto.Dirty && TextFiles.Open(auto.Path!).Text == auto.Text, "Auto-save writes edited Unicode text to disk");
            File.WriteAllText(auto.Path!, "changed elsewhere");
            window.Editor.Text = "our later edit";
            Check(!window.FlushAutoSaves(true), "Auto-save detects external write conflict");
            Check(File.ReadAllText(auto.Path!) == "changed elsewhere" && auto.Text == "our later edit", "Conflict preserves disk file and in-memory edits");
            app.Preferences.NextDocumentNumber = 1;
            var afterReset = window.NewDocument(); Check(afterReset.UntitledNumber == 1 && File.Exists(afterReset.Path), "Reset numbering creates Text 1 in the chosen folder");
            app.Preferences.NextDocumentNumber = 1;
            var collision = window.NewDocument(); Check(collision.UntitledNumber == 2, "Number reset safely skips existing Text 1");
            results.Add($"PASS {results.Count(r => r.StartsWith("PASS "))} UI integration checks");
            File.WriteAllLines(report, results);
            app.Shutdown(0);
        }
        catch (Exception ex)
        {
            results.Add("FAIL " + ex); File.WriteAllLines(report, results); app.Shutdown(1);
        }
    }
    static void Capture(Window window, string path)
    {
        window.UpdateLayout();
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var file = File.Create(path); encoder.Save(file);
    }
    static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
        { var child = System.Windows.Media.VisualTreeHelper.GetChild(node, i); yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
}
