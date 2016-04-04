@echo off
echo Builing setup ...
echo.

rmdir Releases /S /Q

"%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe" GitMind.sln /t:rebuild /v:m /nologo

echo.
mkdir Releases >nul 2>&1
copy GitMind\bin\Debug\GitMind.exe Releases\GitMindSetup.exe /Y 

PowerShell -Command "& {(Get-Item Releases\GitMindSetup.exe).VersionInfo.FILEVERSION }" > Releases\version.txt
echo.
echo GitMindSetup.exe version:
type Releases\version.txt 

echo.
echo.
pause