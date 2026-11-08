using System;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Recognition;
using System.Text.RegularExpressions;

// Listens to the default microphone with Windows' own offline recognizer and types what it hears.
class Dictation
{
    SpeechRecognitionEngine engine;
    Settings settings;
    CultureInfo culture = CultureInfo.InvariantCulture;
    bool sentenceStart = true;
    int lastLength = 0;

    public bool Running { get; private set; }
    public event Action<string> Typed;      // the text that was just typed
    public event Action<int> Level;         // microphone level, 0..100

    public static List<RecognizerInfo> Installed()
    {
        var list = new List<RecognizerInfo>(SpeechRecognitionEngine.InstalledRecognizers());
        list.Sort(delegate (RecognizerInfo a, RecognizerInfo b) { return string.Compare(a.Culture.DisplayName, b.Culture.DisplayName, StringComparison.CurrentCultureIgnoreCase); });
        return list;
    }

    static RecognizerInfo Pick(string cultureName)
    {
        var all = Installed();
        if (all.Count == 0) return null;
        foreach (var r in all) if (r.Culture.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase)) return r;
        foreach (var r in all) if (r.Culture.Name.Equals(CultureInfo.CurrentUICulture.Name, StringComparison.OrdinalIgnoreCase)) return r;
        return all[0];
    }

    public bool Start(Settings s, out string error)
    {
        error = null;
        settings = s;
        try
        {
            RecognizerInfo info = Pick(s.Language);
            if (info == null) { error = "Windows has no speech recognizer installed."; return false; }
            culture = info.Culture;
            engine = new SpeechRecognitionEngine(info);
            engine.LoadGrammar(new DictationGrammar());
            try
            {
                // wait a little longer before deciding a sentence has ended, so phrases are not cut in half
                engine.EndSilenceTimeout = TimeSpan.FromMilliseconds(450);
                engine.EndSilenceTimeoutAmbiguous = TimeSpan.FromMilliseconds(800);
            }
            catch { }
            engine.SetInputToDefaultAudioDevice();
            engine.SpeechRecognized += OnRecognized;
            engine.AudioLevelUpdated += OnLevel;
            engine.RecognizeAsync(RecognizeMode.Multiple);
            sentenceStart = true;
            lastLength = 0;
            Running = true;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Cleanup();
            return false;
        }
    }

    public void Stop()
    {
        Running = false;
        try { if (engine != null) engine.RecognizeAsyncCancel(); } catch { }
        Cleanup();
    }

    void Cleanup()
    {
        Running = false;
        if (engine == null) return;
        try { engine.SpeechRecognized -= OnRecognized; engine.AudioLevelUpdated -= OnLevel; engine.Dispose(); } catch { }
        engine = null;
    }

    void OnLevel(object sender, AudioLevelUpdatedEventArgs e)
    {
        Action<int> handler = Level;
        if (handler != null) handler(e.AudioLevel);
    }

    void OnRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        if (!Running || e.Result == null || e.Result.Confidence < settings.MinConfidence) return;
        string text = e.Result.Text.Trim();
        if (text.Length == 0) return;
        bool english = culture.TwoLetterISOLanguageName == "en";

        if (english)
        {
            string lower = text.ToLowerInvariant();
            if (lower == "scratch that" || lower == "delete that")
            {
                Native.RestoreWindow();
                Native.Backspaces(lastLength);
                lastLength = 0;
                return;
            }
            if (settings.Punctuation)
            {
                text = Regex.Replace(text, @"\bquestion mark\b", "?", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"\bexclamation (mark|point)\b", "!", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"\b(full stop|period)\b", ".", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"\bcomma\b", ",", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"\bcolon\b", ":", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"\s+([,.?!:])", "$1");
            }
            text = Regex.Replace(text, @"\bi\b", "I");
        }

        if (settings.Capitalise && sentenceStart && text.Length > 0) text = char.ToUpper(text[0], culture) + text.Substring(1);
        char last = text[text.Length - 1];
        sentenceStart = (last == '.' || last == '?' || last == '!');

        text += " ";
        Native.RestoreWindow();
        Native.TypeText(text);
        lastLength = text.Length;

        Action<string> handler = Typed;
        if (handler != null) handler(text.Trim());
    }
}
