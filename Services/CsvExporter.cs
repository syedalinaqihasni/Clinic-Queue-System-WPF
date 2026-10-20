using System; using System.Data.SQLite; using System.IO; using System.Text; using ClinicQueueSystem.Data;
namespace ClinicQueueSystem.Services
{
 public static class CsvExporter
 {
  public static string ExportTickets(DateTime from, DateTime to, string path)
  {
   using(var c=Database.Open()) using(var cmd=new SQLiteCommand(@"SELECT t.ticket_number,t.ticket_date,t.ticket_time,d.name,COALESCE(doc.name,''),s.name,t.price,t.status,t.print_count FROM tickets t JOIN departments d ON d.id=t.department_id LEFT JOIN doctors doc ON doc.id=t.doctor_id JOIN shifts s ON s.id=t.shift_id WHERE t.ticket_date BETWEEN @f AND @to ORDER BY t.id",c))
   { cmd.Parameters.AddWithValue("@f",from.ToString("yyyy-MM-dd")); cmd.Parameters.AddWithValue("@to",to.ToString("yyyy-MM-dd")); using(var r=cmd.ExecuteReader()) using(var w=new StreamWriter(path,false,Encoding.UTF8)){ w.WriteLine("Ticket,Date,Time,Department,Doctor,Shift,Price,Status,PrintCount"); while(r.Read()){ w.WriteLine(string.Join(",", Q(r[0]),Q(r[1]),Q(r[2]),Q(r[3]),Q(r[4]),Q(r[5]),Q(r[6]),Q(r[7]),Q(r[8]))); } } }
   return path;
  }
  static string Q(object v){var s=Convert.ToString(v)??""; return "\""+s.Replace("\"","\"\"")+"\"";}
 }
}
