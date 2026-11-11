@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo .NET Framework 4.8 compiler was not found.
  exit /b 1
)
if not exist "dist" mkdir "dist"
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32manifest:src\app.manifest /out:dist\AppleDisplayBrightness.exe src\StudioDisplayBrightness.cs src\Program.cs
if errorlevel 1 exit /b 1
echo Application built: dist\AppleDisplayBrightness.exe
if "%~1"=="" (
  echo To build the installer, pass the full path to Inno Setup ISCC.exe.
  exit /b 0
)
if not exist "%~1" (
  echo Inno Setup compiler was not found at the supplied path.
  exit /b 1
)
"%~1" /Qp installer\setup.iss
if errorlevel 1 exit /b 1
echo Installer built: dist\AppleDisplayBrightness-1.0.0-Setup.exe
exit /b 0
