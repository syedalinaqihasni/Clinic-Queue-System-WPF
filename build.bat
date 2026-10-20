@echo off
setlocal
cd /d "%~dp0"

echo ===============================================
echo Clinic Queue System - Windows 10 Release Build
echo ===============================================
echo.

where msbuild >nul 2>&1
if errorlevel 1 (
  if exist "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
  ) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
  ) else (
    echo MSBuild was not found.
    echo Install Visual Studio 2022 with the .NET desktop development workload.
    pause
    exit /b 1
  )
) else (
  set "MSBUILD=msbuild"
)

if not exist packages\System.Data.SQLite.Core.1.0.118 (
  echo Restoring NuGet packages...
  nuget restore ClinicQueueSystem.sln
  if errorlevel 1 (
    echo NuGet restore failed. Install NuGet or restore packages from Visual Studio.
    pause
    exit /b 1
  )
)

echo Building Release...
"%MSBUILD%" ClinicQueueSystem.sln /t:Build /p:Configuration=Release /m
if errorlevel 1 (
  echo.
  echo BUILD FAILED.
  pause
  exit /b 1
)

echo.
echo BUILD SUCCESSFUL.
echo EXE: %CD%\bin\Release\ClinicQueueSystem.exe
echo.
pause
