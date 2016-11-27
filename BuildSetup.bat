@echo off
echo Building setup ...
echo.

del GitMindSetup.exe >nul 2>&1
del version.txt >nul 2>&1

call nuget restore GitMind.sln
echo.

"%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" GitMind.sln /t:rebuild /v:m /nologo

echo.
copy GitMind\bin\Debug\GitMind.exe GitMindSetup.exe /Y 

PowerShell -Command "& {(Get-Item GitMindSetup.exe).VersionInfo.FILEVERSION }" > version.txt
echo.
echo GitMindSetup.exe version:
type version.txt 

echo.
echo.
pause