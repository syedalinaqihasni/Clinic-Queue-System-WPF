@echo off
setlocal
set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not exist "%MSBUILD%" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2026\Community\MSBuild\Current\Bin\MSBuild.exe"
if not exist "%MSBUILD%" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%MSBUILD%" if /I "%MSBUILD:~-11%"=="vswhere.exe" for /f "usebackq tokens=*" %%i in (`"%MSBUILD%" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not exist "%MSBUILD%" (echo MSBuild not found. Install Visual Studio with .NET desktop development. pause & exit /b 1)
"%MSBUILD%" ClinicQueueSystem.sln /t:Restore,Rebuild /p:Configuration=Release /p:Platform="Any CPU" /m
if errorlevel 1 (echo BUILD FAILED & pause & exit /b 1)
echo Build succeeded. Output: bin\Release\ClinicQueueSystem.exe
pause