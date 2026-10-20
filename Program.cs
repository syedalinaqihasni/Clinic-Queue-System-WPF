using System;
using System.Windows.Forms;
using ClinicQueueSystem.Data;

namespace ClinicQueueSystem
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (sender, args) =>
                MessageBox.Show(args.Exception.Message, "Clinic Queue System", MessageBoxButtons.OK, MessageBoxIcon.Error);

            try
            {
                Database.Initialize();
                Application.Run(new Forms.LoginForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The application could not start.\r\n\r\n" + ex.Message +
                    "\r\n\r\nCheck that .NET Framework 4.8 and the application files are installed correctly.",
                    "Clinic Queue System - Startup Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
