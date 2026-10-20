# Clinic Queue System

Windows desktop starter implementation for a clinic thermal-ticket queue system.

## Stack
- C# WinForms
- .NET Framework 4.8
- SQLite
- Windows printer spooler (starter ticket printing)

## Default login
Username: `admin`
Password: `admin123`

Change this password immediately after first login.

## Numbering
Sequence is independent for `date + shift + department`, producing codes such as G001, C001 and E001. A printed ticket is never reused; voiding does not decrement the sequence.

## Build
Open `ClinicQueueSystem.csproj` in Visual Studio 2019/2022 with .NET Framework 4.8 developer tools installed and restore NuGet packages.

For Windows 7, test on the exact Windows 7 SP1 machine and printer driver you will deploy to. .NET Framework 4.8 requires Windows 7 SP1.

## Database
Stored in `%APPDATA%\\ClinicQueueSystem\\clinic_queue.db`.

## Important
This is a working starter codebase, not yet a production-certified clinic/POS application. Before deployment, add stronger role/permission enforcement, robust raw ESC/POS printing, printer retry/transaction handling, encrypted/secured backups, validation, installer/signing, and full testing.
