@echo off
echo Building setup ...
echo.

del GitMindSetup.exe >nul 2>&1
del version.txt >nul 2>&1


if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" (
  set MSBUILD="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
)
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" (
  set MSBUILD="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
)
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" (
  set MSBUILD="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
)

if exist %MSBUILD% (
  echo Using MSBuild: %MSBUILD%
  echo.


  echo Restore nuget packets ...
  rem call %MSBUILD% /nologo /t:restore /v:m GitMind.sln
  .\Binaries\nuget.exe restore GitMind.sln
  echo.

  echo Building ...
  %MSBUILD% /t:rebuild /v:m /nologo GitMind.sln 
  echo.

  echo Copy to Setup file ...
  copy GitMind\bin\Debug\GitMind.exe GitMindSetup.exe /Y 

  echo Get built version...
  PowerShell -Command "& {(Get-Item GitMindSetup.exe).VersionInfo.FILEVERSION }" > version.txt
  echo.

  echo GitMindSetup.exe version:
  type version.txt 
  
) else (
  echo.
  echo Error: Failed to locate compatible msbuild.exe
)

echo.
echo.
pause