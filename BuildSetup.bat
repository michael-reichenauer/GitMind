@echo off
echo Building setup ...
echo.

rmdir Releases /S /Q >nul 2>&1

call nuget restore GitMind.sln
echo.

"%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe" GitMind.sln /t:rebuild /v:m /nologo

echo.
mkdir Releases >nul 2>&1
copy GitMind\bin\Debug\GitMind.exe GitMindSetup.exe /Y 

PowerShell -Command "& {(Get-Item GitMindSetup.exe).VersionInfo.FILEVERSION }" > version.txt
echo.
echo GitMindSetup.exe version:
type version.txt 

echo.
echo.
pause