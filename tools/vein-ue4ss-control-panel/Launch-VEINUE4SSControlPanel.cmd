@echo off
setlocal
set "APP=%~dp0bin\Release\net8.0-windows\win-x64\publish\Vein Dump Manager.exe"
if exist "%APP%" (
    start "" "%APP%"
    exit /b 0
)
dotnet run --project "%~dp0Vein.Ue4ss.DumpLog.csproj"
