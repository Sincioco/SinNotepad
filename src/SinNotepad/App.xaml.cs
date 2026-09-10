using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using SinNotepad.Core;

namespace SinNotepad;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;
    public Store Store { get; private set; } = null!;
    public Settings Preferences { get; private set; } = new();
    public bool TestMode { get; private set; }
    public bool Exiting { get; private set; }
    public string? PersistenceError { get; private set; }
    Mutex? instanceMutex;
    string pipeName = "";
    readonly CancellationTokenSource stop = new();
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(2) };
    bool sessionChanged;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var args = e.Args.ToList();
        string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sin - Notepad");
        int dataIndex = args.IndexOf("--data-dir");
        if (dataIndex >= 0 && dataIndex + 1 < args.Count) { dataPath = Path.GetFullPath(args[dataIndex + 1]); args.RemoveRange(dataIndex, 2); }
        TestMode = args.Remove("--self-test");
        if (TestMode && dataIndex < 0) dataPath = Path.Combine(Path.GetTempPath(), "SinNotePad-ui-tests", Guid.NewGuid().ToString("N"));
        Store = new(dataPath);
        Directory.CreateDirectory(dataPath);
        pipeName = "SinNotepad-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataPath.ToUpperInvariant())))[..24];
        instanceMutex = new Mutex(true, "Local\\" + pipeName, out bool isFirst);
        if (!isFirst && !TestMode)
        {
            try { using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly); await client.ConnectAsync(5000); using var writer = new StreamWriter(client); await writer.WriteLineAsync(JsonSerializer.Serialize(args)); }
            catch (Exception ex) { MessageBox.Show("Could not contact the running editor. Please try again.\n\n" + ex.Message, "Sin - Notepad"); }
            Shutdown(); return;
        }
        Preferences = Store.Read<Settings>("settings.json");
        Preferences.NextDocumentNumber = Math.Max(1, Preferences.NextDocumentNumber);
        DispatcherUnhandledException += (_, ev) => { ev.Handled = true; MessageBox.Show(ev.Exception.Message, "Sin - Notepad", MessageBoxButton.OK, MessageBoxImage.Error); };
        if (TestMode) { await UiSelfTest.Run(this); return; }
        _ = ListenForFiles();
        var session = Preferences.RestoreSession ? Store.Read<Session>("session.json") : new Session();
        foreach (var saved in session.Windows.Where(w => w.Documents.Count > 0)) { var window = new MainWindow(saved); window.Show(); }
        if (Windows.OfType<MainWindow>().FirstOrDefault() is not { } main) { main = new MainWindow(); main.Show(); }
        MainWindow = main;
        if (args.Count > 0) main.OpenPaths(args);
        timer.Tick += (_, _) => { if (sessionChanged) SaveState(); };
        timer.Start();
        sessionChanged = true;
        SessionEnding += (_, _) => { foreach (var window in Windows.OfType<MainWindow>()) window.FlushAutoSaves(true); SaveState(); };
    }
    async Task ListenForFiles()
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(stop.Token);
                using var reader = new StreamReader(server); string? line = await reader.ReadLineAsync(stop.Token);
                var paths = JsonSerializer.Deserialize<List<string>>(line ?? "[]") ?? [];
                await Dispatcher.InvokeAsync(() =>
                {
                    var main = Windows.OfType<MainWindow>().FirstOrDefault(w => w.IsActive) ?? Windows.OfType<MainWindow>().FirstOrDefault();
                    if (main == null) { main = new MainWindow(); main.Show(); }
                    if (paths.Count > 0) main.OpenPaths(paths);
                    if (main.WindowState == WindowState.Minimized) main.WindowState = WindowState.Normal;
                    main.Activate();
                });
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) when (ex is IOException or JsonException) { await Task.Delay(250, stop.Token).ConfigureAwait(false); }
        }
    }
    public int NextNumber() { int result = Preferences.NextDocumentNumber++; MarkChanged(); return result; }
    public void MarkChanged() => sessionChanged = true;
    public bool SaveState()
    {
        try
        {
            Store.Write("settings.json", Preferences);
            if (Preferences.RestoreSession) Store.Write("session.json", new Session { Windows = Windows.OfType<MainWindow>().Select(w => w.Snapshot()).ToList() });
            else Store.Write("session.json", new Session());
            sessionChanged = false; PersistenceError = null; return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { PersistenceError = ex.Message; return false; }
    }
    public void ExitAll()
    {
        var windows = Windows.OfType<MainWindow>().ToArray();
        foreach (var window in windows) if (!window.PrepareClose()) return;
        if (!SaveState()) { MessageBox.Show("Could not save your session. Save your files before closing.\n\n" + PersistenceError, "Sin - Notepad"); return; }
        Exiting = true;
        foreach (var window in windows) window.Close();
    }
    public void CloseDocumentClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Document doc } source && Window.GetWindow(source) is MainWindow window) window.CloseDocument(doc);
        e.Handled = true;
    }
    protected override void OnExit(ExitEventArgs e)
    {
        timer.Stop(); stop.Cancel(); instanceMutex?.Dispose(); base.OnExit(e);
    }
}
