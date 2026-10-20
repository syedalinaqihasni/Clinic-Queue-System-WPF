using System; using System.Data.SQLite; using System.Drawing; using System.Windows.Forms; using ClinicQueueSystem.Data; using ClinicQueueSystem.Services;
namespace ClinicQueueSystem.Forms
{
 public class LoginForm:Form
 {
  TextBox user,pass; Button login; public int UserId; public string UserRole,FullName;
  public LoginForm(){ Text="Clinic Queue System - Login"; Width=390; Height=250; StartPosition=FormStartPosition.CenterScreen; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
   var p=new Panel{Dock=DockStyle.Fill,Padding=new Padding(28)}; Controls.Add(p); var title=new Label{Text=Database.Setting("clinic_name","Clinic"),Font=new Font("Segoe UI",16,FontStyle.Bold),Dock=DockStyle.Top,Height=40,TextAlign=ContentAlignment.MiddleCenter}; p.Controls.Add(title);
   p.Controls.Add(new Label{Text="Username",Top=58,Left=10,Width=90}); user=new TextBox{Top=55,Left=105,Width=210}; p.Controls.Add(user); p.Controls.Add(new Label{Text="Password",Top=98,Left=10,Width=90}); pass=new TextBox{Top=95,Left=105,Width=210,UseSystemPasswordChar=true}; p.Controls.Add(pass); login=new Button{Text="LOGIN",Top=140,Left=105,Width=210,Height=34}; login.Click+=DoLogin; p.Controls.Add(login); AcceptButton=login; Shown+=(s,e)=>user.Focus(); }
  void DoLogin(object s,EventArgs e){ using(var c=Database.Open()) using(var cmd=new SQLiteCommand("SELECT id,password_hash,role,full_name FROM users WHERE username=@u AND active=1",c)){cmd.Parameters.AddWithValue("@u",user.Text.Trim()); using(var r=cmd.ExecuteReader()){if(r.Read()&&PasswordHasher.Verify(pass.Text,r["password_hash"].ToString())){UserId=Convert.ToInt32(r["id"]);UserRole=r["role"].ToString();FullName=r["full_name"].ToString();Hide(); using(var f=new MainForm(UserId,UserRole,FullName))f.ShowDialog(); Show(); pass.Clear(); return;}}} MessageBox.Show("Invalid username or password.","Login",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
 }
}
