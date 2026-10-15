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
            RefreshGrids();
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
            if (_user.Role == "ADMIN")
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
