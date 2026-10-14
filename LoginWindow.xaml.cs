using System;
using System.Linq;
using System.Windows;
using ClinicQueueSystem.Models;
using ClinicQueueSystem.Services;
namespace ClinicQueueSystem
{
    public partial class LoginWindow : Window
    {
        public User AuthenticatedUser { get; private set; }
        public LoginWindow() { InitializeComponent(); UsernameBox.Text = "admin"; Loaded += (s, e) => PasswordBox.Focus(); }
        private void SignIn_Click(object sender, RoutedEventArgs e) { var u = JsonStore.Data.Users.FirstOrDefault(x => x.Active && string.Equals(x.Username, UsernameBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)); if (u != null && PasswordService.Verify(PasswordBox.Password, u.PasswordHash)) { AuthenticatedUser = u; DialogResult = true; Close(); } else { ErrorText.Text = "The username or password is incorrect, or the account is inactive."; PasswordBox.Clear(); PasswordBox.Focus(); } }
    }
}
