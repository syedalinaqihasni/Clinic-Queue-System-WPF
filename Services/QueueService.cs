using System;
using System.Collections.Generic;
using System.Globalization;
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
            {
                throw new InvalidOperationException("Select a department, shift and counter.");
            }

            if (price < 0)
            {
                throw new InvalidOperationException("Ticket price cannot be negative.");
            }

            string date = DateTime.Now.ToString("yyyy-MM-dd");

            string key = BuildSequenceKey(date, shift.Name, dept.Code);

            // Find the smallest unused ticket number.
            int n = GetNextSequence(dept, shift);

            if (n > 999)
            {
                throw new InvalidOperationException(
                    "This department has reached ticket 999 for this shift today."
                );
            }

            DateTime now = DateTime.Now;

            var t = new Ticket
            {
                TicketNumber = dept.Code + n.ToString("000"),
                Sequence = n,
                Date = date,
                Time = now.ToString("HH:mm:ss"),
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

            // Keep the stored sequence synchronized with the issued ticket.
            JsonStore.Data.Sequences[key] = n;

            Log(user, "ISSUE_TICKET", t.TicketNumber + " / " + dept.Name);

            JsonStore.Save();

            return t;
        }

        public static int GetNextSequence(Department dept, Shift shift)
        {
            if (dept == null || shift == null)
            {
                return 1;
            }

            string date = DateTime.Now.ToString("yyyy-MM-dd");

            // Collect numbers already used for this date, shift and department.
            var usedNumbers = new HashSet<int>(
                JsonStore
                    .Data.Tickets.Where(t =>
                        t.Date == date
                        && t.ShiftName == shift.Name
                        && t.DepartmentCode == dept.Code
                        && !string.IsNullOrWhiteSpace(t.TicketNumber)
                        && t.TicketNumber.StartsWith(dept.Code, StringComparison.Ordinal)
                    )
                    .Select(t =>
                    {
                        string suffix = t.TicketNumber.Substring(dept.Code.Length);

                        int number;

                        return int.TryParse(
                            suffix,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out number
                        )
                            ? number
                            : 0;
                    })
                    .Where(number => number >= 1 && number <= 999)
            );

            // Reuse the first available number.
            for (int number = 1; number <= 999; number++)
            {
                if (!usedNumbers.Contains(number))
                {
                    return number;
                }
            }

            // All numbers from 001 to 999 are already in use.
            return 1000;
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
            {
                return;
            }

            t.PrintCount++;
            t.Status = "REPRINTED";

            Log(user, "REPRINT_TICKET", t.TicketNumber);

            JsonStore.Save();
        }

        public static void Void(Ticket t, string user)
        {
            if (t == null)
            {
                return;
            }

            t.Status = "VOID";

            Log(user, "VOID_TICKET", t.TicketNumber);

            JsonStore.Save();
        }

        public static void Delete(Ticket t, string user)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t), "Ticket cannot be null.");
            }

            var ticket = JsonStore.Data.Tickets.FirstOrDefault(x =>
                x.TicketNumber == t.TicketNumber
                && x.Date == t.Date
                && x.ShiftName == t.ShiftName
                && x.DepartmentCode == t.DepartmentCode
            );

            if (ticket == null)
            {
                throw new InvalidOperationException("Ticket not found.");
            }

            string ticketNumber = ticket.TicketNumber;
            string date = ticket.Date;
            string shiftName = ticket.ShiftName;
            string departmentCode = ticket.DepartmentCode;

            // Permanently remove the ticket.
            JsonStore.Data.Tickets.Remove(ticket);

            // Recalculate the saved counter from remaining tickets.
            string key = BuildSequenceKey(date, shiftName, departmentCode);

            int highestRemainingSequence = JsonStore
                .Data.Tickets.Where(x =>
                    x.Date == date && x.ShiftName == shiftName && x.DepartmentCode == departmentCode
                )
                .Select(x => x.Sequence)
                .DefaultIfEmpty(0)
                .Max();

            JsonStore.Data.Sequences[key] = highestRemainingSequence;

            Log(user, "DELETE_TICKET", ticketNumber);

            JsonStore.Save();
        }

        private static string BuildSequenceKey(string date, string shiftName, string departmentCode)
        {
            return date + "|" + shiftName + "|" + departmentCode;
        }
    }
}
