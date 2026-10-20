using System;
using System.Drawing.Printing;
using System.Text;

namespace ClinicQueueSystem.Services
{
    public static class EscPosPrinter
    {
        public static void Print(string printerName, QueueResult q, string clinicName, string footer)
        {
            if (string.IsNullOrWhiteSpace(printerName)) throw new Exception("No Windows printer is configured.");
            // This starter implementation uses the Windows spooler through PrintDocument.
            // For a true raw ESC/POS implementation, replace this with a Win32 RAW spooler call.
            var pd = new PrintDocument { PrinterSettings = { PrinterName = printerName } };
            if (!pd.PrinterSettings.IsValid) throw new Exception("Printer is not available: " + printerName);
            pd.PrintPage += (s,e) =>
            {
                using (var f1 = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold))
                using (var f2 = new System.Drawing.Font("Arial", 8))
                using (var f3 = new System.Drawing.Font("Arial", 22, System.Drawing.FontStyle.Bold))
                {
                    float y=5; float w=e.PageBounds.Width;
                    e.Graphics.DrawString(clinicName, f1, System.Drawing.Brushes.Black, Center(e.Graphics, clinicName, f1, w), y); y+=18;
                    e.Graphics.DrawString("QUEUE TICKET", f2, System.Drawing.Brushes.Black, Center(e.Graphics,"QUEUE TICKET",f2,w),y); y+=25;
                    e.Graphics.DrawString(q.TicketNumber, f3, System.Drawing.Brushes.Black, Center(e.Graphics,q.TicketNumber,f3,w),y); y+=42;
                    e.Graphics.DrawString(q.Department, f1, System.Drawing.Brushes.Black, Center(e.Graphics,q.Department,f1,w),y); y+=22;
                    if(!string.IsNullOrWhiteSpace(q.Doctor)){ e.Graphics.DrawString(q.Doctor,f2,System.Drawing.Brushes.Black,Center(e.Graphics,q.Doctor,f2,w),y); y+=20; }
                    e.Graphics.DrawString("Date: "+q.DateTime.ToString("dd-MMM-yyyy"),f2,System.Drawing.Brushes.Black,5,y); y+=17;
                    e.Graphics.DrawString("Time: "+q.DateTime.ToString("hh:mm tt"),f2,System.Drawing.Brushes.Black,5,y); y+=17;
                    e.Graphics.DrawString("Shift: "+q.Shift,f2,System.Drawing.Brushes.Black,5,y); y+=17;
                    e.Graphics.DrawString("Counter: "+q.Counter,f2,System.Drawing.Brushes.Black,5,y); y+=17;
                    e.Graphics.DrawString("Fee: Rs. "+q.Price.ToString("0.##"),f2,System.Drawing.Brushes.Black,5,y); y+=25;
                    e.Graphics.DrawString(footer,f2,System.Drawing.Brushes.Black,Center(e.Graphics,footer,f2,w),y);
                }
            };
            pd.Print();
        }
        private static float Center(System.Drawing.Graphics g,string text,System.Drawing.Font f,float width){ return Math.Max(0,(width-g.MeasureString(text,f).Width)/2); }
    }
}
