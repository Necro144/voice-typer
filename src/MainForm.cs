using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Speech.Recognition;
using System.Windows.Forms;

class MainForm : Form
{
    const int HOTKEY_ID = 1;
    static readonly Color Green = Color.FromArgb(34, 139, 84);
    static readonly Color Red = Color.FromArgb(200, 50, 50);
    static readonly Color Grey = Color.FromArgb(110, 110, 120);
    static readonly Color Ink = Color.FromArgb(30, 30, 40);

    readonly Settings settings = Settings.Load();
    readonly Dictation dictation = new Dictation();
    readonly Icon iconOff, iconOn;
    NotifyIcon tray;
    ToolStripMenuItem trayToggle;
    Timer focusTimer;
    bool exiting;
    int level;

    Button btnToggle;
    Panel meter;
    Label lblStatus, lblLangNote, lblStrict;
    ComboBox cmbLang;
    TrackBar tbStrict;
    CheckBox chkSounds, chkPunct, chkCap;
    TextBox txtHeard;

    public MainForm()
    {
        iconOff = LoadIcon("VoiceTyper.tray-off.ico");
        iconOn = LoadIcon("VoiceTyper.tray-on.ico");
        BuildWindow();
        dictation.Typed += delegate (string t) { if (IsHandleCreated) BeginInvoke(new Action(delegate { txtHeard.Text = t; })); };
        dictation.Level += delegate (int v) { level = v; if (IsHandleCreated) BeginInvoke(new Action(delegate { meter.Invalidate(); })); };
    }

    static Icon LoadIcon(string name)
    {
        using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            return new Icon(s, new Size(32, 32));
    }

    // ---------------------------------------------------------------- window

    void BuildWindow()
    {
        Text = "Voice Typer";
        Icon = iconOff;
        Font = new Font("Segoe UI", 9f);
        BackColor = Color.White;
        ForeColor = Ink;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(440, 556);

        var title = new Label { Text = "Voice Typer", Font = new Font("Segoe UI Semibold", 18f), Location = new Point(18, 12), AutoSize = true };
        var sub = new Label { Text = "Say it and it gets typed where your cursor is.", ForeColor = Grey, Location = new Point(21, 52), AutoSize = true };

        btnToggle = new Button
        {
            Text = "Start dictation", Location = new Point(20, 84), Size = new Size(400, 54),
            FlatStyle = FlatStyle.Flat, BackColor = Green, ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 13f), Cursor = Cursors.Hand
        };
        btnToggle.FlatAppearance.BorderSize = 0;
        btnToggle.Click += delegate { Toggle(); };

        meter = new Panel { Location = new Point(20, 148), Size = new Size(400, 8), BackColor = Color.FromArgb(232, 232, 238) };
        meter.Paint += delegate (object s, PaintEventArgs e)
        {
            int w = dictation.Running ? meter.Width * Math.Max(0, Math.Min(100, level)) / 100 : 0;
            using (var b = new SolidBrush(Green)) e.Graphics.FillRectangle(b, 0, 0, w, meter.Height);
        };

        lblStatus = new Label { Text = "Click into a text box in another program, then press Start.", ForeColor = Grey, Location = new Point(20, 164), Size = new Size(400, 36) };

        var lblLang = new Label { Text = "Language", Font = new Font("Segoe UI Semibold", 9.5f), Location = new Point(20, 210), AutoSize = true };
        cmbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(20, 232), Width = 400 };
        lblLangNote = new Label { ForeColor = Grey, Location = new Point(20, 260), Size = new Size(400, 34) };
        var linkLang = new LinkLabel { Text = "Add more languages in Windows", Location = new Point(20, 296), AutoSize = true };
        linkLang.LinkClicked += delegate { OpenSettingsPage(); };

        lblStrict = new Label { Font = new Font("Segoe UI Semibold", 9.5f), Location = new Point(20, 328), AutoSize = true };
        tbStrict = new TrackBar { Minimum = 0, Maximum = 100, TickFrequency = 25, LargeChange = 10, Location = new Point(14, 348), Width = 412, Height = 40, Value = settings.Strictness };
        var strictHint = new Label { Text = "Low types more but may add stray words. High skips phrases Windows is unsure about.", ForeColor = Grey, Location = new Point(20, 388), Size = new Size(400, 34) };

        chkSounds = new CheckBox { Text = "Play a tone when dictation starts and stops", Location = new Point(20, 428), AutoSize = true, Checked = settings.Sounds };
        chkPunct = new CheckBox { Text = "Spoken punctuation (comma, full stop, question mark...)", Location = new Point(20, 452), AutoSize = true, Checked = settings.Punctuation };
        chkCap = new CheckBox { Text = "Capitalise the start of sentences", Location = new Point(20, 476), AutoSize = true, Checked = settings.Capitalise };

        var lblHeard = new Label { Text = "Last typed", ForeColor = Grey, Location = new Point(20, 506), AutoSize = true };
        txtHeard = new TextBox { ReadOnly = true, Location = new Point(20, 526), Width = 400, BackColor = Color.FromArgb(246, 246, 249), BorderStyle = BorderStyle.FixedSingle };

        Controls.AddRange(new Control[] { title, sub, btnToggle, meter, lblStatus, lblLang, cmbLang, lblLangNote, linkLang,
            lblStrict, tbStrict, strictHint, chkSounds, chkPunct, chkCap, lblHeard, txtHeard });

        FillLanguages();
        UpdateStrictLabel();
        cmbLang.SelectedIndexChanged += delegate { SaveChoices(); };
        tbStrict.ValueChanged += delegate { UpdateStrictLabel(); SaveChoices(); };
        chkSounds.CheckedChanged += delegate { SaveChoices(); };
        chkPunct.CheckedChanged += delegate { SaveChoices(); };
        chkCap.CheckedChanged += delegate { SaveChoices(); };
    }

    sealed class LanguageItem
    {
        public RecognizerInfo Info;
        public override string ToString() { return Info.Culture.DisplayName; }
    }

    void FillLanguages()
    {
        var installed = Dictation.Installed();
        int pick = 0;
        for (int i = 0; i < installed.Count; i++)
        {
            cmbLang.Items.Add(new LanguageItem { Info = installed[i] });
            if (installed[i].Culture.Name.Equals(settings.Language, StringComparison.OrdinalIgnoreCase)) pick = i;
        }
        if (installed.Count == 0)
        {
            cmbLang.Enabled = false;
            btnToggle.Enabled = false;
            lblLangNote.Text = "Windows has no speech recognizer installed on this PC. Add one in Windows Settings.";
            return;
        }
        cmbLang.SelectedIndex = pick;
        lblLangNote.Text = installed.Count == 1
            ? "Only " + installed[0].Culture.DisplayName + " is installed. Windows can add more, such as English (United Kingdom), French, German or Spanish."
            : installed.Count + " speech languages are installed on this PC.";
    }

    void UpdateStrictLabel() { lblStrict.Text = "Strictness: " + tbStrict.Value; }

    void SaveChoices()
    {
        var item = cmbLang.SelectedItem as LanguageItem;
        if (item != null) settings.Language = item.Info.Culture.Name;
        settings.Strictness = tbStrict.Value;
        settings.Sounds = chkSounds.Checked;
        settings.Punctuation = chkPunct.Checked;
        settings.Capitalise = chkCap.Checked;
        settings.Save();
    }

    static void OpenSettingsPage()
    {
        try { Process.Start("ms-settings:regionlanguage"); } catch { }
    }

    // ---------------------------------------------------------------- tray, hotkey, focus

    // Called once from Program after the window exists.
    public void StartBackground()
    {
        trayToggle = new ToolStripMenuItem("Start dictation  (Ctrl+Alt+D)");
        trayToggle.Click += delegate { Toggle(); };
        var open = new ToolStripMenuItem("Open Voice Typer");
        open.Click += delegate { ShowWindow(); };
        var exit = new ToolStripMenuItem("Exit");
        exit.Click += delegate { ExitApp(); };
        var menu = new ContextMenuStrip();
        menu.Items.Add(trayToggle);
        menu.Items.Add(open);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);

        tray = new NotifyIcon { Icon = iconOff, Text = "Voice Typer (off)", ContextMenuStrip = menu, Visible = true };
        tray.MouseClick += delegate (object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) Toggle(); };

        focusTimer = new Timer { Interval = 200 };
        focusTimer.Tick += delegate { Native.RememberWindow(); };
        focusTimer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!Native.RegisterHotKey(Handle, HOTKEY_ID, Native.MOD_CONTROL | Native.MOD_ALT, (uint)Keys.D))
            lblStatus.Text = "Ctrl+Alt+D is used by another program. Use the Start button or the tray icon instead.";
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID) Toggle();
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!exiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();   // keep running in the tray
            return;
        }
        base.OnFormClosing(e);
    }

    void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    void ExitApp()
    {
        exiting = true;
        dictation.Stop();
        Native.UnregisterHotKey(Handle, HOTKEY_ID);
        if (focusTimer != null) focusTimer.Stop();
        if (tray != null) { tray.Visible = false; tray.Dispose(); }
        Application.Exit();
    }

    // ---------------------------------------------------------------- start / stop

    void Toggle()
    {
        if (dictation.Running) StopDictation(); else StartDictation();
        Native.RestoreWindow();   // after a tray or button click, go back to the window you were typing in
    }

    void StartDictation()
    {
        SaveChoices();
        string error;
        if (!dictation.Start(settings, out error))
        {
            lblStatus.ForeColor = Red;
            lblStatus.Text = "Could not start: " + error;
            return;
        }
        btnToggle.Text = "Stop dictation";
        btnToggle.BackColor = Red;
        lblStatus.ForeColor = Green;
        lblStatus.Text = "Listening. Words go to the last program you used.";
        if (tray != null) { tray.Icon = iconOn; tray.Text = "Voice Typer (listening)"; trayToggle.Text = "Stop dictation  (Ctrl+Alt+D)"; }
        Icon = iconOn;
        if (settings.Sounds) Native.Tone(true);
    }

    void StopDictation()
    {
        dictation.Stop();
        level = 0;
        meter.Invalidate();
        btnToggle.Text = "Start dictation";
        btnToggle.BackColor = Green;
        lblStatus.ForeColor = Grey;
        lblStatus.Text = "Stopped. Click into a text box in another program, then press Start.";
        if (tray != null) { tray.Icon = iconOff; tray.Text = "Voice Typer (off)"; trayToggle.Text = "Start dictation  (Ctrl+Alt+D)"; }
        Icon = iconOff;
        if (settings.Sounds) Native.Tone(false);
    }
}
