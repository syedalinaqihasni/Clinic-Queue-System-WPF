using System;
using System.Windows.Forms;
using ClinicQueueSystem.Data;
namespace ClinicQueueSystem { static class Program { [STAThread] static void Main(){ Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Database.Initialize(); Application.Run(new Forms.LoginForm()); } } }
