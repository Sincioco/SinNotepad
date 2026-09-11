using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
            var applicationFileMenu = (MenuItem)window.MainMenu.Items[0];
            Check((string)((MenuItem)applicationFileMenu.Items[0]).Header == "_New", "File menu labels Ctrl+N as New");
            Check((string)window.RecentMenu.Header == "_Recently Opened", "File menu exposes Recently Opened");
            Check(window.RecentMenu.HasItems, "Recently Opened starts as an expandable submenu");
            Check(window.ActiveDocument!.Name == "Text 1", "First document is Text 1");
            Check(app.Preferences.AutoSaveAllOnClose, "Close auto-save is enabled by default");
            var initialEditor = window.Editor!;
            var editorScrollBars = Descendants(initialEditor).OfType<ScrollBar>().ToArray();
            Check(initialEditor.Cursor == Cursors.IBeam && editorScrollBars.Length >= 2 && editorScrollBars.All(scrollBar => scrollBar.Cursor == Cursors.Arrow), "Editor uses an insertion cursor while its scrollbars use the Windows arrow cursor");
            Check(GlyphCount(window.CurrentView!.Gutter) == 1, "An empty document visibly renders line number 1");
            Check(window.Title == "Sin - Notepad - Text 1" && System.Windows.Shell.WindowChrome.GetWindowChrome(window) == null, "Standard native title bar shows the new document name");
            Check(window.Icon == null, "Window lets Windows select a taskbar-sized executable icon frame");
            Check(window.MainMenu.TranslatePoint(new Point(0, window.MainMenu.ActualHeight), window).Y <= window.HorizontalNavigation.TranslatePoint(new Point(), window).Y, "Tabs sit below the menu bar");
            Check(!Descendants(window.Tabs).OfType<Button>().Any(), "Tabs have no close X buttons");
            Check(window.CreateDocumentMenu(window.ActiveDocument!).Items.OfType<MenuItem>().Last().IsEnabled &&
                !window.CreateDocumentMenu(window.ActiveDocument!).Items.OfType<MenuItem>().First().IsEnabled, "Unsaved documents offer Close while file operations require a path");
            app.Preferences.Recent.Clear();
            var recentWindow = new MainWindow(new WindowSession { Documents = [new Document { UntitledNumber = 0 }] }); recentWindow.Show();
            var recentPaths = Enumerable.Range(1, 12).Select(i => Path.Combine(app.Store.DirectoryPath, $"recent-{i}.txt")).ToArray();
            foreach (var path in recentPaths) { File.WriteAllText(path, path); recentWindow.OpenPaths([path]); }
            Check(app.Preferences.Recent.Count == Settings.RecentFileLimit && app.Preferences.Recent[0] == recentPaths[11] && app.Preferences.Recent[^1] == recentPaths[2], "Recently Opened retains the latest 10 files in newest-first order");
            recentWindow.RecentMenu.RaiseEvent(new RoutedEventArgs(MenuItem.SubmenuOpenedEvent));
            Check(recentWindow.RecentMenu.Items.Count == Settings.RecentFileLimit &&
                (string)((MenuItem)recentWindow.RecentMenu.Items[0]).ToolTip == recentPaths[11], "Recently Opened displays the saved history");
            app.SaveState();
            Check(app.Store.Read<Settings>("settings.json").Recent.SequenceEqual(app.Preferences.Recent), "Recently Opened persists between launches");
            recentWindow.Close();
            var first = window.ActiveDocument;
            var editor = window.Editor!;
            editor.Text = "before selected after"; editor.ClearUndo(); editor.Select(7, 8);
            window.InsertDate(5, new DateTime(2026, 9, 11, 17, 33, 0));
            Check(editor.Text == "before 2026-09-11 - 1733 after", "Date/Time replaces the selection in the chosen format");
            editor.Undo(); Check(editor.Text == "before selected after", "Date/Time insertion is undoable");
            window.InsertDate(now: new DateTime(2026, 9, 12, 8, 5, 0));
            Check(editor.Text.Contains("2026-09-12 - 0805"), "F5 uses the most recently chosen Date/Time format");
            window.PopulateDateTimeMenu(new DateTime(2026, 9, 11, 17, 33, 0));
            Check(window.DateTimeMenu.Items.Count == 6 && ((MenuItem)window.DateTimeMenu.Items[5]).IsChecked, "Date/Time submenu exposes all six choices and marks the current choice");
            editor.Text = "before selected after"; editor.ClearUndo(); editor.Select(7, 8);
            Check(window.TryInsertShortcut(Key.D, ModifierKeys.Control, new DateTime(2026, 9, 11, 19, 8, 0)) &&
                editor.Text == "before Friday, September 11, 2026 at 7:08 pm after", "Ctrl+D inserts the requested long English date/time format");
            Check(app.Preferences.DateTimeFormat == 5 && editor.SelectionLength == 0, "Ctrl+D leaves the F5 preference intact and places the caret after the date");
            editor.Undo(); Check(editor.Text == "before selected after", "Ctrl+D insertion is a single undoable edit");
            editor.Select(7, 8);
            bool navigationBefore = window.IsDocumentList;
            Check(window.TryInsertShortcut(Key.L, ModifierKeys.Control) && editor.Text == "before " + new string('_', 80) + " after", "Ctrl+L inserts exactly 80 underscores without adding a newline");
            Check(editor.SelectionStart == 87 && editor.SelectionLength == 0 && window.IsDocumentList == navigationBefore, "Ctrl+L leaves navigation unchanged and the caret after the separator");
            editor.Undo(); Check(editor.Text == "before selected after", "Ctrl+L insertion is a single undoable edit");
            Check(!window.TryInsertShortcut(Key.L, ModifierKeys.Control | ModifierKeys.Shift) &&
                !window.TryInsertShortcut(Key.D, ModifierKeys.Control | ModifierKeys.Alt) && editor.Text == "before selected after", "Insertion shortcuts require plain Ctrl and leave Ctrl+Shift+L available for navigation");
            editor.Select(7, 8);
            string dateAndSeparator = "Friday, September 11, 2026 at 7:08 pm\r\n" + new string('_', 80);
            Check(window.TryInsertShortcut(Key.I, ModifierKeys.Control, new DateTime(2026, 9, 11, 19, 8, 0)) &&
                editor.Text == "before " + dateAndSeparator + " after", "Ctrl+I replaces the selection with the long date/time and exactly 80 underscores on the next line");
            Check(editor.SelectionStart == 7 + dateAndSeparator.Length && editor.SelectionLength == 0 &&
                app.Preferences.DateTimeFormat == 5 && window.IsDocumentList == navigationBefore, "Ctrl+I places the caret after the separator and preserves the F5 preference and navigation layout");
            editor.Undo(); Check(editor.Text == "before selected after", "Ctrl+I date/time and separator are undone together in one step");
            editor.Redo(); Check(editor.Text == "before " + dateAndSeparator + " after", "Ctrl+I date/time and separator are restored together by redo");
            Check(!window.TryInsertShortcut(Key.I, ModifierKeys.None) &&
                !window.TryInsertShortcut(Key.I, ModifierKeys.Control | ModifierKeys.Shift) &&
                !window.TryInsertShortcut(Key.I, ModifierKeys.Control | ModifierKeys.Alt) &&
                editor.Text == "before " + dateAndSeparator + " after", "The combined insertion only handles plain Ctrl+I");
            editor.Text = "first line\nsecond line\nthird line";
            editor.ClearUndo(); editor.CaretIndex = editor.Text.Length; editor.SelectedText = " edited";
            Check(first.Dirty, "Editing marks the document modified");
            Check(first.Name == "Text 1", "Editing does not rename numbered documents");
            Check(window.CurrentView!.Gutter.LineCount == 3, "Gutter counts logical lines");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(GlyphCount(window.CurrentView.Gutter) == 3, "All three line numbers are visibly drawn after editing");
            var unchangedGlyphs = Glyphs(window.CurrentView.Gutter).ToArray();
            var typingFrames = new List<int>();
            EventHandler sampleTypingFrame = (_, _) => typingFrames.Add(GlyphCount(window.CurrentView!.Gutter));
            System.Windows.Media.CompositionTarget.Rendering += sampleTypingFrame;
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    editor.SelectedText = "x";
                    await Task.Delay(60);
                    Check(GlyphCount(window.CurrentView.Gutter) == 3, $"Line numbers remain visibly drawn after keystroke {i + 1}");
                }
            }
            finally { System.Windows.Media.CompositionTarget.Rendering -= sampleTypingFrame; }
            results.Add($"INFO typing frames: {typingFrames.Count}; blank frames: {typingFrames.Count(count => count == 0)}");
            Check(typingFrames.Count >= 5 && typingFrames.All(count => count == 3), "Every rendered typing frame retains all three line numbers without a blank flash");
            Check(unchangedGlyphs.SequenceEqual(Glyphs(window.CurrentView.Gutter)), "Ordinary typing reuses the existing number glyphs when line positions are unchanged");
            editor.SelectedText = "\r\nnew line";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(GlyphCount(window.CurrentView.Gutter) == 4, "Typing a newline visibly adds a line number");
            editor.Undo();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(GlyphCount(window.CurrentView.Gutter) == 3, "Undo redraws line numbers");
            for (int i = 0; i < 5; i++) editor.Undo();
            window.SetLineNumbers(false); window.UpdateLayout();
            Check(window.CurrentView.Gutter.Visibility == Visibility.Collapsed && window.CurrentView.Gutter.LineCount == 3, "Hiding line numbers removes the gutter but preserves status and Go to data");
            window.SetLineNumbers(true);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(GlyphCount(window.CurrentView.Gutter) == 3 && ReferenceEquals(editor, window.Editor), "Showing line numbers restores drawing without replacing the editor");
            Check(window.GoToLine(2), "Go to logical line succeeds");
            Check(window.CurrentView.Gutter.Position(editor.CaretIndex) == (2, 1), "Caret status identifies line 2 column 1");
            Check(!window.GoToLine(99), "Out-of-range line rejected");
            window.SetDocumentList(true); window.UpdateLayout();
            Check(window.IsDocumentList && window.DocumentPane.IsVisible, "Vertical Document List is visible");
            Check(!Descendants(window.DocumentList).OfType<Button>().Any(), "Document List rows have no close X buttons");
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
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(Glyphs(window.CurrentView.Gutter).All(g => Math.Abs(g.FontRenderingEmSize - editor.FontSize) < 0.01), "Cached line-number drawings refresh to match the zoomed editor font");
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
            Check(window.CurrentView.Gutter.LineCount == 200 && editor.VerticalOffset > 0, "Long text scrolls with 200 logical line numbers");
            Check(new string(Glyphs(window.CurrentView.Gutter).Last().Characters.ToArray()) == "200", "Scrolling replaces cached numbers with the visible final line 200");
            var snapshot = window.Snapshot(); app.Store.Write("test-session.json", snapshot);
            var restored = new MainWindow(app.Store.Read<WindowSession>("test-session.json")); restored.Show();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(restored.Documents.Count == 2 && restored.ActiveDocument!.Text == first.Text, "Session restores unsaved text and tab order");
            Check(restored.IsDocumentList && restored.ActiveDocument!.Id == first.Id, "Session restores navigation mode and selected document");
            app.Preferences.AutoSaveDirectory = Path.Combine(app.Store.DirectoryPath, "auto-save");
            app.Preferences.NextDocumentNumber = 50;
            var auto = window.NewDocument();
            Check(auto.Path != null && File.Exists(auto.Path) && new FileInfo(auto.Path).Length == 0, "New numbered document exists on disk immediately");
            Check(window.Title == "Sin - Notepad - " + auto.Name && !window.Title.Contains(Path.DirectorySeparatorChar), "Title bar shows only the filename for a saved document");
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
            string originalPath = afterReset.Path!;
            window.ActiveDocument = afterReset;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var renameEditor = window.Editor!; renameEditor.ClearUndo(); renameEditor.SelectedText = "keep pending edits";
            var mirror = new MainWindow(); mirror.Show(); mirror.OpenPaths([originalPath]);
            var mirrorDoc = mirror.ActiveDocument!;
            window.ActiveDocument = collision;
            window.RenameDocumentFile(afterReset, "Renamed café.txt");
            Check(!File.Exists(originalPath) && File.Exists(afterReset.Path) && afterReset.Dirty && File.ReadAllText(afterReset.Path!) == "", "Rename moves the physical file and retains unsaved edits separately");
            Check(mirrorDoc.Path == afterReset.Path && mirror.Title == "Sin - Notepad - " + afterReset.Name && window.ActiveDocument == collision, "Rename updates other windows and targets the requested inactive document");
            window.ActiveDocument = afterReset;
            Check(window.Editor == renameEditor && renameEditor.CanUndo && window.Title == "Sin - Notepad - " + afterReset.Name, "Rename preserves editor identity, undo and the filename title");
            window.FlushAutoSaves(true);
            Check(File.ReadAllText(afterReset.Path!) == "keep pending edits" && !File.Exists(originalPath), "Pending auto-save follows the renamed file");
            var fileMenu = window.CreateDocumentMenu(afterReset);
            Check(fileMenu.Items.OfType<MenuItem>().Count() == 5 && fileMenu.Items.OfType<MenuItem>().All(i => i.IsEnabled), "Saved documents expose Rename, Delete, Copy path, Open folder and Close");
            var clipboardBefore = Clipboard.GetDataObject();
            try
            {
                ((MenuItem)fileMenu.Items[3]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Check(Clipboard.GetText() == afterReset.Path, "Copy full path places the exact file path on the clipboard");
            }
            finally { if (clipboardBefore != null) Clipboard.SetDataObject(clipboardBefore, true); else Clipboard.Clear(); }
            var explorer = MainWindow.ContainingFolderCommand(afterReset.Path!);
            Check(explorer.Arguments == $"/select,\"{afterReset.Path}\"" && explorer.FileName.EndsWith("explorer.exe"), "Open containing folder selects the correct file, including spaces and Unicode");
            renameEditor.SelectedText = " unsaved";
            bool cancelled = window.DeleteDocumentFile(afterReset, (path, dirty) =>
            {
                window.FlushAutoSaves(true);
                Check(dirty && File.ReadAllText(path) == "keep pending edits", "Delete confirmation pauses auto-save and identifies unsaved changes");
                return false;
            });
            Check(!cancelled && File.Exists(afterReset.Path) && window.Documents.Contains(afterReset) && afterReset.Dirty, "Cancelling Delete preserves file, tabs and edits");
            bool failedDelete = false;
            try { window.DeleteDocumentFile(afterReset, (_, _) => true, _ => throw new IOException("Simulated unavailable recycle service")); }
            catch (IOException) { failedDelete = true; }
            Check(failedDelete && window.Documents.Contains(afterReset) && File.Exists(afterReset.Path), "Failed deletion preserves file and open document");
            window.FlushAutoSaves(true);
            Check(!afterReset.Dirty, "Auto-save resumes after cancelled or failed deletion");
            string deletedPath = afterReset.Path!;
            string savedCopy = Path.Combine(app.Store.DirectoryPath, "saved-during-delete.txt");
            Document? openedDuringDelete = null;
            Check(window.DeleteDocumentFile(afterReset, (_, _) =>
            {
                TextFiles.Save(mirrorDoc, savedCopy);
                mirror.OpenPaths([deletedPath]); openedDuringDelete = mirror.ActiveDocument;
                return true;
            }), "Delete sends the isolated fixture through the Windows Recycle Bin service");
            Check(!File.Exists(deletedPath) && !window.Documents.Contains(afterReset) && !mirror.Documents.Contains(openedDuringDelete!), "Successful Delete removes every current reference, including one opened during confirmation");
            Check(mirror.Documents.Contains(mirrorDoc) && File.Exists(savedCopy), "Delete preserves a document saved to a different path during confirmation");
            window.FlushAutoSaves(true); mirror.FlushAutoSaves(true);
            Check(!File.Exists(deletedPath), "Auto-save cannot recreate a deleted file");
            app.Preferences.AutoSaveDirectory = "";
            window.SetLineNumbers(false);
            var closeTarget = window.NewDocument(); window.ActiveDocument = collision;
            window.ActiveDocument = closeTarget;
            Check(window.CurrentView!.Gutter.Visibility == Visibility.Collapsed, "New documents inherit the hidden line-number preference");
            window.ActiveDocument = collision;
            window.SetLineNumbers(true);
            ((MenuItem)window.CreateDocumentMenu(closeTarget).Items[^1]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Check(!window.Documents.Contains(closeTarget) && window.ActiveDocument == collision, "Context-menu Close removes its target while preserving the other active document");
            string closeSavePath = Path.Combine(app.Store.DirectoryPath, "save-all-on-close.txt");
            File.WriteAllText(closeSavePath, "before close");
            var closeSaveWindow = new MainWindow(); closeSaveWindow.Show();
            closeSaveWindow.OpenPaths([closeSavePath]);
            var closeSaveDocument = closeSaveWindow.ActiveDocument!;
            closeSaveWindow.Editor!.Text = "saved while closing";
            app.Preferences.AutoSaveAllOnClose = true;
            Check(closeSaveWindow.PrepareClose() && File.ReadAllText(closeSavePath) == "saved while closing" && !closeSaveDocument.Dirty, "Closing auto-saves every changed document that has a file path when enabled");
            closeSaveWindow.Editor.Text = "leave pending";
            app.Preferences.AutoSaveAllOnClose = false;
            Check(closeSaveWindow.PrepareClose() && File.ReadAllText(closeSavePath) == "saved while closing" && closeSaveDocument.Dirty, "Disabling close auto-save leaves ordinary file changes pending for session recovery");
            app.Preferences.AutoSaveAllOnClose = true;
            closeSaveWindow.Close();
            var gutterDocument = window.NewDocument();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            app.Preferences.WordWrap = false; window.ApplyPreferences();
            window.Editor!.Text = string.Join("\n", Enumerable.Repeat(new string('x', 500), 3));
            window.Editor.CaretIndex = window.Editor.Text.Length; window.Editor.ScrollToHorizontalOffset(1800);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(window.Editor.HorizontalOffset > 0 && GlyphCount(window.CurrentView!.Gutter) == 3, "Line numbers remain visible during horizontal scrolling with word wrap off");
            window.Editor.SelectedText = "typing";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(GlyphCount(window.CurrentView!.Gutter) == 3, "Typing after horizontal scrolling retains all visible line numbers");
            app.Preferences.WordWrap = true; window.ApplyPreferences();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(GlyphCount(window.CurrentView!.Gutter) > 0, "Wrapped text redraws logical line numbers after the layout changes");
            window.Editor.Text = "short\nsecond\nthird"; window.Editor.ScrollToHome();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            double secondLineBaseline = Glyphs(window.CurrentView.Gutter).ElementAt(1).BaselineOrigin.Y;
            window.Editor.Select(5, 0); window.Editor.SelectedText = new string('x', 120);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(Glyphs(window.CurrentView.Gutter).ElementAt(1).BaselineOrigin.Y > secondLineBaseline, "Wrapping the first paragraph moves subsequent line-number drawings into alignment");
            app.BeginExit();
            Check(app.Exiting, "The app enters exit mode before dispatcher teardown begins");
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
    static int GlyphCount(DependencyObject element) => Glyphs(element).Count();
    static IEnumerable<System.Windows.Media.GlyphRun> Glyphs(DependencyObject element)
    {
        IEnumerable<System.Windows.Media.GlyphRun> Read(System.Windows.Media.Drawing? drawing) => drawing switch
        {
            System.Windows.Media.GlyphRunDrawing glyph => [glyph.GlyphRun],
            System.Windows.Media.DrawingGroup group => group.Children.SelectMany(Read),
            _ => []
        };
        return Read(System.Windows.Media.VisualTreeHelper.GetDrawing((System.Windows.Media.Visual)element));
    }
    static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
        { var child = System.Windows.Media.VisualTreeHelper.GetChild(node, i); yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
}
