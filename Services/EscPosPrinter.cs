using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using ClinicQueueSystem.Data;

namespace ClinicQueueSystem.Services
{
    public static class EscPosPrinter
    {
        [DllImport("winspool.drv", EntryPoint="OpenPrinterA", SetLastError=true, CharSet=CharSet.Ansi)] static extern bool OpenPrinter(string p, out IntPtr h, IntPtr defaults);
        [DllImport("winspool.drv", EntryPoint="ClosePrinter", SetLastError=true)] static extern bool ClosePrinter(IntPtr h);
        [DllImport("winspool.drv", EntryPoint="StartDocPrinterA", SetLastError=true, CharSet=CharSet.Ansi)] static extern int StartDocPrinter(IntPtr h, int level, IntPtr di);
        [DllImport("winspool.drv", EntryPoint="EndDocPrinter", SetLastError=true)] static extern bool EndDocPrinter(IntPtr h);
        [DllImport("winspool.drv", EntryPoint="StartPagePrinter", SetLastError=true)] static extern bool StartPagePrinter(IntPtr h);
        [DllImport("winspool.drv", EntryPoint="EndPagePrinter", SetLastError=true)] static extern bool EndPagePrinter(IntPtr h);
        [DllImport("winspool.drv", EntryPoint="WritePrinter", SetLastError=true)] static extern bool WritePrinter(IntPtr h, IntPtr p, int count, out int written);
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)] struct DOCINFO { public string pDocName; public string pOutputFile; public string pDataType; }
        [DllImport("winspool.drv", EntryPoint="StartDocPrinterA", SetLastError=true, CharSet=CharSet.Ansi)] static extern int StartDocPrinter(IntPtr h, int level, ref DOCINFO di);

        public static void Print(IssuedTicket t, string printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName)) throw new Exception("No printer is assigned to this counter.");
            var b = new List<byte>();
            b.AddRange(new byte[] { 0x1B,0x40, 0x1B,0x61,0x01, 0x1B,0x45,0x01 });
            Add(b, Database.Setting("clinic_name", "Clinic")); Add(b, "\n");
            b.AddRange(new byte[] { 0x1B,0x45,0x00 }); Add(b, "QUEUE TICKET\n");
            b.AddRange(new byte[] { 0x1B,0x45,0x01,0x1D,0x21,0x22 }); Add(b, t.Number + "\n");
            b.AddRange(new byte[] { 0x1D,0x21,0x00,0x1B,0x45,0x00 });
            Add(b, t.Department + "\n"); if (!string.IsNullOrWhiteSpace(t.Doctor)) Add(b, "Doctor: " + t.Doctor + "\n");
            Add(b, "Date: " + t.PrintedAt.ToString("dd-MM-yyyy") + "\n"); Add(b, "Time: " + t.PrintedAt.ToString("hh:mm:ss tt") + "\n"); Add(b, "Shift: " + t.Shift + "\n"); if (!string.IsNullOrWhiteSpace(t.Counter)) Add(b, "Counter: " + t.Counter + "\n");
            Add(b, "Fee: " + Database.Setting("currency", "Rs") + " " + t.Price.ToString("0.00") + "\n\n"); Add(b, "--------------------------------\n"); Add(b, Database.Setting("ticket_footer", "Please wait for your turn") + "\n\n\n");
            b.AddRange(new byte[] { 0x1D,0x56,0x00 }); Send(printerName, b.ToArray());
        }
        static void Add(List<byte> b, string s) { b.AddRange(Encoding.ASCII.GetBytes(s)); }
        static void Send(string printer, byte[] data)
        {
            IntPtr h; if (!OpenPrinter(printer, out h, IntPtr.Zero)) throw new Exception("Unable to open printer: " + printer);
            try { var di = new DOCINFO { pDocName="Clinic Queue Ticket", pDataType="RAW" }; if (StartDocPrinter(h,1,ref di)==0) throw new Exception("Unable to start printer job."); if (!StartPagePrinter(h)) throw new Exception("Unable to start printer page."); var ptr=Marshal.AllocHGlobal(data.Length); try { Marshal.Copy(data,0,ptr,data.Length); int written; if(!WritePrinter(h,ptr,data.Length,out written)||written!=data.Length) throw new Exception("Printer did not accept the complete ticket."); } finally { Marshal.FreeHGlobal(ptr); } EndPagePrinter(h); EndDocPrinter(h); } finally { ClosePrinter(h); }
        }
    }
}
