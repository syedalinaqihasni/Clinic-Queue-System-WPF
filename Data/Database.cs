using System;
using System.Data.SQLite;
using System.IO;

namespace ClinicQueueSystem.Data
{
    public static class Database
    {
        public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClinicQueueSystem");
        public static readonly string DbFile = Path.Combine(Folder, "clinic_queue.db");
        public static string ConnectionString => "Data Source=" + DbFile + ";Version=3;foreign keys=true;busy_timeout=5000;";

        public static void Initialize()
        {
            Directory.CreateDirectory(Folder);
            if (!File.Exists(DbFile)) SQLiteConnection.CreateFile(DbFile);
            using (var c = Open())
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS users(id INTEGER PRIMARY KEY AUTOINCREMENT,username TEXT NOT NULL UNIQUE,password_hash TEXT NOT NULL,full_name TEXT NOT NULL,role TEXT NOT NULL DEFAULT 'RECEPTIONIST',active INTEGER NOT NULL DEFAULT 1,created_at TEXT NOT NULL,updated_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS departments(id INTEGER PRIMARY KEY AUTOINCREMENT,code TEXT NOT NULL UNIQUE,name TEXT NOT NULL,default_price REAL NOT NULL DEFAULT 0,description TEXT,display_order INTEGER NOT NULL DEFAULT 0,active INTEGER NOT NULL DEFAULT 1,created_at TEXT NOT NULL,created_by INTEGER,updated_at TEXT NOT NULL,updated_by INTEGER);
CREATE TABLE IF NOT EXISTS doctors(id INTEGER PRIMARY KEY AUTOINCREMENT,name TEXT NOT NULL,room_number TEXT,specialization TEXT,default_price REAL NOT NULL DEFAULT 0,active INTEGER NOT NULL DEFAULT 1,created_at TEXT NOT NULL,updated_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS doctor_departments(id INTEGER PRIMARY KEY AUTOINCREMENT,doctor_id INTEGER NOT NULL,department_id INTEGER NOT NULL,active INTEGER NOT NULL DEFAULT 1,UNIQUE(doctor_id,department_id),FOREIGN KEY(doctor_id) REFERENCES doctors(id),FOREIGN KEY(department_id) REFERENCES departments(id));
CREATE TABLE IF NOT EXISTS shifts(id INTEGER PRIMARY KEY AUTOINCREMENT,name TEXT NOT NULL UNIQUE,start_time TEXT NOT NULL,end_time TEXT NOT NULL,active INTEGER NOT NULL DEFAULT 1,display_order INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS printers(id INTEGER PRIMARY KEY AUTOINCREMENT,name TEXT NOT NULL,system_printer_name TEXT NOT NULL,connection_type TEXT NOT NULL DEFAULT 'WINDOWS',paper_width INTEGER NOT NULL DEFAULT 80,ip_address TEXT,port INTEGER NOT NULL DEFAULT 9100,active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS counters(id INTEGER PRIMARY KEY AUTOINCREMENT,counter_number TEXT NOT NULL UNIQUE,name TEXT NOT NULL,location TEXT,printer_id INTEGER,active INTEGER NOT NULL DEFAULT 1,FOREIGN KEY(printer_id) REFERENCES printers(id));
CREATE TABLE IF NOT EXISTS sequence_counters(id INTEGER PRIMARY KEY AUTOINCREMENT,sequence_date TEXT NOT NULL,shift_id INTEGER NOT NULL,department_id INTEGER NOT NULL,last_number INTEGER NOT NULL DEFAULT 0,updated_at TEXT NOT NULL,UNIQUE(sequence_date,shift_id,department_id));
CREATE TABLE IF NOT EXISTS tickets(id INTEGER PRIMARY KEY AUTOINCREMENT,ticket_number TEXT NOT NULL,sequence_number INTEGER NOT NULL,ticket_date TEXT NOT NULL,ticket_time TEXT NOT NULL,department_id INTEGER NOT NULL,doctor_id INTEGER,shift_id INTEGER NOT NULL,counter_id INTEGER,user_id INTEGER NOT NULL,price REAL NOT NULL,status TEXT NOT NULL DEFAULT 'PRINTED',print_count INTEGER NOT NULL DEFAULT 1,created_at TEXT NOT NULL,void_reason TEXT,FOREIGN KEY(department_id) REFERENCES departments(id),FOREIGN KEY(doctor_id) REFERENCES doctors(id),FOREIGN KEY(shift_id) REFERENCES shifts(id),FOREIGN KEY(counter_id) REFERENCES counters(id),FOREIGN KEY(user_id) REFERENCES users(id));
CREATE TABLE IF NOT EXISTS price_history(id INTEGER PRIMARY KEY AUTOINCREMENT,department_id INTEGER NOT NULL,old_price REAL NOT NULL,new_price REAL NOT NULL,changed_by INTEGER,changed_at TEXT NOT NULL,FOREIGN KEY(department_id) REFERENCES departments(id));
CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY,value TEXT);
CREATE TABLE IF NOT EXISTS audit_logs(id INTEGER PRIMARY KEY AUTOINCREMENT,user_id INTEGER,action TEXT NOT NULL,entity_type TEXT,entity_id INTEGER,description TEXT,created_at TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS idx_tickets_date ON tickets(ticket_date);
CREATE INDEX IF NOT EXISTS idx_tickets_dept ON tickets(ticket_date,department_id);
CREATE INDEX IF NOT EXISTS idx_audit_date ON audit_logs(created_at);
";
                cmd.ExecuteNonQuery();
            }
            Seed();
        }

        public static SQLiteConnection Open()
        {
            var c = new SQLiteConnection(ConnectionString);
            c.Open();
            return c;
        }

        private static void Seed()
        {
            using (var c = Open())
            using (var tx = c.BeginTransaction())
            {
                var now = DateTime.Now.ToString("s");
                Execute(c, tx, "INSERT OR IGNORE INTO settings(key,value) VALUES('clinic_name','ABC Clinic')");
                Execute(c, tx, "INSERT OR IGNORE INTO settings(key,value) VALUES('ticket_footer','Please wait for your turn')");
                Execute(c, tx, "INSERT OR IGNORE INTO settings(key,value) VALUES('currency','Rs')");
                Execute(c, tx, "INSERT OR IGNORE INTO settings(key,value) VALUES('auto_shift','1')");
                Execute(c, tx, "INSERT OR IGNORE INTO users(username,password_hash,full_name,role,active,created_at,updated_at) VALUES('admin','" + Services.PasswordHasher.Hash("admin123") + "','Administrator','ADMIN',1,'" + now + "','" + now + "')");
                Execute(c, tx, "INSERT OR IGNORE INTO departments(code,name,default_price,description,display_order,active,created_at,updated_at) VALUES('G','General OPD',500,'General outpatient department',1,1,'" + now + "','" + now + "')");
                Execute(c, tx, "INSERT OR IGNORE INTO departments(code,name,default_price,description,display_order,active,created_at,updated_at) VALUES('C','Consultant OPD',1500,'Consultant outpatient department',2,1,'" + now + "','" + now + "')");
                Execute(c, tx, "INSERT OR IGNORE INTO departments(code,name,default_price,description,display_order,active,created_at,updated_at) VALUES('E','ENT OPD',800,'ENT outpatient department',3,1,'" + now + "','" + now + "')");
                Execute(c, tx, "INSERT OR IGNORE INTO shifts(name,start_time,end_time,active,display_order) VALUES('Morning','08:00','14:00',1,1)");
                Execute(c, tx, "INSERT OR IGNORE INTO shifts(name,start_time,end_time,active,display_order) VALUES('Evening','14:00','20:00',1,2)");
                Execute(c, tx, "INSERT OR IGNORE INTO counters(counter_number,name,location,active) VALUES('01','Main Reception','Reception',1)");
                tx.Commit();
            }
        }

        public static void Execute(SQLiteConnection c, SQLiteTransaction tx, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, c, tx)) cmd.ExecuteNonQuery();
        }

        public static object Scalar(SQLiteConnection c, string sql, params SQLiteParameter[] p)
        {
            using (var cmd = new SQLiteCommand(sql, c)) { if (p != null) cmd.Parameters.AddRange(p); return cmd.ExecuteScalar(); }
        }

        public static string Setting(string key, string fallback = "")
        {
            using (var c = Open()) { var v = Scalar(c, "SELECT value FROM settings WHERE key=@k", new SQLiteParameter("@k", key)); return v == null || v == DBNull.Value ? fallback : Convert.ToString(v); }
        }
    }
}
