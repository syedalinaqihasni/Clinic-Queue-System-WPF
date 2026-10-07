using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using ClinicQueueSystem.Models;

namespace ClinicQueueSystem.Services
{
    public static class JsonStore
    {
        private static readonly object Gate = new object();
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer
        {
            MaxJsonLength = 20 * 1024 * 1024,
            RecursionLimit = 100,
        };
        public static string Folder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClinicQueueSystem"
            );
        public static string FilePath => Path.Combine(Folder, "clinic-data.json");
        public static AppData Data { get; set; }

        public static void Initialize()
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Folder);
                if (File.Exists(FilePath))
                {
                    try
                    {
                        Data =
                            Serializer.Deserialize<AppData>(
                                File.ReadAllText(FilePath, Encoding.UTF8)
                            ) ?? new AppData();
                    }
                    catch (Exception ex)
                    {
                        string backup =
                            FilePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                        File.Copy(FilePath, backup);
                        throw new InvalidOperationException(
                            "Clinic data JSON could not be read. A copy was saved to: " + backup,
                            ex
                        );
                    }
                }
                else
                {
                    Data = Seed();
                    Save();
                }
                Normalize();
                Save();
            }
        }

        private static void Normalize()
        {
            if (Data.Users == null)
                Data.Users = new System.Collections.Generic.List<User>();
            if (Data.Departments == null)
                Data.Departments = new System.Collections.Generic.List<Department>();
            if (Data.Doctors == null)
                Data.Doctors = new System.Collections.Generic.List<Doctor>();
            if (Data.Shifts == null)
                Data.Shifts = new System.Collections.Generic.List<Shift>();
            if (Data.Counters == null)
                Data.Counters = new System.Collections.Generic.List<Counter>();
            if (Data.Printers == null)
                Data.Printers = new System.Collections.Generic.List<PrinterConfig>();
            if (Data.Tickets == null)
                Data.Tickets = new System.Collections.Generic.List<Ticket>();
            if (Data.Audit == null)
                Data.Audit = new System.Collections.Generic.List<AuditEntry>();
            if (Data.Sequences == null)
                Data.Sequences = new System.Collections.Generic.Dictionary<string, int>();
        }

        public static void Save()
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Folder);
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, Serializer.Serialize(Data), new UTF8Encoding(false));
                if (File.Exists(FilePath))
                {
                    string old = FilePath + ".bak";
                    try
                    {
                        File.Replace(temp, FilePath, old, true);
                    }
                    catch
                    {
                        File.Copy(temp, FilePath, true);
                        File.Delete(temp);
                    }
                }
                else
                    File.Move(temp, FilePath);
            }
        }

        private static AppData Seed()
        {
            var d = new AppData();
            d.Users.Add(
                new User
                {
                    Username = "admin",
                    PasswordHash = PasswordService.Hash("admin123"),
                    FullName = "Clinic Administrator",
                    Role = "ADMIN",
                }
            );
            d.Departments.Add(
                new Department
                {
                    Code = "G",
                    Name = "General OPD",
                    DefaultPrice = 500,
                }
            );
            d.Departments.Add(
                new Department
                {
                    Code = "C",
                    Name = "Consultant OPD",
                    DefaultPrice = 1500,
                }
            );
            d.Departments.Add(
                new Department
                {
                    Code = "E",
                    Name = "ENT OPD",
                    DefaultPrice = 800,
                }
            );
            d.Shifts.Add(
                new Shift
                {
                    Name = "Morning",
                    StartTime = "08:00",
                    EndTime = "14:00",
                }
            );
            d.Shifts.Add(
                new Shift
                {
                    Name = "Evening",
                    StartTime = "14:00",
                    EndTime = "20:00",
                }
            );
            d.Counters.Add(new Counter { Name = "Counter 01", PrinterName = "" });
            d.Printers.Add(
                new PrinterConfig
                {
                    Name = "Default thermal printer",
                    WindowsPrinterName = "",
                    PaperWidthMm = 80,
                }
            );
            return d;
        }
    }
}
