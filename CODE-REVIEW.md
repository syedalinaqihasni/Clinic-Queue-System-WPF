# Code review notes — v1.2.1

## Fixes made
- `DeleteSelected<T>` now declares `where T : class`, which is required because it uses `as T`.
- The username column in the user grid is read-only so account edits map to a stable username.
- JSON/master data validation now rejects null/empty records, invalid shift times, invalid printer widths, duplicate usernames/department codes, unsupported roles, negative prices, and removal of the last active administrator.
- Closing the login dialog without authenticating shuts down the app.
- Version metadata and README updated to 1.2.1.

## Checks performed here
- All `.xaml` files parse as well-formed XML.
- All `Click` and `SelectionChanged` handlers declared in XAML have matching code-behind methods.
- C# files have balanced raw brace counts.

## Important limitation
This environment does not include the Windows WPF/.NET Framework compiler, so a real MSBuild compile and printer test could not be run here. These checks do not prove the entire solution compiles. Open the solution in Visual Studio on Windows and run **Build > Rebuild Solution**. Resolve any remaining environment-specific compiler errors before using it with clinic data.
