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
        readonly int userId; readonly string userName, role; ComboBox dept=new ComboBox(), doctor=new ComboBox(), shift=new ComboBox(), counter=new ComboBox(); NumericUpDown price=new NumericUpDown(); Label next=new Label(), last=new Label(); QueueResult lastQueue;
        public MainForm(int id,string name,string r){userId=id;userName=name;role=r; Build(); LoadData();}
        void Build(){Text="Clinic Queue System";WindowState=FormWindowState.Maximized; var top=new Panel{Dock=DockStyle.Top,Height=55}; top.Controls.Add(new Label{Text="CLINIC QUEUE SYSTEM",Font=new Font("Segoe UI",16,FontStyle.Bold),Location=new Point(20,14),AutoSize=true}); var admin=new Button{Text="ADMIN",Location=new Point(250,12),Width=100}; admin.Click+=(s,e)=>{if(role=="ADMIN")using(var f=new AdminForm(userId,userName))f.ShowDialog();else MessageBox.Show("Administrator access required."); LoadData();}; top.Controls.Add(admin); var logout=new Button{Text="LOGOUT",Location=new Point(360,12),Width=100}; logout.Click+=(s,e)=>Close(); top.Controls.Add(logout); Controls.Add(top);
            var grid=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(40),ColumnCount=2,RowCount=7};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,180));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            Add(grid,"Department",dept,0);Add(grid,"Doctor",doctor,1);Add(grid,"Shift",shift,2);Add(grid,"Counter",counter,3);Add(grid,"Price",price,4);price.Maximum=1000000;price.DecimalPlaces=2;
            var print=new Button{Text="PRINT TICKET",Dock=DockStyle.Fill,Font=new Font("Segoe UI",18,FontStyle.Bold),Height=60};print.Click+=Print;grid.Controls.Add(print,1,5);
            next.Text="Next Ticket: -";next.Font=new Font("Segoe UI",14,FontStyle.Bold);grid.Controls.Add(next,1,6);Controls.Add(grid);
        }
        void Add(TableLayoutPanel g,string label,Control c,int row){g.Controls.Add(new Label{Text=label,AutoSize=true,Anchor=AnchorStyles.Left},0,row);c.Dock=DockStyle.Fill;g.Controls.Add(c,1,row);}
        void LoadData(){LoadCombo(dept,"SELECT id,name FROM departments WHERE active=1 ORDER BY display_order,name", "name","id");LoadCombo(shift,"SELECT id,name FROM shifts WHERE active=1 ORDER BY display_order,name","name","id");LoadCombo(counter,"SELECT id,counter_number || ' - ' || name AS name FROM counters WHERE active=1 ORDER BY counter_number","name","id"); if(dept.Items.Count>0)dept.SelectedIndex=0; if(shift.Items.Count>0)shift.SelectedIndex=0;if(counter.Items.Count>0)counter.SelectedIndex=0;dept.SelectedIndexChanged+=(s,e)=>LoadDoctors();LoadDoctors(); UpdateNext();}
        void LoadCombo(ComboBox box,string sql,string display,string value){box.Items.Clear();var t=Database.Query(sql);box.DisplayMember=display;box.ValueMember=value;box.DataSource=t;}
        void LoadDoctors(){if(dept.SelectedValue==null)return; int d=Convert.ToInt32(dept.SelectedValue);doctor.DataSource=Database.Query("SELECT d.id,d.name FROM doctors d JOIN doctor_departments dd ON dd.doctor_id=d.id WHERE dd.department_id="+d+" AND d.active=1 AND dd.active=1 ORDER BY d.name");doctor.DisplayMember="name";doctor.ValueMember="id";doctor.SelectedIndex=-1; var p=Database.Scalar(Database.Connection(),"SELECT default_price FROM departments WHERE id="+d);}
        void UpdateNext(){if(dept.SelectedValue==null||shift.SelectedValue==null)return;string date=DateTime.Now.ToString("yyyy-MM-dd");object n=Database.Scalar(Database.Connection(),"SELECT last_number FROM sequence_counters WHERE sequence_date=@d AND shift_id=@s AND department_id=@dep",new SQLiteParameter("@d",date),new SQLiteParameter("@s",shift.SelectedValue),new SQLiteParameter("@dep",dept.SelectedValue));int nextNo=n==null?1:Convert.ToInt32(n)+1;string code=Convert.ToString(Database.Scalar(Database.Connection(),"SELECT code FROM departments WHERE id="+dept.SelectedValue));next.Text="Next Ticket: "+code+nextNo.ToString("000");object pr=Database.Scalar(Database.Connection(),"SELECT default_price FROM departments WHERE id="+dept.SelectedValue);if(pr!=null)price.Value=Convert.ToDecimal(pr);}
        void Print(object s,EventArgs e){try{int d=Convert.ToInt32(dept.SelectedValue),sh=Convert.ToInt32(shift.SelectedValue),co=counter.SelectedValue==null?(int?)null:Convert.ToInt32(counter.SelectedValue),dr=doctor.SelectedValue==null?(int?)null:Convert.ToInt32(doctor.SelectedValue);var q=new QueueService().IssueTicket(d,sh,dr,co,userId,price.Value);string printer=GetPrinter(co);string clinic=GetSetting("clinic_name","ABC Clinic"),footer=GetSetting("ticket_footer","Please wait for your turn");EscPosPrinter.Print(printer,q,clinic,footer);lastQueue=q;last.Text="Last Ticket: "+q.TicketNumber;next.Text="Printed: "+q.TicketNumber; }catch(Exception ex){MessageBox.Show(ex.Message,"Print Error",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
        string GetPrinter(int? co){if(!co.HasValue)return "";return Convert.ToString(Database.Scalar(Database.Connection(),"SELECT p.system_printer_name FROM printers p JOIN counters c ON c.printer_id=p.id WHERE c.id="+co.Value));}
        string GetSetting(string k,string fallback){object v=Database.Scalar(Database.Connection(),"SELECT value FROM settings WHERE key=@k",new SQLiteParameter("@k",k));return v==null?fallback:v.ToString();}
    }
}
