using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SinNotepad;

internal static class FileAssociations
{
    const string ApplicationName = "Sin - Notepad";
    const string ProgId = "SinNotepad.TextFile";
    const uint AssociationChanged = 0x08000000;
    const uint FlushNotification = 0x1003;

    [DllImport("shell32.dll")]
    static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);

    static string RegistrationVersion => typeof(FileAssociations).Assembly.GetName().Version?.ToString() ?? "0";

    public static void OpenSinNotepadDefaults()
    {
        if (Register()) Thread.Sleep(500);
        OpenSettings("ms-settings:defaultapps?registeredAppUser=Sin%20-%20Notepad");
    }

    public static void OpenMicrosoftNotepadDefaults() =>
        OpenSettings("ms-settings:defaultapps?registeredAUMID=Microsoft.WindowsNotepad_8wekyb3d8bbwe%21App");

    public static bool Register()
    {
        string executable = Environment.ProcessPath ?? throw new IOException("The application path is unavailable.");
        using var software = Registry.CurrentUser.CreateSubKey("Software");
        if (IsRegistrationCurrent(software, executable)) return false;
        Register(software, executable);
        SHChangeNotify(AssociationChanged, FlushNotification, IntPtr.Zero, IntPtr.Zero);
        return true;
    }

    internal static bool IsRegistrationCurrent(RegistryKey software, string executable)
    {
        using var registration = software.OpenSubKey("SinNotepad");
        return string.Equals((string?)registration?.GetValue("RegisteredExecutable"), executable, StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string?)registration?.GetValue("RegistrationVersion"), RegistrationVersion, StringComparison.Ordinal);
    }

    internal static void Register(RegistryKey software, string executable)
    {
        string icon = $"\"{executable}\",0";
        string command = $"\"{executable}\" \"%1\"";

        using (var registered = software.CreateSubKey("RegisteredApplications"))
            registered.SetValue(ApplicationName, @"Software\SinNotepad\Capabilities");
        using (var registration = software.CreateSubKey("SinNotepad"))
        {
            registration.SetValue("RegisteredExecutable", executable);
            registration.SetValue("RegistrationVersion", RegistrationVersion);
        }
        using (var capabilities = software.CreateSubKey(@"SinNotepad\Capabilities"))
        {
            capabilities.SetValue("ApplicationName", ApplicationName);
            capabilities.SetValue("ApplicationDescription", "Plain-text editor with tabs and a resizable document list.");
            capabilities.SetValue("ApplicationIcon", icon);
            using var associations = capabilities.CreateSubKey("FileAssociations");
            associations.SetValue(".txt", ProgId);
        }
        using (var type = software.CreateSubKey(@"Classes\" + ProgId)) type.SetValue(null, "Text Document");
        using (var defaultIcon = software.CreateSubKey(@"Classes\" + ProgId + @"\DefaultIcon")) defaultIcon.SetValue(null, icon);
        using (var open = software.CreateSubKey(@"Classes\" + ProgId + @"\shell\open\command")) open.SetValue(null, command);
        const string applicationKey = @"Classes\Applications\Sin - Notepad.exe";
        using (var application = software.CreateSubKey(applicationKey))
        {
            application.SetValue("FriendlyAppName", ApplicationName);
            using var supported = application.CreateSubKey("SupportedTypes");
            supported.SetValue(".txt", "");
        }
        using (var applicationIcon = software.CreateSubKey(applicationKey + @"\DefaultIcon")) applicationIcon.SetValue(null, icon);
        using (var applicationOpen = software.CreateSubKey(applicationKey + @"\shell\open\command")) applicationOpen.SetValue(null, command);
    }

    static void OpenSettings(string uri) => Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
}
