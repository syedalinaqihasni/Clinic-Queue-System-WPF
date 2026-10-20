using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using ClinicQueueSystem.Services;

namespace ClinicQueueSystem.Data
{
    public static class Database
    {
        public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClinicQueueSystem");
        public static readonly string File = Path.Combine(Folder, "clinic_queue.db");
        public static readonly string ConnectionString = "Data Source=" + File + ";Version=3;foreign keys=true;busy_timeout=5000;";

        public static SQLiteConnection Connection()
        {
            var c = new SQLiteConnection(ConnectionString);
            c.Open();
            return c;
        }

        public static void Initialize()
        {
            Directory.CreateDirectory(Folder);
            using (var c = Connection())
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS users(
 id INTEGER PRIMARY KEY AUTOINCREMENT, username TEXT NOT NULL UNIQUE, password_hash TEXT NOT NULL,
 full_name TEXT NOT NULL, role TEXT NOT NULL DEFAULT 'RECEPTIONIST', active INTEGER NOT NULL DEFAULT 1,
 created_at TEXT NOT NULL, updated_at TEXT);
CREATE TABLE IF NOT EXISTS departments(
 id INTEGER PRIMARY KEY AUTOINCREMENT, code TEXT NOT NULL UNIQUE, name TEXT NOT NULL,
 default_price REAL NOT NULL DEFAULT 0, description TEXT, display_order INTEGER NOT NULL DEFAULT 0,
 active INTEGER NOT NULL DEFAULT 1, created_at TEXT NOT NULL, created_by INTEGER, updated_at TEXT, updated_by INTEGER,
 FOREIGN KEY(created_by) REFERENCES users(id), FOREIGN KEY(updated_by) REFERENCES users(id));
CREATE TABLE IF NOT EXISTS doctors(
 id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, room_number TEXT, default_price REAL,
 specialization TEXT, active INTEGER NOT NULL DEFAULT 1, created_at TEXT NOT NULL, updated_at TEXT);
CREATE TABLE IF NOT EXISTS doctor_departments(
 id INTEGER PRIMARY KEY AUTOINCREMENT, doctor_id INTEGER NOT NULL, department_id INTEGER NOT NULL, active INTEGER NOT NULL DEFAULT 1,
 UNIQUE(doctor_id,department_id), FOREIGN KEY(doctor_id) REFERENCES doctors(id), FOREIGN KEY(department_id) REFERENCES departments(id));
CREATE TABLE IF NOT EXISTS shifts(
 id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, start_time TEXT NOT NULL, end_time TEXT NOT NULL,
 active INTEGER NOT NULL DEFAULT 1, display_order INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS counters(
 id INTEGER PRIMARY KEY AUTOINCREMENT, counter_number TEXT NOT NULL UNIQUE, name TEXT NOT NULL,
 location TEXT, printer_id INTEGER, active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS printers(
 id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, system_printer_name TEXT, connection_type TEXT DEFAULT 'USB',
 paper_width INTEGER DEFAULT 58, ip_address TEXT, port INTEGER, active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS sequence_counters(
 id INTEGER PRIMARY KEY AUTOINCREMENT, sequence_date TEXT NOT NULL, shift_id INTEGER NOT NULL,
 department_id INTEGER NOT NULL, last_number INTEGER NOT NULL DEFAULT 0, updated_at TEXT NOT NULL,
 UNIQUE(sequence_date,shift_id,department_id));
CREATE TABLE IF NOT EXISTS tickets(
 id INTEGER PRIMARY KEY AUTOINCREMENT, ticket_number TEXT NOT NULL, sequence_number INTEGER NOT NULL,
 ticket_date TEXT NOT NULL, ticket_time TEXT NOT NULL, department_id INTEGER NOT NULL, doctor_id INTEGER,
 shift_id INTEGER NOT NULL, counter_id INTEGER, user_id INTEGER, price REAL NOT NULL DEFAULT 0,
 status TEXT NOT NULL DEFAULT 'PRINTED', print_count INTEGER NOT NULL DEFAULT 1, created_at TEXT NOT NULL,
 FOREIGN KEY(department_id) REFERENCES departments(id), FOREIGN KEY(doctor_id) REFERENCES doctors(id),
 FOREIGN KEY(shift_id) REFERENCES shifts(id), FOREIGN KEY(counter_id) REFERENCES counters(id), FOREIGN KEY(user_id) REFERENCES users(id));
CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY,value TEXT);
CREATE TABLE IF NOT EXISTS audit_logs(
 id INTEGER PRIMARY KEY AUTOINCREMENT, user_id INTEGER, action TEXT NOT NULL, entity_type TEXT,
 entity_id INTEGER, description TEXT, created_at TEXT NOT NULL,
 FOREIGN KEY(user_id) REFERENCES users(id));
CREATE INDEX IF NOT EXISTS idx_tickets_date ON tickets(ticket_date);
CREATE INDEX IF NOT EXISTS idx_tickets_number ON tickets(ticket_number);
CREATE INDEX IF NOT EXISTS idx_audit_created ON audit_logs(created_at);
";
                cmd.ExecuteNonQuery();
            }
            Seed();
        }

        private static void Seed()
        {
            using (var c = Connection())
            {
                Execute(c, "SELECT COUNT(*) FROM users", null, r =>
                {
                    if (Convert.ToInt32(r) == 0)
                        ExecuteNonQuery(c, "INSERT INTO users(username,password_hash,full_name,role,created_at) VALUES('admin',@p,'Administrator','ADMIN',@d)",
                            new SQLiteParameter("@p", PasswordHasher.Hash("admin123")), new SQLiteParameter("@d", DateTime.Now.ToString("s")));
                });
                if (Scalar(c, "SELECT COUNT(*) FROM departments") == 0)
                {
                    ExecuteNonQuery(c, "INSERT INTO departments(code,name,default_price,display_order,created_at) VALUES('G','General OPD',500,1,@d)", new SQLiteParameter("@d", DateTime.Now.ToString("s")));
                    ExecuteNonQuery(c, "INSERT INTO departments(code,name,default_price,display_order,created_at) VALUES('C','Consultant OPD',1500,2,@d)", new SQLiteParameter("@d", DateTime.Now.ToString("s")));
                    ExecuteNonQuery(c, "INSERT INTO departments(code,name,default_price,display_order,created_at) VALUES('E','ENT OPD',800,3,@d)", new SQLiteParameter("@d", DateTime.Now.ToString("s")));
                }
                if (Scalar(c, "SELECT COUNT(*) FROM shifts") == 0)
                {
                    ExecuteNonQuery(c, "INSERT INTO shifts(name,start_time,end_time,display_order) VALUES('Morning','08:00','14:00',1)");
                    ExecuteNonQuery(c, "INSERT INTO shifts(name,start_time,end_time,display_order) VALUES('Evening','14:00','20:00',2)");
                }
                if (Scalar(c, "SELECT COUNT(*) FROM counters") == 0)
                    ExecuteNonQuery(c, "INSERT INTO counters(counter_number,name,location) VALUES('01','Main Reception','Reception')");
                ExecuteNonQuery(c, "INSERT OR IGNORE INTO settings(key,value) VALUES('clinic_name','ABC Clinic')");
                ExecuteNonQuery(c, "INSERT OR IGNORE INTO settings(key,value) VALUES('ticket_footer','Please wait for your turn')");
                ExecuteNonQuery(c, "INSERT OR IGNORE INTO settings(key,value) VALUES('timezone','Asia/Karachi')");
            }
        }

        public static object Scalar(SQLiteConnection c, string sql, params SQLiteParameter[] p)
        {
            using (var cmd = c.CreateCommand()) { cmd.CommandText = sql; if (p != null) cmd.Parameters.AddRange(p); return cmd.ExecuteScalar(); }
        }
        private static void Execute(SQLiteConnection c, string sql, SQLiteParameter[] p, Action<object> action)
        { action(Scalar(c, sql, p)); }
        public static void ExecuteNonQuery(SQLiteConnection c, string sql, params SQLiteParameter[] p)
        { using (var cmd = c.CreateCommand()) { cmd.CommandText = sql; if (p != null) cmd.Parameters.AddRange(p); cmd.ExecuteNonQuery(); } }
        public static DataTable Query(string sql, params SQLiteParameter[] p)
        {
            using (var c = Connection()) using (var cmd = c.CreateCommand()) using (var da = new SQLiteDataAdapter())
            { cmd.CommandText = sql; if (p != null) cmd.Parameters.AddRange(p); da.SelectCommand = cmd; var t = new DataTable(); da.Fill(t); return t; }
        }
    }
}
