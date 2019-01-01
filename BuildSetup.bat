@echo off

powershell -ExecutionPolicy RemoteSigned -File .\Build.ps1 -configuration "Release" -Target Build-Setup

echo.
echo.
pause