// Voice Typer: press Ctrl+Alt+D (or click the tray icon) and whatever you say is typed into the
// app you are in. Works offline with Windows' own speech recognizer. No network, no logging.
using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

class VoiceTyper : Form
{
    const int HOTKEY_ID = 1;
    const int WM_HOTKEY = 0x0312;
    const uint MOD_ALT = 1, MOD_CONTROL = 2;

    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint n, INPUT[] inputs, int size);
    [DllImport("kernel32.dll")] static extern bool Beep(uint freq, uint ms);

    [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public InputUnion U; }

    NotifyIcon tray;
    Icon iconOff, iconOn;
    SpeechRecognitionEngine engine;
    bool listening = false;
    bool sentenceStart = true;
    ToolStripMenuItem toggleItem;

    [STAThread]
    static void Main()
    {
        bool first;
        using (var mutex = new Mutex(true, "VoiceTyper-single-instance", out first))
        {
            if (!first) return;
            Application.EnableVisualStyles();
            Application.Run(new VoiceTyper());
        }
    }

    VoiceTyper()
    {
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        iconOff = MakeIcon(Color.Gray);
        iconOn = MakeIcon(Color.Red);
        var menu = new ContextMenuStrip();
        toggleItem = new ToolStripMenuItem("Start dictation  (Ctrl+Alt+D)");
        toggleItem.Click += delegate { Toggle(); };
        var exit = new ToolStripMenuItem("Exit");
        exit.Click += delegate { Close(); };
        menu.Items.Add(toggleItem);
        menu.Items.Add(exit);
        tray = new NotifyIcon();
        tray.Icon = iconOff;
        tray.Text = "Voice Typer (off)";
        tray.ContextMenuStrip = menu;
        tray.Visible = true;
        tray.MouseClick += delegate (object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) Toggle(); };
    }

    protected override void SetVisibleCore(bool value) { base.SetVisibleCore(false); }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, (uint)Keys.D))
            tray.ShowBalloonTip(4000, "Voice Typer", "Ctrl+Alt+D is taken by another program. Click the tray icon instead.", ToolTipIcon.Warning);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        if (engine != null) { try { engine.RecognizeAsyncCancel(); } catch { } engine.Dispose(); }
        tray.Visible = false;
        base.OnFormClosed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID) Toggle();
        base.WndProc(ref m);
    }

    static Icon MakeIcon(Color c)
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(c)) g.FillEllipse(b, 1, 1, 13, 13);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    static void Tone(bool on)
    {
        new Thread(delegate ()
        {
            uint a = on ? 660u : 990u, b = on ? 990u : 660u;
            Beep(a, 110); Beep(b, 130);
        }).Start();
    }

    void Toggle()
    {
        if (listening) Stop(); else Start();
    }

    void Start()
    {
        try
        {
            if (engine == null)
            {
                engine = new SpeechRecognitionEngine(new CultureInfo("en-US"));
                engine.LoadGrammar(new DictationGrammar());
                engine.SetInputToDefaultAudioDevice();
                engine.SpeechRecognized += OnRecognized;
            }
            sentenceStart = true;
            engine.RecognizeAsync(RecognizeMode.Multiple);
            listening = true;
            tray.Icon = iconOn;
            tray.Text = "Voice Typer (listening)";
            toggleItem.Text = "Stop dictation  (Ctrl+Alt+D)";
            Tone(true);
        }
        catch (Exception ex)
        {
            listening = false;
            if (engine != null) { try { engine.Dispose(); } catch { } engine = null; }
            tray.ShowBalloonTip(5000, "Voice Typer", "Could not start: " + ex.Message, ToolTipIcon.Error);
        }
    }

    void Stop()
    {
        try { engine.RecognizeAsyncCancel(); } catch { }
        listening = false;
        tray.Icon = iconOff;
        tray.Text = "Voice Typer (off)";
        toggleItem.Text = "Start dictation  (Ctrl+Alt+D)";
        Tone(false);
    }

    void OnRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        if (!listening || e.Result == null || e.Result.Confidence < 0.2f) return;
        string t = e.Result.Text.Trim();
        if (t.Length == 0) return;
        t = Regex.Replace(t, @"\b(question mark)\b", "?", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\b(exclamation (mark|point))\b", "!", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\b(full stop|period)\b", ".", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\bcomma\b", ",", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\bcolon\b", ":", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\s+([,.?!:])", "$1");
        t = Regex.Replace(t, @"\bi\b", "I");
        if (sentenceStart && t.Length > 0) t = char.ToUpper(t[0]) + t.Substring(1);
        char last = t[t.Length - 1];
        sentenceStart = (last == '.' || last == '?' || last == '!');
        SendText(t + " ");
    }

    static void SendText(string s)
    {
        var inputs = new INPUT[s.Length * 2];
        int n = 0;
        foreach (char ch in s)
        {
            var down = new INPUT(); down.type = 1; down.U.ki.wScan = ch; down.U.ki.dwFlags = 0x0004;        // KEYEVENTF_UNICODE
            var up = new INPUT(); up.type = 1; up.U.ki.wScan = ch; up.U.ki.dwFlags = 0x0004 | 0x0002;     // ... | KEYUP
            inputs[n++] = down; inputs[n++] = up;
        }
        SendInput((uint)n, inputs, Marshal.SizeOf(typeof(INPUT)));
    }
}
