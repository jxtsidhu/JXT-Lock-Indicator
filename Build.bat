@echo off
setlocal
cd /d "%~dp0"
set EXE=JXT KEY LOCK INDICATOR.exe
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Could not find the .NET Framework compiler. Enable ".NET Framework 4.x" in Windows Features.
  pause & exit /b 1
)
echo Closing any running copy...
taskkill /f /im "%EXE%" >nul 2>&1
timeout /t 1 >nul
echo Building JXT KEY LOCK INDICATOR...
"%CSC%" /nologo /target:winexe /optimize+ /out:"%EXE%" /r:System.Windows.Forms.dll /r:System.Drawing.dll JXTKeyLock.cs
if errorlevel 1 (
  echo.
  echo ===== BUILD FAILED - please copy the error lines above and send them =====
  pause & exit /b 1
)
echo.
echo Build OK. Starting the app - look for its icon near the clock (click ^ if hidden).
start "" "%EXE%"
timeout /t 4 >nul
