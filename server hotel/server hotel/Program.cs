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
    private const string UsersFile = "users.txt";
    private const string RoomsFile = "rooms_data.txt";
    static List<User> users = new List<User>();
    static List<Room> rooms = new List<Room>();
    static object fileLock = new object();

    class User { public int Id; public string Username, Password, Role; }
    class Room
    {
        public string Number, Type; public int Price;
        public string[,] Schedule = new string[7, 3];
    }

    static void Main()
    {
        LoadUsers(); LoadRooms();
        TcpListener server = new TcpListener(IPAddress.Any, 8000);
        server.Start();
        Console.WriteLine("=== HOTEL SERVER PRO ACTIVE [PORT 8000] ===");
        while (true)
        {
            try
            {
                TcpClient client = server.AcceptTcpClient();
                new Thread(() => HandleClient(client)).Start();
            }
            catch { }
        }
    }

    static void HandleClient(TcpClient client)
    {
        using (client) using (NetworkStream stream = client.GetStream())
        {
            while (true)
            {
                try
                {
                    string request = Receive(stream);
                    if (string.IsNullOrEmpty(request) || request == "EXIT") break;
                    string[] p = request.Split(':');
                    switch (p[0])
                    {
                        case "SIGNUP": Send(stream, ProcessSignUp(p[1], p[2])); break;
                        case "LOGIN": Send(stream, ProcessLogin(p[1], p[2])); break;
                        case "VIEW": Send(stream, GetStatusReport(false)); break;
                        case "VIEW_AVAILABLE": Send(stream, GetStatusReport(true)); break;
                        case "BOOK": Send(stream, ProcessBooking(p[1], p[2], int.Parse(p[3]), int.Parse(p[4]), int.Parse(p[5]), int.Parse(p[6]))); break;
                        case "CANCEL_ALL": Send(stream, CancelAllUserBooking(p[1], p[2])); break;
                        case "ADDROOM": Send(stream, AddRoom(p[1], p[2])); break;
                        case "LISTUSERS": Send(stream, string.Join("\n", users.Select(u => $"ID: {u.Id} | Name: {u.Username} | Role: {u.Role}"))); break;
                        case "UPDATEUSER": Send(stream, UpdateUser(p[1], p[2], p[3], p[4])); break;
                        case "DELETEUSER": users.RemoveAll(u => u.Id.ToString() == p[1]); SaveUsers(); Send(stream, "SUCCESS: Deleted"); break;
                    }
                }
                catch { break; }
            }
        }
    }

    static string ProcessSignUp(string u, string p)
    {
        lock (users)
        {
            if (users.Any(x => x.Username == u)) return "FAILED: Taken";
            users.Add(new User { Id = users.Count + 1, Username = u, Password = p, Role = users.Count == 0 ? "Admin" : "User" });
            SaveUsers(); return "SUCCESS: Registered";
        }
    }

    static string ProcessLogin(string u, string p)
    {
        var user = users.FirstOrDefault(x => x.Username == u && x.Password == p);
        return user != null ? $"SUCCESS:{user.Role}:{user.Username}" : "FAILED: Invalid";
    }

    static string CancelAllUserBooking(string rNo, string uname)
    {
        var room = rooms.FirstOrDefault(r => r.Number == rNo);
        if (room == null) return "FAILED: Room not found";
        bool found = false;
        for (int i = 0; i < 7; i++)
            for (int j = 0; j < 3; j++)
                if (room.Schedule[i, j] == uname) { room.Schedule[i, j] = null; found = true; }
        if (found) { SaveRooms(); return "SUCCESS: Cleared"; }
        return "FAILED: No bookings found";
    }

    static string ProcessBooking(string rNo, string uname, int sD, int sP, int eD, int eP)
    {
        var room = rooms.FirstOrDefault(r => r.Number == rNo);
        if (room == null) return "FAILED";
        for (int d = sD - 1; d <= eD - 1; d++)
        {
            int start = (d == sD - 1) ? sP - 1 : 0;
            int end = (d == eD - 1) ? eP - 1 : 2;
            for (int p = start; p <= end; p++) if (!string.IsNullOrEmpty(room.Schedule[d, p])) return "FAILED: Occupied";
        }
        for (int d = sD - 1; d <= eD - 1; d++)
        {
            int start = (d == sD - 1) ? sP - 1 : 0;
            int end = (d == eD - 1) ? eP - 1 : 2;
            for (int p = start; p <= end; p++) room.Schedule[d, p] = uname;
        }
        SaveRooms(); return "SUCCESS: Booked";
    }

    static string AddRoom(string n, string t)
    {
        if (rooms.Any(r => r.Number == n)) return "FAILED: Exists";
        rooms.Add(new Room { Number = n, Type = t, Price = t == "Sweet" ? 300 : (t == "VIP" ? 200 : 100) });
        SaveRooms(); return "SUCCESS";
    }

    static string UpdateUser(string id, string n, string p, string r)
    {
        var u = users.FirstOrDefault(x => x.Id.ToString() == id);
        if (u == null) return "FAILED";
        u.Username = n; u.Password = p; u.Role = r; SaveUsers(); return "SUCCESS";
    }

    static string GetStatusReport(bool onlyAvail)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var r in rooms.OrderBy(x => x.Number))
        {
            bool hasSpace = false;
            StringBuilder temp = new StringBuilder();
            temp.AppendLine($"Room {r.Number} [{r.Type}] - ${r.Price}/Period");
            for (int d = 0; d < 7; d++)
            {
                temp.Append($" Day {d + 1}: ");
                for (int p = 0; p < 3; p++)
                {
                    bool isFree = string.IsNullOrEmpty(r.Schedule[d, p]);
                    if (isFree) hasSpace = true;
                    temp.Append(isFree ? "[A] " : $"[{r.Schedule[d, p]}] ");
                }
                temp.AppendLine();
            }
            if (!onlyAvail || hasSpace) sb.Append(temp.ToString());
        }
        return sb.ToString();
    }

    static void SaveUsers() { lock (fileLock) File.WriteAllLines(UsersFile, users.Select(u => $"{u.Id}|{u.Username}|{u.Password}|{u.Role}")); }
    static void LoadUsers() { if (File.Exists(UsersFile)) users = File.ReadAllLines(UsersFile).Select(l => { var p = l.Split('|'); return new User { Id = int.Parse(p[0]), Username = p[1], Password = p[2], Role = p[3] }; }).ToList(); }
    static void SaveRooms()
    {
        lock (fileLock)
        {
            var lines = rooms.Select(r => {
                string s = ""; for (int i = 0; i < 7; i++) for (int j = 0; j < 3; j++) s += (r.Schedule[i, j] ?? "NULL") + ",";
                return $"{r.Number}|{r.Type}|{r.Price}|{s.TrimEnd(',')}";
            });
            File.WriteAllLines(RoomsFile, lines);
        }
    }
    static void LoadRooms()
    {
        if (!File.Exists(RoomsFile)) return;
        rooms = File.ReadAllLines(RoomsFile).Select(l => {
            var p = l.Split('|'); var r = new Room { Number = p[0], Type = p[1], Price = int.Parse(p[2]) };
            var s = p[3].Split(','); int k = 0;
            for (int i = 0; i < 7; i++) for (int j = 0; j < 3; j++) r.Schedule[i, j] = s[k++] == "NULL" ? null : s[k - 1];
            return r;
        }).ToList();
    }
    static void Send(NetworkStream s, string m) { byte[] d = Encoding.UTF8.GetBytes(m); s.Write(d, 0, d.Length); }
    static string Receive(NetworkStream s) { byte[] b = new byte[10000]; int r = s.Read(b, 0, b.Length); return r > 0 ? Encoding.UTF8.GetString(b, 0, r).Trim() : ""; }
}