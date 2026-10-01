# Clinic Queue System WPF v1.1 (.NET Framework 4.8)

This is a WPF/XAML desktop application intended for Windows 10 and Visual Studio 2026. It uses JSON files instead of SQLite or any SQL database.

## Included
- WPF/XAML UI (`MainWindow.xaml`, `App.xaml`)
- .NET Framework 4.8 target
- JSON storage under `%APPDATA%\ClinicQueueSystem\clinic-data.json`
- First-run sample departments (General OPD, Consultant OPD, ENT OPD), shifts and counter
- Admin login with PBKDF2 password hash
- Per-date, per-shift, per-department numbering (G001/C001/E001)
- Ticket history, reprint/void tracking, CSV export
- Windows print spooler ticket printing (uses the installed Windows printer driver)
- JSON backup

## Build in Visual Studio 2026
1. Extract the ZIP.
2. Install the **.NET desktop development** workload and **.NET Framework 4.8 Developer Pack/Targeting Pack** from Visual Studio Installer.
3. Open `ClinicQueueSystem.sln`.
4. Select **Release | Any CPU**.
5. Choose **Build > Rebuild Solution**. Or run `build.bat`.
6. Run `bin\Release\ClinicQueueSystem.exe`.

First login: `admin` / `admin123`. Change/remove this starter password before clinic deployment. Data is stored in `%APPDATA%\ClinicQueueSystem\clinic-data.json`; a `.bak` file is kept on save when possible. Back up the whole folder regularly.

## JSON vs CSV
JSON is the primary storage format because the app has related data and nested collections. CSV is available for ticket exports. Avoid manually editing the JSON while the app is running. The app automatically creates a JSON data file at first launch.

## Important limitations
This is a starter source package and has not been compiled or hardware-tested on Windows in this environment. The Admin master-data buttons currently display records and guidance; they are not yet full add/edit/delete screens. User creation/password changes, a full role-permission matrix, automatic shift selection, raw ESC/POS commands/cutting, multi-PC shared numbering, and installer packaging require additional implementation/testing. Printing uses the installed Windows printer driver; configure an exact printer name in `clinic-data.json` under `Printers[].WindowsPrinterName`. For a shared clinic with multiple reception PCs, a single JSON file on a network share is not safe for concurrent writes; use a central service/database for multi-PC deployment.
