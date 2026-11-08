@echo off
rem Builds VoiceTyper.exe with the C# compiler that ships with Windows. Run from this folder.
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
for /r "%WINDIR%\Microsoft.NET\assembly\GAC_MSIL\System.Speech" %%f in (System.Speech.dll) do set "SPEECH=%%f"
"%CSC%" /nologo /target:winexe /optimize+ /out:VoiceTyper.exe ^
  /win32icon:icons\app.ico /win32manifest:app.manifest ^
  /resource:icons\tray-off.ico,VoiceTyper.tray-off.ico /resource:icons\tray-on.ico,VoiceTyper.tray-on.ico ^
  /r:"%SPEECH%" /r:System.Windows.Forms.dll /r:System.Drawing.dll src\*.cs
