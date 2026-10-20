using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using ClinicQueueSystem.Data;
using ClinicQueueSystem.Services;

namespace ClinicQueueSystem.Forms
{
    public class LoginForm : Form
    {
        TextBox user = new TextBox(), pass = new TextBox();
        public LoginForm()
        {
            Text="Clinic Queue System - Login"; Width=420; Height=250; StartPosition=FormStartPosition.CenterScreen;
            var title=new Label{Text="CLINIC QUEUE SYSTEM",Dock=DockStyle.Top,Height=45,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI",15,FontStyle.Bold)};
            var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(35),ColumnCount=2,RowCount=3}; panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            panel.Controls.Add(new Label{Text="Username",AutoSize=true},0,0); panel.Controls.Add(user,1,0); panel.Controls.Add(new Label{Text="Password",AutoSize=true},0,1); panel.Controls.Add(pass,1,1); pass.UseSystemPasswordChar=true;
            var b=new Button{Text="LOGIN",Dock=DockStyle.Fill}; b.Click+=Login; panel.Controls.Add(b,1,2); AcceptButton=b; Controls.Add(panel); Controls.Add(title);
        }
        void Login(object s,EventArgs e)
        {
            using(var c=Database.Connection()) using(var cmd=c.CreateCommand())
            { cmd.CommandText="SELECT id,full_name,role,password_hash FROM users WHERE username=@u AND active=1"; cmd.Parameters.AddWithValue("@u",user.Text.Trim()); using(var r=cmd.ExecuteReader()){
                if(!r.Read()){MessageBox.Show("Invalid username or password.");return;} if(!PasswordHasher.Verify(pass.Text,r[3].ToString())){MessageBox.Show("Invalid username or password.");return;}
                int id=Convert.ToInt32(r[0]); string name=r[1].ToString(), role=r[2].ToString(); Hide(); using(var f=new MainForm(id,name,role)){f.ShowDialog();} Close(); }
            }
        }
    }
}
