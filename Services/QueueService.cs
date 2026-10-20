using System;
using System.Data.SQLite;
using ClinicQueueSystem.Data;

namespace ClinicQueueSystem.Services
{
    public sealed class IssuedTicket
    {
        public long Id; public string Number; public int Sequence; public string Department; public string Doctor; public string Shift; public string Counter; public decimal Price; public DateTime PrintedAt;
    }

    public static class QueueService
    {
        public static int GetCurrentShiftId()
        {
            var t = DateTime.Now.TimeOfDay;
            using (var c = Database.Open())
            using (var cmd = new SQLiteCommand("SELECT id,start_time,end_time FROM shifts WHERE active=1 ORDER BY display_order,id", c))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    var s = TimeSpan.Parse(r["start_time"].ToString()); var e = TimeSpan.Parse(r["end_time"].ToString());
                    if (s <= e ? t >= s && t < e : (t >= s || t < e)) return Convert.ToInt32(r["id"]);
                }
            }
            return 0;
        }

        public static IssuedTicket Issue(int departmentId, int doctorId, int shiftId, int counterId, int userId, decimal price)
        {
            using (var c = Database.Open())
            using (var tx = c.BeginTransaction())
            {
                var auto = Database.Setting("auto_shift", "1") == "1";
                if (auto) { var current = GetCurrentShiftId(); if (current > 0) shiftId = current; }
                if (shiftId <= 0) throw new Exception("No active shift is configured for the current time.");
                var deptCode = Convert.ToString(Scalar(c, tx, "SELECT code FROM departments WHERE id=@id AND active=1", new SQLiteParameter("@id", departmentId)));
                if (string.IsNullOrWhiteSpace(deptCode)) throw new Exception("Department is inactive or missing.");
                var deptName = Convert.ToString(Scalar(c, tx, "SELECT name FROM departments WHERE id=@id", new SQLiteParameter("@id", departmentId)));
                var doctor = doctorId > 0 ? Convert.ToString(Scalar(c, tx, "SELECT name FROM doctors WHERE id=@id", new SQLiteParameter("@id", doctorId))) : "";
                var shift = Convert.ToString(Scalar(c, tx, "SELECT name FROM shifts WHERE id=@id", new SQLiteParameter("@id", shiftId)));
                var counter = counterId > 0 ? Convert.ToString(Scalar(c, tx, "SELECT name FROM counters WHERE id=@id", new SQLiteParameter("@id", counterId))) : "";
                var date = DateTime.Now.ToString("yyyy-MM-dd"); var now = DateTime.Now.ToString("s");
                var next = Convert.ToInt32(Scalar(c, tx, "SELECT COALESCE(last_number,0)+1 FROM sequence_counters WHERE sequence_date=@d AND shift_id=@s AND department_id=@dep", new SQLiteParameter("@d", date), new SQLiteParameter("@s", shiftId), new SQLiteParameter("@dep", departmentId)) ?? 1);
                if (next > 999) throw new Exception("Sequence limit 999 reached for this department and shift.");
                Database.Execute(c, tx, "INSERT INTO sequence_counters(sequence_date,shift_id,department_id,last_number,updated_at) VALUES(@d,@s,@dep,@n,@u) ON CONFLICT(sequence_date,shift_id,department_id) DO UPDATE SET last_number=excluded.last_number,updated_at=excluded.updated_at", new SQLiteParameter("@d", date), new SQLiteParameter("@s", shiftId), new SQLiteParameter("@dep", departmentId), new SQLiteParameter("@n", next), new SQLiteParameter("@u", now));
                var ticketNo = deptCode + next.ToString("000");
                using (var cmd = new SQLiteCommand("INSERT INTO tickets(ticket_number,sequence_number,ticket_date,ticket_time,department_id,doctor_id,shift_id,counter_id,user_id,price,status,print_count,created_at) VALUES(@n,@seq,@d,@t,@dep,@doc,@sh,@co,@u,@p,'PRINTED',1,@at); SELECT last_insert_rowid();", c, tx))
                {
                    cmd.Parameters.AddWithValue("@n", ticketNo); cmd.Parameters.AddWithValue("@seq", next); cmd.Parameters.AddWithValue("@d", date); cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("HH:mm:ss")); cmd.Parameters.AddWithValue("@dep", departmentId); cmd.Parameters.AddWithValue("@doc", doctorId > 0 ? (object)doctorId : DBNull.Value); cmd.Parameters.AddWithValue("@sh", shiftId); cmd.Parameters.AddWithValue("@co", counterId > 0 ? (object)counterId : DBNull.Value); cmd.Parameters.AddWithValue("@u", userId); cmd.Parameters.AddWithValue("@p", price); var id = Convert.ToInt64(cmd.ExecuteScalar());
                    Database.Execute(c, tx, "INSERT INTO audit_logs(user_id,action,entity_type,entity_id,description,created_at) VALUES(@u,'PRINT_TICKET','TICKET',@id,@d,@at)", new SQLiteParameter("@u", userId), new SQLiteParameter("@id", id), new SQLiteParameter("@d", ticketNo + " issued"), new SQLiteParameter("@at", now));
                    tx.Commit(); return new IssuedTicket { Id = id, Number = ticketNo, Sequence = next, Department = deptName, Doctor = doctor, Shift = shift, Counter = counter, Price = price, PrintedAt = DateTime.Now };
                }
            }
        }

        public static void Reprint(long ticketId, int userId)
        { using (var c = Database.Open()) using (var tx = c.BeginTransaction()) { Database.Execute(c, tx, "UPDATE tickets SET print_count=print_count+1,status='REPRINTED' WHERE id=@id", new SQLiteParameter("@id", ticketId)); Database.Execute(c, tx, "INSERT INTO audit_logs(user_id,action,entity_type,entity_id,description,created_at) VALUES(@u,'REPRINT_TICKET','TICKET',@id,'Ticket reprinted',@at)", new SQLiteParameter("@u", userId), new SQLiteParameter("@id", ticketId), new SQLiteParameter("@at", DateTime.Now.ToString("s"))); tx.Commit(); } }
        public static void Void(long ticketId, int userId, string reason)
        { using (var c = Database.Open()) using (var tx = c.BeginTransaction()) { Database.Execute(c, tx, "UPDATE tickets SET status='VOID',void_reason=@r WHERE id=@id AND status<>'VOID'", new SQLiteParameter("@r", reason), new SQLiteParameter("@id", ticketId)); Database.Execute(c, tx, "INSERT INTO audit_logs(user_id,action,entity_type,entity_id,description,created_at) VALUES(@u,'VOID_TICKET','TICKET',@id,@d,@at)", new SQLiteParameter("@u", userId), new SQLiteParameter("@id", ticketId), new SQLiteParameter("@d", "Void: " + reason), new SQLiteParameter("@at", DateTime.Now.ToString("s"))); tx.Commit(); } }
        private static object Scalar(SQLiteConnection c, SQLiteTransaction tx, string sql, params SQLiteParameter[] p) { using (var cmd = new SQLiteCommand(sql, c, tx)) { if (p != null) cmd.Parameters.AddRange(p); return cmd.ExecuteScalar(); } }
    }
}
