@echo off
setlocal
if not exist packages\System.Data.SQLite.Core.1.0.118 echo Run: nuget restore ClinicQueueSystem.sln
"%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" ClinicQueueSystem.sln /t:Restore,Build /p:Configuration=Release
if errorlevel 1 exit /b 1
echo Build complete: bin\Release\ClinicQueueSystem.exe
