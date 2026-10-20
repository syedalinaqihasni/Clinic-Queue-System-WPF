# Clinic Queue System v1.0

Windows desktop queue/ticket application for a small clinic. Built with C# WinForms, .NET Framework 4.8 and SQLite.

## V1.0 included
- Login with ADMIN / SUPERVISOR / RECEPTIONIST roles
- General OPD, Consultant OPD and ENT OPD seeded
- Independent sequence per Department + Shift + Date: G001, G002... / C001... / E001...
- Automatic current-shift selection from configured shift times
- Manual shift selection remains available when automatic shift mode is disabled
- New shift and new date reset the sequence to 001
- Doctor management and department assignment
- Department pricing and ticket price snapshot
- Counter and printer assignment
- Raw ESC/POS printing through the Windows printer spooler
- Bold/large ticket number, date, time, department, doctor, shift, counter and fee
- Ticket voiding and reprint tracking
- Audit log
- Ticket search/listing
- Daily/date-range summary and CSV export
- Database backup and restore
- Clinic name, currency, footer and automatic-shift settings

## Default login
Username: admin
Password: admin123

Change the password immediately from Administration > Users.

## Hardware
Use a thermal POS printer installed in Windows. In Administration > Printers, enter the exact Windows printer name. Then assign that printer to a Counter. The printer must accept RAW jobs/ESC-POS commands.

Typical 58mm printers work best with `paper_width=58`; typical 80mm printers with `paper_width=80`. V1.0 uses the standard ESC/POS cut command and ASCII text.

## Build
1. Windows 10/11 or Windows 7 SP1 with .NET Framework 4.8.
2. Visual Studio 2022 with .NET desktop development, or MSBuild Build Tools.
3. Restore NuGet package `System.Data.SQLite.Core` 1.0.118.
4. Open `ClinicQueueSystem.sln` and Build > Release.
5. Run `bin\\Release\\ClinicQueueSystem.exe`.

The database is stored in `%APPDATA%\\ClinicQueueSystem\\clinic_queue.db`.

## Important multi-counter note
SQLite is local to the PC. V1.0 is intended for one Windows PC/counter. If several reception PCs must share one live sequence, use a central server/API or shared database architecture in V2 rather than placing the SQLite file on a network share.

## Windows 7
The application targets .NET Framework 4.8, which requires Windows 7 SP1 or later. Test the exact thermal-printer driver on the target PC before deployment.


## Windows 10 build target (v1.0.1)

This release is prepared specifically for Windows 10 desktop deployment. It targets .NET Framework 4.8 and includes a Windows 10 application compatibility manifest and DPI settings.

### Recommended Windows 10 setup
- Windows 10 64-bit (22H2 recommended)
- Visual Studio 2022 Community/Professional/Build Tools
- .NET desktop development workload
- .NET Framework 4.8 Runtime/Developer Pack
- Thermal printer installed in Windows with its manufacturer driver

### Build
From the project directory, double-click `build.bat`, or run it from Command Prompt. It restores NuGet packages and creates:

`bin\Release\ClinicQueueSystem.exe`

### Run
Double-click `bin\Release\ClinicQueueSystem.exe`.

Default login: `admin` / `admin123`. Change the password before production use.


## Main reception window
The main reception window is implemented in `Forms\MainForm.cs` and is opened after a successful login. It is built in code, so a separate `MainForm.Designer.cs` file is not required. The window includes department, doctor, shift, counter, fee, next-ticket preview, print-ticket action, clock, and administration/logout controls.

## Visual Studio 2026
Open `ClinicQueueSystem.sln`, ensure the .NET Framework 4.8 Developer/Targeting Pack and the .NET desktop development workload are installed, restore NuGet packages, select Release, then Build > Rebuild Solution.
