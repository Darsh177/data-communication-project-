using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class HotelServer
{
    private const string DataFile = "hotel_schedule.txt";
    static Dictionary<string, bool[,]> roomSchedules = new Dictionary<string, bool[,]>();
    static object fileLock = new object();

    static void Main()
    {
        LoadData();
        TcpListener server = new TcpListener(IPAddress.Any, 8000);
        server.Start();
        Console.WriteLine("Hotel Server Active... Waiting for Choices.");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            new Thread(() => HandleClient(client)).Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            while (true)
            {
                try
                {
                    // استقبال الطلب من الكلينت (VIEW أو BOOK)
                    string request = Receive(stream);
                    if (request == "EXIT" || string.IsNullOrEmpty(request)) break;

                    if (request == "VIEW")
                    {
                        // إرسال حالة الغرف فقط
                        string status = GetStatusReport();
                        Send(stream, status);
                    }
                    else if (request.StartsWith("BOOK:"))
                    {
                        // معالجة عملية الحجز
                        string result = ProcessBooking(request);
                        Send(stream, result);
                    }
                }
                catch { break; }
            }
        }
    }

    static string GetStatusReport()
    {
        string report = "\n--- Current Room Availability (7 Days x 3 Periods) ---\n";
        foreach (var room in roomSchedules)
        {
            report += $"\nRoom {room.Key}:\n";
            for (int d = 0; d < 7; d++)
            {
                report += $" Day {d + 1}: ";
                for (int p = 0; p < 3; p++)
                    report += room.Value[d, p] ? "[X] " : "[A] ";
                report += "\n";
            }
        }
        return report;
    }

    static string ProcessBooking(string request)
    {
        try
        {
            string[] p = request.Split(':');
            string rNo = p[1];
            int sD = int.Parse(p[2]) - 1, sP = int.Parse(p[3]) - 1;
            int eD = int.Parse(p[4]) - 1, eP = int.Parse(p[5]) - 1;

            if (sD < 0 || eD > 6 || sP < 0 || eP > 2 || (sD > eD) || (sD == eD && sP > eP))
                return "FAILED: Invalid time range.";

            lock (roomSchedules)
            {
                if (CheckAvailability(rNo, sD, sP, eD, eP))
                {
                    BookPeriod(rNo, sD, sP, eD, eP);
                    SaveData();
                    return "SUCCESS: Reservation saved.";
                }
                return "FAILED: Room is already booked for these periods.";
            }
        }
        catch { return "ERROR: Data format error."; }
    }

    // --- نفس دوال الـ CheckAvailability و BookPeriod و SaveData من الكود السابق ---
    static bool CheckAvailability(string rNo, int sD, int sP, int eD, int eP)
    {
        if (!roomSchedules.ContainsKey(rNo)) return false;
        for (int d = sD; d <= eD; d++)
        {
            int startP = (d == sD) ? sP : 0;
            int endP = (d == eD) ? eP : 2;
            for (int p = startP; p <= endP; p++) if (roomSchedules[rNo][d, p]) return false;
        }
        return true;
    }

    static void BookPeriod(string rNo, int sD, int sP, int eD, int eP)
    {
        for (int d = sD; d <= eD; d++)
        {
            int startP = (d == sD) ? sP : 0;
            int endP = (d == eD) ? eP : 2;
            for (int p = startP; p <= endP; p++) roomSchedules[rNo][d, p] = true;
        }
    }

    static void LoadData()
    {
        if (File.Exists(DataFile))
        {
            foreach (var line in File.ReadAllLines(DataFile))
            {
                var parts = line.Split('|');
                bool[,] sch = new bool[7, 3];
                var data = parts[1].Split(',');
                int k = 0;
                for (int i = 0; i < 7; i++) for (int j = 0; j < 3; j++) sch[i, j] = bool.Parse(data[k++]);
                roomSchedules[parts[0]] = sch;
            }
        }
        else
        {
            for (int i = 101; i <= 103; i++) roomSchedules[i.ToString()] = new bool[7, 3];
            SaveData();
        }
    }

    static void SaveData()
    {
        lock (fileLock)
        {
            File.WriteAllLines(DataFile, roomSchedules.Select(r => $"{r.Key}|{string.Join(",", r.Value.Cast<bool>())}"));
        }
    }

    static void Send(NetworkStream s, string m) { byte[] d = Encoding.UTF8.GetBytes(m); s.Write(d, 0, d.Length); }
    static string Receive(NetworkStream s) { byte[] b = new byte[2048]; int r = s.Read(b, 0, b.Length); return Encoding.UTF8.GetString(b, 0, r).Trim().ToUpper(); }
}