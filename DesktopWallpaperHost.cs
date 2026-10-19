using System.Runtime.InteropServices;

namespace LiveWallpaper;

/// <summary>
/// Attaches a WPF window to the desktop background layer used by Windows Explorer.
/// Windows does not expose a supported public API for hosting animated wallpapers,
/// so this uses the commonly available Progman/WorkerW desktop window arrangement.
/// </summary>
internal static class DesktopWallpaperHost
{
    private const uint WorkerWMessage = 0x052C;
    private const uint SmtoNormal = 0x0000;
    private const int GwlStyle = -16;
    private const long WsChild = 0x40000000L;
    private const long WsPopup = unchecked((long)0x80000000);
    private const long WsVisible = 0x10000000L;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;

    private static readonly IntPtr HwndBottom = new(1);

    public static bool TryAttach(IntPtr wallpaperWindow, out string error)
    {
        error = string.Empty;

        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            error = "Windows Explorer's desktop window was not found. Restart the app after signing in to Windows.";
            return false;
        }

        // Ask Explorer to create its background WorkerW window. SendMessageTimeout
        // prevents the app from hanging if the shell does not answer.
        SendMessageTimeout(progman, WorkerWMessage, new IntPtr(0xD), new IntPtr(1),
            SmtoNormal, 1000, out _);

        var desktopHost = FindWallpaperWorker();
        if (desktopHost == IntPtr.Zero)
        {
            error = "Windows did not provide a desktop background layer. Your current wallpaper has not been changed.";
            return false;
        }

        var style = GetWindowStyle(wallpaperWindow);
        if (style == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
        {
            error = "The wallpaper window could not be prepared for the desktop.";
            return false;
        }

        Marshal.SetLastPInvokeError(0);
        SetWindowStyle(wallpaperWindow, (style.ToInt64() & ~WsPopup) | WsChild | WsVisible);
        if (Marshal.GetLastWin32Error() != 0)
        {
            error = "Windows rejected the wallpaper window style.";
            return false;
        }

        Marshal.SetLastPInvokeError(0);
        SetParent(wallpaperWindow, desktopHost);
        if (Marshal.GetLastWin32Error() != 0)
        {
            error = "Windows could not attach the wallpaper to the desktop. This can happen when Windows Explorer uses a different display-scaling mode.";
            return false;
        }

        var left = GetSystemMetrics(SmXVirtualScreen);
        var top = GetSystemMetrics(SmYVirtualScreen);
        var width = GetSystemMetrics(SmCxVirtualScreen);
        var height = GetSystemMetrics(SmCyVirtualScreen);

        if (width <= 0 || height <= 0 || !SetWindowPos(wallpaperWindow, HwndBottom,
                left, top, width, height, SwpNoActivate | SwpShowWindow))
        {
            SetParent(wallpaperWindow, IntPtr.Zero);
            error = "Windows attached the wallpaper but could not size it across your displays.";
            return false;
        }

        return true;
    }

    private static IntPtr FindWallpaperWorker()
    {
        IntPtr worker = IntPtr.Zero;

        EnumWindows((topLevelWindow, _) =>
        {
            var shellView = FindWindowEx(topLevelWindow, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView == IntPtr.Zero)
            {
                return true;
            }

            // Explorer places the wallpaper WorkerW immediately after the window
            // containing the desktop icons.
            worker = FindWindowEx(IntPtr.Zero, topLevelWindow, "WorkerW", null);
            return false;
        }, IntPtr.Zero);

        return worker;
    }

    private static IntPtr GetWindowStyle(IntPtr window)
    {
        Marshal.SetLastPInvokeError(0);
        return IntPtr.Size == 8 ? GetWindowLongPtr64(window, GwlStyle) : new IntPtr(GetWindowLong32(window, GwlStyle));
    }

    private static void SetWindowStyle(IntPtr window, long style)
    {
        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr64(window, GwlStyle, new IntPtr(style));
        }
        else
        {
            SetWindowLong32(window, GwlStyle, unchecked((int)style));
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, IntPtr wParam, IntPtr lParam,
        uint flags, uint timeout, out IntPtr result);

    [DllImport("user32.dll", EntryPoint = "SetParent", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetSystemMetrics", SetLastError = true)]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr window, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr window, int index, int value);

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr lParam);
}
