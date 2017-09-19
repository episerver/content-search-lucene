@echo off

powershell .\build\pack.ps1 %1
EXIT /B %errorlevel%