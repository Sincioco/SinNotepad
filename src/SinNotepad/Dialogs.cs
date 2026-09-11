using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace SinNotepad;

public enum SaveChoice { Cancel, Save, Discard }
public static class Dialogs
{
    static Window Create(Window owner, string title, double width = 450)
    {
        var w = new Window { Owner = owner, Title = title, Width = width, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false, FontFamily = new FontFamily("Segoe UI"), FontSize = 14, ThemeMode = owner.ThemeMode };
        w.SetResourceReference(Window.BackgroundProperty, "ShellBrush"); w.SetResourceReference(Window.ForegroundProperty, "TextBrush"); return w;
    }
    static Button Button(string label, Action action, bool primary = false, bool cancel = false)
    {
        var b = new Button { Content = label, MinWidth = 90, Margin = new Thickness(6, 0, 0, 0), Padding = new Thickness(12, 6, 12, 6), IsDefault = primary, IsCancel = cancel };
        b.Click += (_, _) => action(); return b;
    }
    public static SaveChoice SaveChanges(Window owner, string name)
    {
        SaveChoice choice = SaveChoice.Cancel; var w = Create(owner, "Sin - Notepad");
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = $"Do you want to save changes to {name}?", TextWrapping = TextWrapping.Wrap, FontSize = 19, Margin = new Thickness(0, 0, 0, 24) });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(Button("Save", () => { choice = SaveChoice.Save; w.Close(); }, true));
        buttons.Children.Add(Button("Don't save", () => { choice = SaveChoice.Discard; w.Close(); }));
        buttons.Children.Add(Button("Cancel", () => w.Close(), cancel: true)); panel.Children.Add(buttons); w.Content = panel; w.ShowDialog(); return choice;
    }
    public static int? LineNumber(Window owner, int current, int maximum)
    {
        int? result = null; var w = Create(owner, "Go to line", 360);
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = $"Line number (1–{maximum:N0})", Margin = new Thickness(0, 0, 0, 8) });
        var input = new TextBox { Text = current.ToString() }; panel.Children.Add(input);
        var error = new TextBlock { Foreground = Brushes.IndianRed, Margin = new Thickness(0, 6, 0, 10) }; panel.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(Button("Go to", () => { if (int.TryParse(input.Text, out int line) && line >= 1 && line <= maximum) { result = line; w.Close(); } else error.Text = "Enter a valid line number."; }, true));
        buttons.Children.Add(Button("Cancel", () => w.Close(), cancel: true)); panel.Children.Add(buttons);
        w.Content = panel; w.Loaded += (_, _) => { input.Focus(); input.SelectAll(); }; w.ShowDialog(); return result;
    }
    public static void RenameFile(Window owner, string currentName, Action<string> rename)
    {
        var w = Create(owner, "Rename file - Sin - Notepad", 500);
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "File name (including extension)", Margin = new Thickness(0, 0, 0, 8) });
        var input = new TextBox { Text = currentName };
        System.Windows.Automation.AutomationProperties.SetName(input, "File name"); panel.Children.Add(input);
        var error = new TextBlock { Foreground = Brushes.IndianRed, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 16) }; panel.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(Button("Rename", () =>
        {
            try { rename(input.Text); w.Close(); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            { error.Text = ex.Message; input.Focus(); }
        }, true));
        buttons.Children.Add(Button("Cancel", () => w.Close(), cancel: true)); panel.Children.Add(buttons);
        w.Content = panel;
        w.Loaded += (_, _) => { input.Focus(); int dot = currentName.LastIndexOf('.'); input.Select(0, dot > 0 ? dot : currentName.Length); };
        w.ShowDialog();
    }
    public static bool DeleteFile(Window owner, string path, bool dirty)
    {
        bool confirmed = false; var w = Create(owner, "Delete file - Sin - Notepad", 520);
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Move this file to the Recycle Bin?", FontSize = 20, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = path, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 12) });
        panel.Children.Add(new TextBlock { Text = "This closes every tab for this file." + (dirty ? " Unsaved changes in those tabs will be discarded." : ""), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 20) });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(Button("Delete", () => { confirmed = true; w.Close(); }));
        buttons.Children.Add(Button("Cancel", () => w.Close(), primary: true, cancel: true)); panel.Children.Add(buttons);
        w.Content = panel; w.ShowDialog(); return confirmed;
    }
    public static void Settings(MainWindow owner)
    {
        var p = App.Current.Preferences;
        var w = Create(owner, "Settings - Sin - Notepad", 620);
        w.MaxHeight = Math.Max(400, SystemParameters.WorkArea.Height - 60);
        var panel = new StackPanel { Margin = new Thickness(26) };
        panel.Children.Add(new TextBlock { Text = "Settings", FontSize = 28, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 20) });
        void Label(string text, string? description = null)
        {
            panel.Children.Add(new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 14, 0, 6) });
            if (description != null) { var label = new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, FontSize = 12, Margin = new Thickness(0, 0, 0, 8) }; label.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); panel.Children.Add(label); }
        }
        Label("Auto-save folder", "New documents are created here immediately as Text N.txt, and saved as you edit. Leave this empty to save new documents manually.");
        var folderRow = new Grid(); folderRow.ColumnDefinitions.Add(new()); folderRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var folder = new TextBox { Text = p.AutoSaveDirectory, MinWidth = 240, VerticalContentAlignment = VerticalAlignment.Center };
        System.Windows.Automation.AutomationProperties.SetName(folder, "Auto-save folder");
        folderRow.Children.Add(folder);
        var browse = Button("Browse…", () => { var dialog = new OpenFolderDialog { Title = "Choose a folder for new text files" }; if (Directory.Exists(folder.Text)) dialog.InitialDirectory = folder.Text; if (dialog.ShowDialog(w) == true) folder.Text = dialog.FolderName; });
        Grid.SetColumn(browse, 1); folderRow.Children.Add(browse); panel.Children.Add(folderRow);
        var clear = new Button { Content = "Use manual saving for new documents", Style = (Style)Application.Current.FindResource("FlatButton"), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 5, 0, 0) }; clear.Click += (_, _) => folder.Text = ""; panel.Children.Add(clear);
        Label("Document numbering", "The sequence continues between launches. Existing files are always skipped when a number is already in use.");
        var sequenceRow = new DockPanel();
        int nextNumber = p.NextDocumentNumber;
        bool resetSequence = false;
        var next = new TextBlock { Text = $"Next document: Text {nextNumber}", VerticalAlignment = VerticalAlignment.Center };
        var reset = Button("Reset to 1", () => { resetSequence = true; next.Text = "Next document: Text 1 (existing files will be skipped)"; });
        DockPanel.SetDock(reset, Dock.Right); sequenceRow.Children.Add(reset); sequenceRow.Children.Add(next); panel.Children.Add(sequenceRow);
        Label("Appearance");
        var themes = new ComboBox { ItemsSource = new[] { "System", "Light", "Dark" }, SelectedItem = p.Theme, HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 200 };
        System.Windows.Automation.AutomationProperties.SetName(themes, "App theme"); panel.Children.Add(themes);
        Label("Editor");
        var wrap = new CheckBox { Content = "Word wrap", IsChecked = p.WordWrap, Margin = new Thickness(0, 3, 0, 8) }; panel.Children.Add(wrap);
        var restore = new CheckBox { Content = "Restore open documents when the app starts", IsChecked = p.RestoreSession, Margin = new Thickness(0, 3, 0, 8) }; panel.Children.Add(restore);
        var error = new TextBlock { Foreground = Brushes.IndianRed, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 10) }; panel.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        buttons.Children.Add(Button("Save", () =>
        {
            string path = folder.Text.Trim();
            try
            {
                if (path.Length > 0)
                {
                    if (!Path.IsPathFullyQualified(path)) { error.Text = "Enter a full folder path, such as D:\\Notes."; return; }
                    path = Path.GetFullPath(path); Directory.CreateDirectory(path);
                    string probe = Path.Combine(path, ".sin-notepad-" + Guid.NewGuid().ToString("N") + ".tmp");
                    using (var file = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { file.WriteByte(0); }
                }
                p.AutoSaveDirectory = path; if (resetSequence) p.NextDocumentNumber = 1; p.Theme = themes.SelectedItem as string ?? "System"; p.WordWrap = wrap.IsChecked == true; p.RestoreSession = restore.IsChecked == true;
                App.Current.MarkChanged();
                if (!App.Current.SaveState()) { error.Text = "Could not save settings: " + App.Current.PersistenceError; return; }
                w.Close();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { error.Text = "This folder cannot be used: " + ex.Message; }
        }, true));
        buttons.Children.Add(Button("Cancel", () => w.Close(), cancel: true)); panel.Children.Add(buttons);
        w.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        w.ShowDialog();
    }
}
