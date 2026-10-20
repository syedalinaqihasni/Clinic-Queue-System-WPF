@echo off
setlocal EnableExtensions
cd /d "%~dp0"

 echo ===============================================
 echo Clinic Queue System v1.0.3 - .NET Framework 4.8
 echo ===============================================
 echo.

set "MSBUILD="
where msbuild >nul 2>&1
if not errorlevel 1 set "MSBUILD=msbuild"

if "%MSBUILD%"=="" (
  set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
  if exist "%VSWHERE%" (
    for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
      if "%MSBUILD%"=="" set "MSBUILD=%%I"
    )
  )
)

if "%MSBUILD%"=="" (
  echo MSBuild was not found.
  echo Install Visual Studio 2026 and select the .NET desktop development workload.
  echo Also install the .NET Framework 4.8 Developer Pack.
  pause
  exit /b 1
)

echo Using MSBuild: %MSBUILD%
echo.

echo Restoring NuGet packages...
if exist "%ProgramFiles(x86)%\NuGet\nuget.exe" (
  "%ProgramFiles(x86)%\NuGet\nuget.exe" restore ClinicQueueSystem.sln -NonInteractive
) else (
  "%MSBUILD%" ClinicQueueSystem.sln /t:Restore /p:RestorePackagesConfig=true /v:minimal
)
if errorlevel 1 (
  echo NuGet restore failed. In Visual Studio, right-click the solution and choose Restore NuGet Packages.
  pause
  exit /b 1
)

echo.
echo Building Release...
"%MSBUILD%" ClinicQueueSystem.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /m /v:minimal
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Copy the complete error from Visual Studio Output and send it for correction.
  pause
  exit /b 1
)

echo.
echo BUILD SUCCESSFUL.
echo EXE: %CD%\bin\Release\ClinicQueueSystem.exe
echo.
pause
