using System;
using System.Linq;
using ClinicQueueSystem.Models;

namespace ClinicQueueSystem.Services
{
    public static class QueueService
    {
        public static Ticket Issue(
            Department dept,
            Doctor doctor,
            Shift shift,
            Counter counter,
            decimal price,
            string user
        )
        {
            if (dept == null || shift == null || counter == null)
                throw new InvalidOperationException("Select a department, shift and counter.");
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string key = date + "|" + shift.Name + "|" + dept.Code;
            int n;
            if (!JsonStore.Data.Sequences.TryGetValue(key, out n))
                n = 0;
            n++;
            if (n > 999)
                throw new InvalidOperationException(
                    "This department has reached ticket 999 for this shift today."
                );
            JsonStore.Data.Sequences[key] = n;
            var t = new Ticket
            {
                TicketNumber = dept.Code + n.ToString("000"),
                Sequence = n,
                Date = date,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                DepartmentCode = dept.Code,
                DepartmentName = dept.Name,
                DoctorName = doctor == null ? "" : doctor.Name,
                ShiftName = shift.Name,
                CounterName = counter.Name,
                Price = price,
                Status = "PRINTED",
                PrintCount = 1,
                CreatedBy = user,
            };
            JsonStore.Data.Tickets.Add(t);
            Log(user, "ISSUE_TICKET", t.TicketNumber + " / " + dept.Name);
            JsonStore.Save();
            return t;
        }

        public static void Log(string user, string action, string details)
        {
            JsonStore.Data.Audit.Add(
                new AuditEntry
                {
                    At = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    User = user,
                    Action = action,
                    Details = details,
                }
            );
        }

        public static void Reprint(Ticket t, string user)
        {
            if (t == null)
                return;
            t.PrintCount++;
            t.Status = "REPRINTED";
            Log(user, "REPRINT_TICKET", t.TicketNumber);
            JsonStore.Save();
        }

        public static void Void(Ticket t, string user)
        {
            if (t == null)
                return;
            t.Status = "VOID";
            Log(user, "VOID_TICKET", t.TicketNumber);
            JsonStore.Save();
        }
    }
}
