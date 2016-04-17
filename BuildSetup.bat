@echo off
echo Building setup ...
echo.

del GitMindSetup.exe >nul 2>&1
del version.txt >nul 2>&1

call nuget restore GitMind.sln
echo.

"%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe" GitMind.sln /t:rebuild /v:m /nologo

echo.
copy GitMind\bin\Debug\GitMind.exe GitMindSetup.exe /Y 

PowerShell -Command "& {(Get-Item GitMindSetup.exe).VersionInfo.FILEVERSION }" > version.txt
echo.
echo GitMindSetup.exe version:
type version.txt 

echo.
echo.
pause