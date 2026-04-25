@echo off
echo ==========================================
echo  SS15 - Cleaning, building and running...
echo ==========================================
cd /d %~dp0

echo [1/3] Cleaning old binaries...
rmdir /s /q src\SS15.Client\bin  2>nul
rmdir /s /q src\SS15.Client\obj  2>nul

echo [2/3] Restoring packages...
dotnet restore

echo [3/3] Launching client...
dotnet run --project src\SS15.Client

pause