# Voice Typer

A tiny Windows tray program that types what you say into whatever app you are in.

- **Start / stop:** press `Ctrl+Alt+D` or click the tray icon. Rising tone = on, falling tone = off. Red tray dot = listening.
- **Works in any app** that accepts typing (browsers, Discord, Notepad, Word, ...).
- **Offline:** uses Windows' built-in English speech recognizer. No network, no accounts, nothing saved.
- Say "comma", "period", "question mark", "exclamation mark" or "colon" for punctuation.

## Build
Needs only the .NET Framework compiler that ships with Windows (see `build.ps1`).

## Limits
- English (US) only, since that is the recognizer installed on this PC. Accuracy is lower than online services.
- Cannot type into windows running as administrator unless Voice Typer is also run as administrator.
- Uses the Windows default microphone. Change the default, then restart the program.
