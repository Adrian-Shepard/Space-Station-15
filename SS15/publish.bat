@echo off
echo ==========================================
echo  SS15 - Publishing Release build...
echo ==========================================
cd /d %~dp0

dotnet publish src\SS15.Client -c Release -r win-x64 --self-contained true /p:PublishReadyToRun=true -o publish

echo.
echo Build completed! Executable is in the 'publish' folder.
pause