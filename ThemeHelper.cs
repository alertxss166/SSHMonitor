using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SSHMonitor;

public static class ThemeHelper
{
    private const int UseImmersiveDarkMode = 20;
#pragma warning disable SYSLIB1054 // DllImport avoids enabling unsafe code for one small Windows API call.
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
#pragma warning restore SYSLIB1054

    public static void EnableDarkTitleBar(Window window) => window.SourceInitialized += (_, _) =>
    {
        var enabled = 1;
        _ = DwmSetWindowAttribute(new WindowInteropHelper(window).Handle, UseImmersiveDarkMode, ref enabled, sizeof(int));
    };
}
