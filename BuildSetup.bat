@echo off
"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe"

mkdir Releases
copy GitMind\bin\Debug\GitMind.exe Releases\GitMindSetup.exe /Y 

pause