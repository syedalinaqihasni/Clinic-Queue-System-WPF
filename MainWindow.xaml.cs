using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ClinicQueueSystem.Models;
using ClinicQueueSystem.Services;
using Microsoft.Win32;

namespace ClinicQueueSystem
{
    public partial class MainWindow : Window
    {
        private User _user;

        private class SummaryRow
        {
            public string Period { get; set; }
            public string Department { get; set; }
            public string Doctor { get; set; }
            public string Shift { get; set; }
            public int TicketCount { get; set; }
            public decimal TotalPrice { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            JsonStore.Initialize();
            Login();
            if (_user == null)
            {
                Application.Current.Shutdown();
                return;
            }
            UserLabel.Text = _user.FullName + " (" + _user.Role + ")";
            AdminButton.Visibility =
                _user.Role == "ADMIN" ? Visibility.Visible : Visibility.Collapsed;
            StoragePathLabel.Text = JsonStore.FilePath;
            ClinicNameBox.Text = JsonStore.Data.ClinicName;
            FooterBox.Text = JsonStore.Data.TicketFooter;
            LoadCombos();

            SummaryStartDate.SelectedDate = DateTime.Today;
            SummaryEndDate.SelectedDate = DateTime.Today;

            RefreshGrids();
            UpdateSummary();
            UpdatePreview();
            foreach (
                TabItem tab in (
                    (TabControl)((Grid)Content).Children.OfType<TabControl>().First()
                ).Items
            )
            {
                if ((string)tab.Header == "Admin")
                    tab.Visibility = Visibility.Collapsed;
            }
        }

        private void Login()
        {
            var w = new LoginWindow();
            if (w.ShowDialog() == true)
                _user = w.AuthenticatedUser;
        }

        private void LoadCombos()
        {
            DepartmentCombo.ItemsSource = JsonStore.Data.Departments.Where(x => x.Active).ToList();
            ShiftCombo.ItemsSource = JsonStore.Data.Shifts.Where(x => x.Active).ToList();
            CounterCombo.ItemsSource = JsonStore.Data.Counters.Where(x => x.Active).ToList();
            if (DepartmentCombo.Items.Count > 0)
                DepartmentCombo.SelectedIndex = 0;
            if (ShiftCombo.Items.Count > 0)
                ShiftCombo.SelectedIndex = 0;
            if (CounterCombo.Items.Count > 0)
                CounterCombo.SelectedIndex = 0;
        }

        private void Department_Changed(object s, SelectionChangedEventArgs e)
        {
            var d = DepartmentCombo.SelectedItem as Department;

            DoctorCombo.ItemsSource =
                d == null
                    ? null
                    : JsonStore
                        .Data.Doctors.Where(x =>
                            x.Active
                            && (
                                string.IsNullOrWhiteSpace(x.DepartmentCode)
                                || x.DepartmentCode == d.Code
                            )
                        )
                        .ToList();

            // Set the default fee from the selected department.
            PriceBox.Text =
                d == null ? "0.00" : d.DefaultPrice.ToString("0.00", CultureInfo.InvariantCulture);

            // Update the department preview.
            PreviewDepartment.Text = d == null ? "Select department" : d.Name;

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var d = DepartmentCombo.SelectedItem as Department;
            var sh = ShiftCombo.SelectedItem as Shift;

            if (d == null || sh == null)
            {
                NextTicketLabel.Text = "---";
                return;
            }

            int next = QueueService.GetNextSequence(d, sh);

            NextTicketLabel.Text = d.Code + next.ToString("000");
        }

        private void Save_Click(object s, RoutedEventArgs e)
        {
            try
            {
                var d = DepartmentCombo.SelectedItem as Department;
                var doc = DoctorCombo.SelectedItem as Doctor;
                var sh = ShiftCombo.SelectedItem as Shift;
                var c = CounterCombo.SelectedItem as Counter;

                decimal price;

                if (
                    !decimal.TryParse(
                        PriceBox.Text,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out price
                    )
                    || price < 0
                )
                {
                    throw new InvalidOperationException("Enter a valid non-negative fee.");
                }

                var t = QueueService.Issue(d, doc, sh, c, price, _user.Username);

                // Save the ticket information without printing.
                StatusLabel.Text = "Saved ticket " + t.TicketNumber;
                FooterStatus.Text = "Last ticket: " + t.TicketNumber;

                RefreshGrids();
                UpdatePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ticket save error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Print_Click(object s, RoutedEventArgs e)
        {
            try
            {
                var d = DepartmentCombo.SelectedItem as Department;
                var doc = DoctorCombo.SelectedItem as Doctor;
                var sh = ShiftCombo.SelectedItem as Shift;
                var c = CounterCombo.SelectedItem as Counter;
                decimal price;
                if (
                    !decimal.TryParse(
                        PriceBox.Text,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out price
                    )
                    || price < 0
                )
                    throw new InvalidOperationException("Enter a valid non-negative fee.");
                var t = QueueService.Issue(d, doc, sh, c, price, _user.Username);
                TicketPrinter.Print(t);
                StatusLabel.Text = "Printed ticket " + t.TicketNumber;
                FooterStatus.Text = "Last ticket: " + t.TicketNumber;
                RefreshGrids();
                UpdatePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ticket error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void RefreshGrids()
        {
            RecentGrid.ItemsSource = null;
            RecentGrid.ItemsSource = JsonStore
                .Data.Tickets.OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Time)
                .Take(12)
                .Select(x => new
                {
                    x.TicketNumber,
                    x.DepartmentName,
                    x.DoctorName,
                    x.ShiftName,
                    x.Time,
                    x.Price,
                    x.Status,
                })
                .ToList();
            HistoryGrid.ItemsSource = null;
            HistoryGrid.ItemsSource = JsonStore
                .Data.Tickets.OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Time)
                .ToList();
            UpdateSummary();
        }

        private void SummaryRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateSummary();
        }

        private DateTime GetPeriodStart(DateTime date, string period)
        {
            if (period == "Weekly")
            {
                int daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;

                return date.Date.AddDays(-daysSinceMonday);
            }

            if (period == "Monthly")
                return new DateTime(date.Year, date.Month, 1);

            return date.Date;
        }

        private string FormatPeriod(DateTime date, string period)
        {
            if (period == "Weekly")
                return date.ToString("yyyy-MM-dd") + " week";

            if (period == "Monthly")
                return date.ToString("yyyy-MM");

            return date.ToString("yyyy-MM-dd");
        }

        private class DailySummaryRow
        {
            public string Date { get; set; }
            public int TicketCount { get; set; }
            public decimal TotalPrice { get; set; }
        }

        private bool UpdateSummary()
        {
            if (
                SummaryGrid == null
                || SummaryDailyGrid == null
                || SummaryStartDate == null
                || SummaryEndDate == null
                || SummaryPeriodCombo == null
            )
            {
                return false;
            }

            if (!SummaryStartDate.SelectedDate.HasValue || !SummaryEndDate.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Please select both start and end dates.",
                    "Summary",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return false;
            }

            DateTime startDate = SummaryStartDate.SelectedDate.Value.Date;
            DateTime endDate = SummaryEndDate.SelectedDate.Value.Date;

            if (startDate > endDate)
            {
                MessageBox.Show(
                    "The start date cannot be later than the end date.",
                    "Invalid Date Range",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return false;
            }

            if (JsonStore.Data == null || JsonStore.Data.Tickets == null)
            {
                return false;
            }

            string selectedPeriod = SummaryPeriodCombo.SelectedItem is ComboBoxItem periodItem
                ? periodItem.Content?.ToString() ?? "Daily"
                : "Daily";

            var tickets = JsonStore
                .Data.Tickets.Where(t =>
                    t != null
                    && !string.Equals(t.Status, "VOID", StringComparison.OrdinalIgnoreCase)
                )
                .Select(t =>
                {
                    DateTime ticketDate;

                    bool validDate = DateTime.TryParseExact(
                        t.Date,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out ticketDate
                    );

                    return new
                    {
                        Ticket = t,
                        TicketDate = ticketDate,
                        ValidDate = validDate,
                    };
                })
                .Where(x =>
                    x.ValidDate && x.TicketDate.Date >= startDate && x.TicketDate.Date <= endDate
                )
                .ToList();

            // Helper to display currency consistently.
            Func<decimal, string> money = amount =>
                amount.ToString("N2", CultureInfo.CurrentCulture);

            // Overall totals.
            SummaryTicketCount.Text = tickets.Count.ToString();
            SummaryGrandTotal.Text = money(tickets.Sum(x => x.Ticket.Price));

            // Shift summaries.
            var morning = tickets
                .Where(x =>
                    string.Equals(
                        (x.Ticket.ShiftName ?? "").Trim(),
                        "Morning",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToList();

            var evening = tickets
                .Where(x =>
                    string.Equals(
                        (x.Ticket.ShiftName ?? "").Trim(),
                        "Evening",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToList();

            SummaryMorningTickets.Text = morning.Count.ToString();
            SummaryMorningTotal.Text = money(morning.Sum(x => x.Ticket.Price));

            SummaryEveningTickets.Text = evening.Count.ToString();
            SummaryEveningTotal.Text = money(evening.Sum(x => x.Ticket.Price));

            // Department summaries.
            var generalOpd = tickets
                .Where(x =>
                    (x.Ticket.DepartmentName ?? "").IndexOf(
                        "General",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                .ToList();

            var consultantsOpd = tickets
                .Where(x =>
                    (x.Ticket.DepartmentName ?? "").IndexOf(
                        "Consultant",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                .ToList();

            var otherDepartments = tickets
                .Where(x =>
                {
                    string department = x.Ticket.DepartmentName ?? "";

                    return department.IndexOf("General", StringComparison.OrdinalIgnoreCase) < 0
                        && department.IndexOf("Consultant", StringComparison.OrdinalIgnoreCase) < 0;
                })
                .ToList();

            SummaryGeneralOpdTickets.Text = generalOpd.Count.ToString();
            SummaryGeneralOpdTotal.Text = money(generalOpd.Sum(x => x.Ticket.Price));

            SummaryConsultantsOpdTickets.Text = consultantsOpd.Count.ToString();
            SummaryConsultantsOpdTotal.Text = money(consultantsOpd.Sum(x => x.Ticket.Price));

            SummaryOtherDepartmentTickets.Text = otherDepartments.Count.ToString();
            SummaryOtherDepartmentTotal.Text = money(otherDepartments.Sum(x => x.Ticket.Price));

            // Detailed report: grouped by the selected daily, weekly or monthly period.
            var rows = tickets
                .GroupBy(x => new
                {
                    PeriodStart = GetPeriodStart(x.TicketDate.Date, selectedPeriod),

                    Department = x.Ticket.DepartmentName ?? "",
                    Doctor = x.Ticket.DoctorName ?? "",
                    Shift = x.Ticket.ShiftName ?? "",
                })
                .Select(g => new SummaryRow
                {
                    Period = FormatPeriod(g.Key.PeriodStart, selectedPeriod),
                    Department = g.Key.Department,
                    Doctor = g.Key.Doctor,
                    Shift = g.Key.Shift,
                    TicketCount = g.Count(),
                    TotalPrice = g.Sum(x => x.Ticket.Price),
                })
                .OrderBy(r => r.Period)
                .ThenBy(r => r.Department)
                .ThenBy(r => r.Doctor)
                .ThenBy(r => r.Shift)
                .ToList();

            SummaryGrid.ItemsSource = rows;

            // Daily totals: one row for each date with tickets.
            var dailyRows = tickets
                .GroupBy(x => x.TicketDate.Date)
                .Select(g => new DailySummaryRow
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    TicketCount = g.Count(),
                    TotalPrice = g.Sum(x => x.Ticket.Price),
                })
                .OrderBy(r => r.Date)
                .ToList();

            SummaryDailyGrid.ItemsSource = dailyRows;

            return true;
        }

        private void SummaryExport_Click(object sender, RoutedEventArgs e)
        {
            if (!UpdateSummary())
                return;

            var rows = SummaryGrid.ItemsSource as System.Collections.Generic.List<SummaryRow>;

            var dailyRows =
                SummaryDailyGrid.ItemsSource as System.Collections.Generic.List<DailySummaryRow>;

            if (rows == null || dailyRows == null)
                return;

            var dialog = new SaveFileDialog
            {
                Title = "Export Summary Report",
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = "Clinic_Summary_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv",
                DefaultExt = ".csv",
                AddExtension = true,
            };

            if (dialog.ShowDialog() != true)
                return;

            var csv = new StringBuilder();

            csv.AppendLine("CLINIC SUMMARY REPORT");
            csv.AppendLine(
                "Start Date," + Csv(SummaryStartDate.SelectedDate.Value.ToString("yyyy-MM-dd"))
            );
            csv.AppendLine(
                "End Date," + Csv(SummaryEndDate.SelectedDate.Value.ToString("yyyy-MM-dd"))
            );
            csv.AppendLine();

            // Detailed summary.
            csv.AppendLine("DETAILED SUMMARY");
            csv.AppendLine("Period,Department,Doctor,Shift,Ticket Count,Total (Rs.)");

            foreach (var row in rows)
            {
                csv.AppendLine(
                    Csv(row.Period)
                        + ","
                        + Csv(row.Department)
                        + ","
                        + Csv(row.Doctor)
                        + ","
                        + Csv(row.Shift)
                        + ","
                        + row.TicketCount
                        + ","
                        + row.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture)
                );
            }

            csv.AppendLine(
                "GRAND TOTAL,,,,"
                    + rows.Sum(r => r.TicketCount)
                    + ","
                    + rows.Sum(r => r.TotalPrice).ToString("0.00", CultureInfo.InvariantCulture)
            );

            csv.AppendLine();

            // Daily breakdown.
            csv.AppendLine("DAILY TOTALS");
            csv.AppendLine("Date,Ticket Count,Total (Rs.)");

            foreach (var row in dailyRows)
            {
                csv.AppendLine(
                    Csv(row.Date)
                        + ","
                        + row.TicketCount
                        + ","
                        + row.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture)
                );
            }

            csv.AppendLine(
                "GRAND TOTAL,"
                    + dailyRows.Sum(r => r.TicketCount)
                    + ","
                    + dailyRows
                        .Sum(r => r.TotalPrice)
                        .ToString("0.00", CultureInfo.InvariantCulture)
            );

            // Write UTF-8 CSV.
            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));

            MessageBox.Show(
                "Summary report exported successfully.",
                "Export Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private Ticket SelectedTicket() => HistoryGrid.SelectedItem as Ticket;

        private void Refresh_Click(object s, RoutedEventArgs e) => RefreshGrids();

        private void Reprint_Click(object s, RoutedEventArgs e)
        {
            var t = SelectedTicket();
            if (t == null)
            {
                MessageBox.Show("Select a ticket first.");
                return;
            }
            try
            {
                QueueService.Reprint(t, _user.Username);
                TicketPrinter.Print(t);
                RefreshGrids();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Void_Click(object s, RoutedEventArgs e)
        {
            var t = SelectedTicket();
            if (t == null)
            {
                MessageBox.Show("Select a ticket first.");
                return;
            }
            if (
                MessageBox.Show("Void " + t.TicketNumber + "?", "Confirm", MessageBoxButton.YesNo)
                != MessageBoxResult.Yes
            )
                return;
            QueueService.Void(t, _user.Username);
            RefreshGrids();
        }

        private void Delete_Click(object s, RoutedEventArgs e)
        {
            var t = SelectedTicket();

            if (t == null)
            {
                MessageBox.Show("Select a ticket first.");
                return;
            }

            if (
                MessageBox.Show(
                    "Permanently delete ticket " + t.TicketNumber + "?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                ) != MessageBoxResult.Yes
            )
            {
                return;
            }

            QueueService.Delete(t, _user.Username);
            RefreshGrids();
        }

        private void Export_Click(object s, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "CSV file|*.csv",
                FileName = "clinic-tickets-" + DateTime.Now.ToString("yyyyMMdd") + ".csv",
            };
            if (dlg.ShowDialog() != true)
                return;
            var sb = new StringBuilder(
                "TicketNumber,Date,Time,Department,Doctor,Shift,Counter,Price,Status,CreatedBy\r\n"
            );
            foreach (var t in JsonStore.Data.Tickets)
                sb.AppendLine(
                    string.Join(
                        ",",
                        new[]
                        {
                            t.TicketNumber,
                            t.Date,
                            t.Time,
                            t.DepartmentName,
                            t.DoctorName,
                            t.ShiftName,
                            t.CounterName,
                            t.Price.ToString(CultureInfo.InvariantCulture),
                            t.Status,
                            t.CreatedBy,
                        }.Select(Csv)
                    )
                );
            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            MessageBox.Show("CSV exported.");
        }

        private string Csv(string x) => "\"" + (x ?? "").Replace("\"", "\"\"") + "\"";

        private void Admin_Click(object s, RoutedEventArgs e)
        {
            if (_user == null || _user.Role != "ADMIN")
            {
                MessageBox.Show("Administrator access required.");
                return;
            }
            var w = new AdminWindow(_user) { Owner = this };
            w.ShowDialog();
            LoadCombos();
            RefreshGrids();
            UpdatePreview();
        }

        private void Logout_Click(object s, RoutedEventArgs e)
        {
            _user = null;
            Login();
            if (_user == null)
                Close();
            else
            {
                UserLabel.Text = _user.FullName + " (" + _user.Role + ")";
                AdminButton.Visibility =
                    _user.Role == "ADMIN" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SaveSettings_Click(object s, RoutedEventArgs e)
        {
            if (!IsAdmin())
                return;
            JsonStore.Data.ClinicName = ClinicNameBox.Text.Trim();
            JsonStore.Data.TicketFooter = FooterBox.Text.Trim();
            JsonStore.Save();
            MessageBox.Show("Settings saved.");
        }

        private bool IsAdmin()
        {
            if (
                _user != null
                && string.Equals(_user.Role, "ADMIN", StringComparison.OrdinalIgnoreCase)
            )
                return true;
            MessageBox.Show("Administrator access required.");
            return false;
        }

        private void OpenDataFolder_Click(object s, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", JsonStore.Folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Backup_Click(object s, RoutedEventArgs e)
        {
            if (!IsAdmin())
                return;
            JsonStore.Save();
            var dlg = new SaveFileDialog
            {
                Filter = "JSON backup|*.json",
                FileName = "clinic-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json",
            };
            if (dlg.ShowDialog() == true)
            {
                File.Copy(JsonStore.FilePath, dlg.FileName, true);
                MessageBox.Show("Backup saved.");
            }
        }

        private void Doctor_Changed(object s, SelectionChangedEventArgs e)
        {
            var doctor = DoctorCombo.SelectedItem as Doctor;

            if (doctor != null)
            {
                // A selected doctor's fee overrides the department fee.
                PriceBox.Text = doctor.DefaultPrice.ToString("0.00", CultureInfo.InvariantCulture);
            }
            else
            {
                // If no doctor is selected, use the department fee.
                var department = DepartmentCombo.SelectedItem as Department;

                PriceBox.Text =
                    department == null
                        ? "0.00"
                        : department.DefaultPrice.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        private void ManageDepartments_Click(object s, RoutedEventArgs e) => Admin_Click(s, e);

        private void ManageDoctors_Click(object s, RoutedEventArgs e) => Admin_Click(s, e);

        private void ManageShifts_Click(object s, RoutedEventArgs e) => Admin_Click(s, e);

        private void ManageCounters_Click(object s, RoutedEventArgs e) => Admin_Click(s, e);

        private void ManagePrinters_Click(object s, RoutedEventArgs e) => Admin_Click(s, e);

        private void ManageUsers_Click(object s, RoutedEventArgs e) => Admin_Click(s, e);
    }
}
