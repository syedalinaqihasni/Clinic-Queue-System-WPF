using System.Windows;
using ClinicQueueSystem.Services;

namespace ClinicQueueSystem
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            JsonStore.Initialize();
        }
    }
}
