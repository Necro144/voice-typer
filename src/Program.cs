using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--selftest") return SelfTest();

        bool first;
        using (var mutex = new Mutex(true, "VoiceTyper-single-instance", out first))
        {
            if (!first) return 0;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new MainForm();
            IntPtr handle = form.Handle;        // creates the window so the hotkey can register
            form.StartBackground();
            if (!(args.Length > 0 && args[0] == "--tray")) form.Show();
            Application.Run();
        }
        return 0;
    }

    // "VoiceTyper.exe --selftest" builds the window and checks the speech setup without opening anything or touching the microphone.
    static int SelfTest()
    {
        string log = Path.Combine(Path.GetTempPath(), "voicetyper-selftest.txt");
        try
        {
            var lines = new System.Collections.Generic.List<string>();
            lines.Add("settings loaded: " + (Settings.Load() != null));
            foreach (var r in Dictation.Installed()) lines.Add("recognizer: " + r.Culture.Name + " (" + r.Culture.DisplayName + ")");
            Application.EnableVisualStyles();
            var form = new MainForm();
            lines.Add("window built, size " + form.ClientSize.Width + "x" + form.ClientSize.Height);
            lines.Add("RESULT: OK");
            File.WriteAllLines(log, lines.ToArray());
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(log, "RESULT: FAILED\r\n" + ex);
            return 1;
        }
    }
}
