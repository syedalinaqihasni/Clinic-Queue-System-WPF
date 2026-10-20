using System;
using System.Data.SQLite;
using ClinicQueueSystem.Data;

namespace ClinicQueueSystem.Services
{
    public class QueueResult
    {
        public long TicketId; public string TicketNumber; public int SequenceNumber; public DateTime DateTime;
        public string Department; public string DepartmentCode; public string Doctor; public string Shift; public string Counter; public decimal Price;
    }

    public class QueueService
    {
        public QueueResult IssueTicket(int departmentId, int shiftId, int? doctorId, int? counterId, int userId, decimal price)
        {
            using (var c = Database.Connection())
            using (var tx = c.BeginTransaction())
            {
                DateTime now = DateTime.Now;
                string date = now.ToString("yyyy-MM-dd");
                string code = Convert.ToString(Database.Scalar(c, "SELECT code FROM departments WHERE id=@id AND active=1", new SQLiteParameter("@id", departmentId)));
                if (string.IsNullOrWhiteSpace(code)) throw new Exception("Department is not active.");
                int number;
                object existing = Database.Scalar(c, "SELECT last_number FROM sequence_counters WHERE sequence_date=@d AND shift_id=@s AND department_id=@dep", new SQLiteParameter("@d", date), new SQLiteParameter("@s", shiftId), new SQLiteParameter("@dep", departmentId));
                if (existing == null || existing == DBNull.Value)
                {
                    number = 1;
                    Database.ExecuteNonQuery(c, "INSERT INTO sequence_counters(sequence_date,shift_id,department_id,last_number,updated_at) VALUES(@d,@s,@dep,1,@u)",
                        new SQLiteParameter("@d", date), new SQLiteParameter("@s", shiftId), new SQLiteParameter("@dep", departmentId), new SQLiteParameter("@u", now.ToString("s")));
                }
                else
                {
                    number = Convert.ToInt32(existing) + 1;
                    if (number > 999) throw new Exception("Sequence reached 999 for this department/shift/day.");
                    Database.ExecuteNonQuery(c, "UPDATE sequence_counters SET last_number=@n,updated_at=@u WHERE sequence_date=@d AND shift_id=@s AND department_id=@dep",
                        new SQLiteParameter("@n", number), new SQLiteParameter("@u", now.ToString("s")), new SQLiteParameter("@d", date), new SQLiteParameter("@s", shiftId), new SQLiteParameter("@dep", departmentId));
                }
                string ticket = code + number.ToString("000");
                object doctor = doctorId.HasValue ? Database.Scalar(c, "SELECT name FROM doctors WHERE id=@id", new SQLiteParameter("@id", doctorId.Value)) : null;
                string shift = Convert.ToString(Database.Scalar(c, "SELECT name FROM shifts WHERE id=@id", new SQLiteParameter("@id", shiftId)));
                string counter = counterId.HasValue ? Convert.ToString(Database.Scalar(c, "SELECT counter_number FROM counters WHERE id=@id", new SQLiteParameter("@id", counterId.Value))) : "";
                string dept = Convert.ToString(Database.Scalar(c, "SELECT name FROM departments WHERE id=@id", new SQLiteParameter("@id", departmentId)));
                Database.ExecuteNonQuery(c, @"INSERT INTO tickets(ticket_number,sequence_number,ticket_date,ticket_time,department_id,doctor_id,shift_id,counter_id,user_id,price,status,print_count,created_at)
VALUES(@t,@n,@d,@time,@dep,@doc,@shift,@counter,@user,@price,'PRINTED',1,@created)",
                    new SQLiteParameter("@t", ticket), new SQLiteParameter("@n", number), new SQLiteParameter("@d", date), new SQLiteParameter("@time", now.ToString("HH:mm:ss")),
                    new SQLiteParameter("@dep", departmentId), new SQLiteParameter("@doc", (object)doctorId ?? DBNull.Value), new SQLiteParameter("@shift", shiftId),
                    new SQLiteParameter("@counter", (object)counterId ?? DBNull.Value), new SQLiteParameter("@user", userId), new SQLiteParameter("@price", price), new SQLiteParameter("@created", now.ToString("s")));
                long id = c.LastInsertRowId;
                Database.ExecuteNonQuery(c, "INSERT INTO audit_logs(user_id,action,entity_type,entity_id,description,created_at) VALUES(@u,'PRINT_TICKET','TICKET',@id,@d,@t)",
                    new SQLiteParameter("@u", userId), new SQLiteParameter("@id", id), new SQLiteParameter("@d", ticket), new SQLiteParameter("@t", now.ToString("s")));
                tx.Commit();
                return new QueueResult { TicketId=id, TicketNumber=ticket, SequenceNumber=number, DateTime=now, Department=dept, DepartmentCode=code, Doctor=doctor == null ? "" : doctor.ToString(), Shift=shift, Counter=counter, Price=price };
            }
        }

        public void Reprint(long ticketId, int userId)
        {
            using (var c = Database.Connection())
            {
                Database.ExecuteNonQuery(c, "UPDATE tickets SET print_count=print_count+1,status='REPRINTED' WHERE id=@id", new SQLiteParameter("@id", ticketId));
                Database.ExecuteNonQuery(c, "INSERT INTO audit_logs(user_id,action,entity_type,entity_id,description,created_at) VALUES(@u,'REPRINT_TICKET','TICKET',@id,'Reprinted ticket',@t)", new SQLiteParameter("@u", userId), new SQLiteParameter("@id", ticketId), new SQLiteParameter("@t", DateTime.Now.ToString("s")));
            }
        }

        public void Void(long ticketId, int userId)
        {
            using (var c = Database.Connection())
            {
                Database.ExecuteNonQuery(c, "UPDATE tickets SET status='VOID' WHERE id=@id AND status <> 'VOID'", new SQLiteParameter("@id", ticketId));
                Database.ExecuteNonQuery(c, "INSERT INTO audit_logs(user_id,action,entity_type,entity_id,description,created_at) VALUES(@u,'VOID_TICKET','TICKET',@id,'Voided ticket',@t)", new SQLiteParameter("@u", userId), new SQLiteParameter("@id", ticketId), new SQLiteParameter("@t", DateTime.Now.ToString("s")));
            }
        }
    }
}
