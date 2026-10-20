using System;
using System.Windows.Forms;
using ClinicQueueSystem.Data;
using ClinicQueueSystem.Forms;

namespace ClinicQueueSystem
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Database.Initialize();
            Application.Run(new LoginForm());
        }
    }
}
