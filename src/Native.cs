using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

// Windows API calls: global hotkey, typing into other programs, and keeping track of which window you were in.
static class Native
{
    public const int WM_HOTKEY = 0x0312;
    public const uint MOD_ALT = 1, MOD_CONTROL = 2;

    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint n, INPUT[] inputs, int size);
    [DllImport("kernel32.dll")] static extern bool Beep(uint freq, uint ms);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern int GetClassName(IntPtr h, StringBuilder sb, int max);

    [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public InputUnion U; }

    const uint KEYEVENTF_KEYUP = 0x0002, KEYEVENTF_UNICODE = 0x0004;
    const ushort VK_BACK = 0x08;

    // Windows that grab focus when you click the tray or the taskbar. Typing must never go there.
    static readonly string[] ShellClasses = {
        "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "NotifyIconOverflowWindow",
        "TopLevelWindowForOverflowXamlIsland", "XamlExplorerHostIslandWindow",
        "Windows.UI.Core.CoreWindow", "#32768" };

    static readonly uint ownPid = (uint)Process.GetCurrentProcess().Id;
    static IntPtr lastWindow = IntPtr.Zero;

    static bool IsShell(IntPtr h)
    {
        var sb = new StringBuilder(128);
        GetClassName(h, sb, 128);
        return Array.IndexOf(ShellClasses, sb.ToString()) >= 0;
    }

    static bool IsOwn(IntPtr h)
    {
        uint pid;
        GetWindowThreadProcessId(h, out pid);
        return pid == ownPid;
    }

    // Called on a timer: remember the last window that belongs to another program.
    public static void RememberWindow()
    {
        IntPtr h = GetForegroundWindow();
        if (h != IntPtr.Zero && !IsOwn(h) && !IsShell(h)) lastWindow = h;
    }

    // If focus is on our own window or the taskbar, hand it back to the window you were typing in.
    public static void RestoreWindow()
    {
        IntPtr h = GetForegroundWindow();
        if ((h == IntPtr.Zero || IsOwn(h) || IsShell(h)) && lastWindow != IntPtr.Zero && IsWindow(lastWindow))
        {
            SetForegroundWindow(lastWindow);
            Thread.Sleep(80);
        }
    }

    public static void TypeText(string s)
    {
        var inputs = new INPUT[s.Length * 2];
        int n = 0;
        foreach (char ch in s)
        {
            var down = new INPUT(); down.type = 1; down.U.ki.wScan = ch; down.U.ki.dwFlags = KEYEVENTF_UNICODE;
            var up = new INPUT(); up.type = 1; up.U.ki.wScan = ch; up.U.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            inputs[n++] = down;
            inputs[n++] = up;
        }
        SendInput((uint)n, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    public static void Backspaces(int count)
    {
        if (count <= 0) return;
        var inputs = new INPUT[count * 2];
        for (int i = 0; i < count; i++)
        {
            inputs[i * 2].type = 1; inputs[i * 2].U.ki.wVk = VK_BACK;
            inputs[i * 2 + 1].type = 1; inputs[i * 2 + 1].U.ki.wVk = VK_BACK; inputs[i * 2 + 1].U.ki.dwFlags = KEYEVENTF_KEYUP;
        }
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    // Rising two-note tone for "on", falling for "off".
    public static void Tone(bool on)
    {
        new Thread(delegate ()
        {
            Beep(on ? 660u : 990u, 110);
            Beep(on ? 990u : 660u, 130);
        }).Start();
    }
}
