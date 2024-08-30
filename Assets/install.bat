:: Check privileges 
net file 1>NUL 2>NUL
if not '%errorlevel%' == '0' (
    powershell Start-Process -FilePath "%0" -ArgumentList "%cd%" -verb runas >NUL 2>&1
    timeout 1
    taskkill /im explorer.exe /f
    start explorer.exe
    exit /b
)

%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe /codebase %~dp0\ComView.Deskband.dll