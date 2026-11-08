$speech = (Get-ChildItem 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Speech' -Recurse -Filter System.Speech.dll | Select-Object -First 1).FullName
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:winexe /optimize+ /out:VoiceTyper.exe /win32manifest:app.manifest "/r:$speech" /r:System.Windows.Forms.dll /r:System.Drawing.dll VoiceTyper.cs
