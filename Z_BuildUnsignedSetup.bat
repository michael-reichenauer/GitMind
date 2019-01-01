@echo off

powershell -ExecutionPolicy RemoteSigned -File .\Build.ps1 -configuration "Release" -Target Build-Unsigned-Setup

echo.
echo.
pause