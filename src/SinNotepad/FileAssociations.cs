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

    public static void OpenSinNotepadDefaults()
    {
        Register();
        OpenSettings("ms-settings:defaultapps?registeredAppUser=Sin%20-%20Notepad");
    }

    public static void OpenMicrosoftNotepadDefaults() =>
        OpenSettings("ms-settings:defaultapps?registeredAUMID=Microsoft.WindowsNotepad_8wekyb3d8bbwe%21App");

    static void Register()
    {
        string executable = Environment.ProcessPath ?? throw new IOException("The application path is unavailable.");
        string icon = $"\"{executable}\",0";
        string command = $"\"{executable}\" \"%1\"";

        using (var registered = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
            registered.SetValue(ApplicationName, @"Software\SinNotepad\Capabilities");
        using (var capabilities = Registry.CurrentUser.CreateSubKey(@"Software\SinNotepad\Capabilities"))
        {
            capabilities.SetValue("ApplicationName", ApplicationName);
            capabilities.SetValue("ApplicationDescription", "Plain-text editor with tabs and a resizable document list.");
            capabilities.SetValue("ApplicationIcon", icon);
            using var associations = capabilities.CreateSubKey("FileAssociations");
            associations.SetValue(".txt", ProgId);
        }
        using (var type = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgId)) type.SetValue(null, "Text Document");
        using (var defaultIcon = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgId + @"\DefaultIcon")) defaultIcon.SetValue(null, icon);
        using (var open = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgId + @"\shell\open\command")) open.SetValue(null, command);
        using (var application = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications\Sin - Notepad.exe"))
        {
            application.SetValue("FriendlyAppName", ApplicationName);
            using var supported = application.CreateSubKey("SupportedTypes");
            supported.SetValue(".txt", "");
        }
        using (var applicationOpen = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications\Sin - Notepad.exe\shell\open\command")) applicationOpen.SetValue(null, command);
        SHChangeNotify(AssociationChanged, FlushNotification, IntPtr.Zero, IntPtr.Zero);
        Thread.Sleep(500);
    }

    static void OpenSettings(string uri) => Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
}
