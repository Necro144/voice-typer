using System;
using System.Collections.Generic;
using System.IO;

// Your choices, saved as a plain text file in %APPDATA%\VoiceTyper\settings.ini.
class Settings
{
    public string Language = "";        // culture name such as en-GB; empty means "pick for me"
    public bool Sounds = true;
    public bool Punctuation = true;
    public bool Capitalise = true;
    public int Strictness = 33;         // 0..100, higher ignores phrases Windows is unsure about

    public float MinConfidence { get { return Strictness * 0.6f / 100f; } }

    static string FilePath
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VoiceTyper", "settings.ini"); }
    }

    public static Settings Load()
    {
        var s = new Settings();
        try
        {
            if (!File.Exists(FilePath)) return s;
            foreach (string line in File.ReadAllLines(FilePath))
            {
                int eq = line.IndexOf('=');
                if (eq < 1) continue;
                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string val = line.Substring(eq + 1).Trim();
                switch (key)
                {
                    case "language": s.Language = val; break;
                    case "sounds": s.Sounds = val != "0"; break;
                    case "punctuation": s.Punctuation = val != "0"; break;
                    case "capitalise": s.Capitalise = val != "0"; break;
                    case "strictness":
                        int n;
                        if (int.TryParse(val, out n)) s.Strictness = Math.Max(0, Math.Min(100, n));
                        break;
                }
            }
        }
        catch { }
        return s;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            var lines = new List<string>();
            lines.Add("language=" + Language);
            lines.Add("sounds=" + (Sounds ? "1" : "0"));
            lines.Add("punctuation=" + (Punctuation ? "1" : "0"));
            lines.Add("capitalise=" + (Capitalise ? "1" : "0"));
            lines.Add("strictness=" + Strictness);
            File.WriteAllLines(FilePath, lines.ToArray());
        }
        catch { }
    }
}
