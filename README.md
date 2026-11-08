# Voice Typer

**Talk instead of type, in any Windows app.** Voice Typer is a tiny tray program (about 10 KB) that turns your voice into typed text wherever your cursor is: browsers, Discord, Notepad, Word, search boxes, chat apps and more.

- **Works offline.** It uses the speech recognizer built into Windows. Nothing is sent to the internet, no account is needed, and nothing is saved or logged.
- **Works everywhere.** It types like a keyboard, so any app that accepts typing accepts your voice.
- **One key to start and stop.** Press `Ctrl+Alt+D` or click the tray icon.
- **You always know if it's on.** A rising two-note tone means on, a falling tone means off, and the tray dot turns red while listening.

## Download and use

1. Download `VoiceTyper.exe` from the [latest release](../../releases/latest).
2. Double-click it. A grey dot appears in the system tray (near the clock). If you don't see it, click the small `^` arrow.
3. Click inside any text box.
4. Press **Ctrl+Alt+D**. You hear a rising tone and the dot turns red.
5. Speak. Your words are typed where the cursor is.
6. Press **Ctrl+Alt+D** again to stop (falling tone).

To quit, right-click the tray icon and choose **Exit**.

## Spoken punctuation

| Say | You get |
|---|---|
| "comma" | , |
| "period" or "full stop" | . |
| "question mark" | ? |
| "exclamation mark" or "exclamation point" | ! |
| "colon" | : |

The first letter of each sentence is capitalised, and a lone "i" becomes "I". There is deliberately no "new line" command, so it can never press Enter and send a chat message by accident.

## Requirements

- Windows 10 or 11 (64-bit), with the .NET Framework 4.x that ships with Windows.
- A working microphone set as the **default recording device** (Settings > System > Sound > Input). If you change the default device, restart Voice Typer.
- The **English (United States)** speech recognizer, which Windows installs with English (US).

## Privacy and safety

- No network access. The program contains no networking code, no telemetry and no logging.
- It listens only while the tray dot is red, and it only types text. It never reads what is on your screen.
- It is a keyboard-style tool, so some antivirus programs may warn about unsigned programs that send keystrokes. That is expected. You can read the full source in `VoiceTyper.cs` (about 200 lines) and build it yourself.
- Check your download against the SHA-256 shown in the release notes.

## Limits and troubleshooting

| Problem | Fix |
|---|---|
| Nothing is typed | Check that the Windows **Test your microphone** bar moves when you speak, and that the right device is the default input. Restart Voice Typer after changing it. |
| It can't type into one app | Apps running as administrator ignore normal programs. Run Voice Typer as administrator too. Some games and secure fields block simulated typing. |
| `Ctrl+Alt+D` doesn't work | Another program owns that shortcut. Click the tray icon instead. |
| Mistakes in the text | The built-in recognizer is less accurate than online services. Speak in full sentences at a steady pace and keep the microphone close. |
| Another language | Only English (US) is supported right now. |

## Build from source

You only need the C# compiler that comes with Windows. In PowerShell, from this folder:

```powershell
$speech = (Get-ChildItem 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Speech' -Recurse -Filter System.Speech.dll | Select-Object -First 1).FullName
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:winexe /optimize+ /out:VoiceTyper.exe /win32manifest:app.manifest "/r:$speech" /r:System.Windows.Forms.dll /r:System.Drawing.dll VoiceTyper.cs
```

(`build.ps1` contains the same commands.)

## How it works

A hidden window registers the global hotkey. When dictation is on, `System.Speech`'s dictation grammar recognizes your speech from the default microphone. Each recognized phrase gets punctuation and capitalisation applied, then is typed into the focused window with the Windows `SendInput` API using Unicode key events.

## Roadmap

- Settings for the hotkey and the sounds
- Option to start with Windows
- More languages, where Windows has a recognizer for them

## License

Not chosen yet.
