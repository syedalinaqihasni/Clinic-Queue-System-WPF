using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using ClinicQueueSystem.Data;
using ClinicQueueSystem.Services;

namespace ClinicQueueSystem.Forms
{
    public class MainForm : Form
    {
        private readonly int userId;
        private readonly string userRole;
        private readonly string fullName;
        private ComboBox departmentBox, doctorBox, shiftBox, counterBox;
        private NumericUpDown feeBox;
        private Label nextTicketLabel, clockLabel, statusLabel;
        private Button printButton, adminButton;
        private Timer clockTimer;
        private bool loadingData;

        public MainForm(int id, string role, string name)
        {
            userId = id;
            userRole = role ?? "RECEPTIONIST";
            fullName = name ?? "User";
            Text = Database.Setting("clinic_name", "Clinic") + " | Reception Queue Counter";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(960, 650);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.FromArgb(245, 247, 250);
            BuildWindow();
            LoadData();
        }

        private void BuildWindow()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = Color.FromArgb(22, 55, 91), Padding = new Padding(24, 12, 24, 12) };
            Controls.Add(header);
            var clinicTitle = new Label { Text = Database.Setting("clinic_name", "Clinic Queue System"), Dock = DockStyle.Left, Width = 390, ForeColor = Color.White, Font = new Font("Segoe UI", 20F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            header.Controls.Add(clinicTitle);
            clockLabel = new Label { Dock = DockStyle.Right, Width = 240, ForeColor = Color.White, Font = new Font("Segoe UI", 11F), TextAlign = ContentAlignment.MiddleRight };
            header.Controls.Add(clockLabel);

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White, Padding = new Padding(20, 8, 20, 8) };
            Controls.Add(toolbar);
            var userLabel = new Label { Text = "Signed in: " + fullName + "  |  " + userRole, Dock = DockStyle.Left, Width = 420, ForeColor = Color.FromArgb(60, 70, 82), TextAlign = ContentAlignment.MiddleLeft };
            toolbar.Controls.Add(userLabel);
            var logoutButton = new Button { Text = "Log out", Dock = DockStyle.Right, Width = 105, Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(238, 242, 247) };
            logoutButton.Click += (s, e) => Close();
            toolbar.Controls.Add(logoutButton);
            adminButton = new Button { Text = "Administration", Dock = DockStyle.Right, Width = 145, Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(229, 238, 249) };
            adminButton.Visible = userRole == "ADMIN" || userRole == "SUPERVISOR";
            adminButton.Click += (s, e) => { using (var form = new AdminForm(userId, userRole)) form.ShowDialog(this); LoadData(); };
            toolbar.Controls.Add(adminButton);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28), ColumnCount = 2, RowCount = 1, BackColor = BackColor };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            Controls.Add(body);
            body.BringToFront();

            var formCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28), Margin = new Padding(0, 0, 16, 0) };
            body.Controls.Add(formCard, 0, 0);
            var formLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(0) };
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            for (int i = 1; i <= 5; i++) formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16F));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            formCard.Controls.Add(formLayout);
            var formTitle = new Label { Text = "Issue a queue ticket", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 55, 91), TextAlign = ContentAlignment.MiddleLeft };
            formLayout.Controls.Add(formTitle, 0, 0); formLayout.SetColumnSpan(formTitle, 2);

            departmentBox = CreateCombo(); doctorBox = CreateCombo(); shiftBox = CreateCombo(); counterBox = CreateCombo();
            AddField(formLayout, "Department", departmentBox, 1);
            AddField(formLayout, "Doctor", doctorBox, 2);
            AddField(formLayout, "Shift", shiftBox, 3);
            AddField(formLayout, "Reception counter", counterBox, 4);
            feeBox = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, ThousandsSeparator = true, Font = new Font("Segoe UI", 13F), Margin = new Padding(4, 8, 4, 8) };
            AddField(formLayout, "Fee (Rs.)", feeBox, 5);
            printButton = new Button { Text = "PRINT TICKET", Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 115, 77), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 16F, FontStyle.Bold), Margin = new Padding(4, 12, 4, 4) };
            printButton.FlatAppearance.BorderSize = 0;
            printButton.Click += PrintTicket;
            formLayout.Controls.Add(printButton, 0, 6); formLayout.SetColumnSpan(printButton, 2);

            var side = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = BackColor, Margin = new Padding(16, 0, 0, 0) };
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 56F)); side.RowStyles.Add(new RowStyle(SizeType.Percent, 22F)); side.RowStyles.Add(new RowStyle(SizeType.Percent, 22F));
            body.Controls.Add(side, 1, 0);
            var nextCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 55, 91), Padding = new Padding(18), Margin = new Padding(0, 0, 0, 16) };
            side.Controls.Add(nextCard, 0, 0);
            var nextCaption = new Label { Text = "NEXT TICKET", Dock = DockStyle.Top, Height = 45, ForeColor = Color.FromArgb(208, 222, 238), Font = new Font("Segoe UI", 12F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            nextCard.Controls.Add(nextCaption);
            nextTicketLabel = new Label { Text = "---", Dock = DockStyle.Fill, ForeColor = Color.White, Font = new Font("Segoe UI", 36F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            nextCard.Controls.Add(nextTicketLabel); nextTicketLabel.BringToFront();
            var infoCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18), Margin = new Padding(0, 0, 0, 16) };
            side.Controls.Add(infoCard, 0, 1);
            infoCard.Controls.Add(new Label { Text = "NUMBERING RULE", Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(22, 55, 91) });
            infoCard.Controls.Add(new Label { Text = "Each department has its own sequence. The sequence restarts for a new shift and a new day.", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(75, 85, 99), TextAlign = ContentAlignment.MiddleLeft });
            var statusCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18) };
            side.Controls.Add(statusCard, 0, 2);
            statusLabel = new Label { Text = "Ready to issue tickets.", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(28, 115, 77), Font = new Font("Segoe UI", 11F), TextAlign = ContentAlignment.MiddleLeft };
            statusCard.Controls.Add(statusLabel);

            departmentBox.SelectedIndexChanged += DepartmentChanged;
            shiftBox.SelectedIndexChanged += (s, e) => UpdateNextTicket();
            clockTimer = new Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) => { clockLabel.Text = DateTime.Now.ToString("ddd, dd MMM yyyy\r\nhh:mm:ss tt"); if (Database.Setting("auto_shift", "1") == "1") AutoSelectShift(); };
            clockTimer.Start();
            FormClosed += (s, e) => { if (clockTimer != null) clockTimer.Dispose(); };
        }

        private ComboBox CreateCombo() { return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 13F), Dock = DockStyle.Fill, Margin = new Padding(4, 8, 4, 8) }; }
        private void AddField(TableLayoutPanel layout, string caption, Control control, int row)
        {
            layout.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(55, 65, 81), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(4, 8, 4, 8) }, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private void LoadData()
        {
            loadingData = true;
            Fill(departmentBox, "SELECT id,code||' - '||name name FROM departments WHERE active=1 ORDER BY display_order,id");
            Fill(shiftBox, "SELECT id,name FROM shifts WHERE active=1 ORDER BY display_order,id");
            Fill(counterBox, "SELECT id,counter_number||' - '||name name FROM counters WHERE active=1 ORDER BY counter_number");
            loadingData = false;
            LoadDoctors(); SetFeeFromDepartment(); AutoSelectShift(); UpdateNextTicket();
        }
        private void Fill(ComboBox box, string sql)
        {
            var table = new DataTable(); using (var connection = Database.Open()) using (var adapter = new SQLiteDataAdapter(sql, connection)) adapter.Fill(table);
            box.DataSource = table; box.DisplayMember = "name"; box.ValueMember = "id";
        }
        private void DepartmentChanged(object sender, EventArgs e)
        {
            if (loadingData) return;
            LoadDoctors(); SetFeeFromDepartment(); UpdateNextTicket();
        }
        private void LoadDoctors()
        {
            if (departmentBox == null || departmentBox.SelectedValue == null || !(departmentBox.SelectedValue is int)) return;
            var table = new DataTable();
            using (var connection = Database.Open()) using (var adapter = new SQLiteDataAdapter("SELECT d.id,d.name FROM doctors d JOIN doctor_departments dd ON dd.doctor_id=d.id AND dd.active=1 WHERE d.active=1 AND dd.department_id=@department ORDER BY d.name", connection))
            { adapter.SelectCommand.Parameters.AddWithValue("@department", Convert.ToInt32(departmentBox.SelectedValue)); adapter.Fill(table); }
            var row = table.NewRow(); row["id"] = 0; row["name"] = "No specific doctor"; table.Rows.InsertAt(row, 0);
            doctorBox.DataSource = table; doctorBox.DisplayMember = "name"; doctorBox.ValueMember = "id";
        }
        private void SetFeeFromDepartment()
        {
            if (departmentBox == null || departmentBox.SelectedValue == null || !(departmentBox.SelectedValue is int)) return;
            using (var connection = Database.Open())
            {
                var value = Database.Scalar(connection, "SELECT default_price FROM departments WHERE id=@id", new SQLiteParameter("@id", Convert.ToInt32(departmentBox.SelectedValue)));
                if (value != null && value != DBNull.Value) feeBox.Value = Math.Min(feeBox.Maximum, Math.Max(feeBox.Minimum, Convert.ToDecimal(value)));
            }
        }
        private void AutoSelectShift()
        {
            if (shiftBox == null || shiftBox.DataSource == null) return;
            int shiftId = QueueService.GetCurrentShiftId();
            if (shiftId > 0 && shiftBox.SelectedValue != null && Convert.ToInt32(shiftBox.SelectedValue) != shiftId) shiftBox.SelectedValue = shiftId;
        }
        private void UpdateNextTicket()
        {
            if (departmentBox == null || departmentBox.SelectedValue == null || shiftBox == null || shiftBox.SelectedValue == null || !(departmentBox.SelectedValue is int) || !(shiftBox.SelectedValue is int)) return;
            int departmentId = Convert.ToInt32(departmentBox.SelectedValue), shiftId = Convert.ToInt32(shiftBox.SelectedValue);
            using (var connection = Database.Open())
            {
                string code;
                using (var command = new SQLiteCommand("SELECT code FROM departments WHERE id=@id", connection)) { command.Parameters.AddWithValue("@id", departmentId); code = Convert.ToString(command.ExecuteScalar()); }
                var value = Database.Scalar(connection, "SELECT COALESCE((SELECT last_number FROM sequence_counters WHERE sequence_date=@date AND shift_id=@shift AND department_id=@department),0)+1", new SQLiteParameter("@date", DateTime.Now.ToString("yyyy-MM-dd")), new SQLiteParameter("@shift", shiftId), new SQLiteParameter("@department", departmentId));
                nextTicketLabel.Text = code + Convert.ToInt32(value ?? 1).ToString("000");
            }
        }
        private void PrintTicket(object sender, EventArgs e)
        {
            if (departmentBox.SelectedValue == null || shiftBox.SelectedValue == null || counterBox.SelectedValue == null) { MessageBox.Show("Please configure at least one active department, shift, and counter.", "Setup required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                printButton.Enabled = false;
                int doctorId = doctorBox.SelectedValue == null ? 0 : Convert.ToInt32(doctorBox.SelectedValue);
                var ticket = QueueService.Issue(Convert.ToInt32(departmentBox.SelectedValue), doctorId, Convert.ToInt32(shiftBox.SelectedValue), Convert.ToInt32(counterBox.SelectedValue), userId, feeBox.Value);
                string printerName;
                using (var connection = Database.Open()) using (var command = new SQLiteCommand("SELECT p.system_printer_name FROM counters c LEFT JOIN printers p ON p.id=c.printer_id WHERE c.id=@id", connection))
                { command.Parameters.AddWithValue("@id", Convert.ToInt32(counterBox.SelectedValue)); printerName = Convert.ToString(command.ExecuteScalar()); }
                EscPosPrinter.Print(ticket, printerName);
                statusLabel.Text = "Ticket " + ticket.TicketNumber + " issued successfully.";
                UpdateNextTicket();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ticket printing failed", MessageBoxButtons.OK, MessageBoxIcon.Error); statusLabel.Text = "Printing failed. Check printer configuration."; }
            finally { printButton.Enabled = true; }
        }
    }
}
