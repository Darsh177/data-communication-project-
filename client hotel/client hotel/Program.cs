using System;
using System.Net.Sockets;
using System.Text;

class HotelClient
{
    static string currentRole = "", currentUname = "";

    static void Main()
    {
        try
        {
            using (TcpClient client = new TcpClient("127.0.0.1", 8000))
            using (NetworkStream stream = client.GetStream())
            {
                while (true)
                {
                    if (string.IsNullOrEmpty(currentRole))
                    {
                        Console.WriteLine("\n--- MAIN MENU ---\n[1] Login [2] Signup [3] Exit System");
                        string c = Console.ReadLine();
                        if (c == "3") return;
                        Console.Write("Username: "); string u = Console.ReadLine();
                        Console.Write("Password: "); string p = Console.ReadLine();
                        Send(stream, (c == "1" ? "LOGIN:" : "SIGNUP:") + u + ":" + p);
                        string res = Receive(stream);
                        if (res.StartsWith("SUCCESS"))
                        {
                            var pts = res.Split(':'); currentRole = pts[1]; currentUname = pts[2];
                        }
                        Console.WriteLine("Server: " + res);
                    }
                    else
                    {
                        bool logout = currentRole == "Admin" ? AdminMenu(stream) : UserMenu(stream);
                        if (logout) { currentRole = ""; currentUname = ""; }
                    }
                }
            }
        }
        catch { Console.WriteLine("Connection Lost."); }
    }

    static bool UserMenu(NetworkStream s)
    {
        Console.WriteLine($"\n--- {currentUname} (USER) ---\n1. View Rooms\n2. Book Room\n3. Cancel/Change Booking\n4. Logout");
        string c = Console.ReadLine();
        if (c == "4") return true;
        if (c == "1") { Send(s, "VIEW"); Console.WriteLine(Receive(s)); }
        else if (c == "2") { PerformBooking(s); }
        else if (c == "3")
        {
            Console.Write("Room Number to Manage: "); string r = Console.ReadLine();
            Console.WriteLine("Option: [1] Cancel Completely [2] Change Time (Edit) [3] Back");
            string opt = Console.ReadLine();
            if (opt == "1") { Send(s, $"CANCEL_ALL:{r}:{currentUname}"); Console.WriteLine(Receive(s)); }
            else if (opt == "2")
            {
                Send(s, $"CANCEL_ALL:{r}:{currentUname}"); // مسح القديم
                if (Receive(s).Contains("SUCCESS"))
                {
                    Console.WriteLine("Old booking cleared. Enter new times:");
                    PerformBooking(s); // طلب الجديد
                }
            }
        }
        return false;
    }

    static void PerformBooking(NetworkStream s)
    {
        bool confirmed = false;
        while (!confirmed)
        {
            Console.WriteLine("\n>> Booking Details <<");
            Console.Write("Room Number: "); string r = Console.ReadLine();
            Console.Write("Start Day (1-7): "); int sd = int.Parse(Console.ReadLine());
            Console.Write("Start Period (1-3): "); int sp = int.Parse(Console.ReadLine());
            Console.Write("End Day (1-7): "); int ed = int.Parse(Console.ReadLine());
            Console.Write("End Period (1-3): "); int ep = int.Parse(Console.ReadLine());

            // حساب السعر التقديري
            int price = r.StartsWith("2") ? 200 : (r.StartsWith("3") ? 300 : 100);
            int totalP = ((ed - sd) * 3) + (ep - sp + 1);
            int totalCost = totalP * price;

            Console.WriteLine($"\nSUMMARY: Room {r} | Duration: {totalP} periods | Total: ${totalCost}");
            Console.Write("[1] Confirm [2] Re-enter [3] Back: ");
            string choice = Console.ReadLine();
            if (choice == "1")
            {
                Send(s, $"BOOK:{r}:{currentUname}:{sd}:{sp}:{ed}:{ep}");
                Console.WriteLine("Server: " + Receive(s)); confirmed = true;
            }
            else if (choice == "3") confirmed = true;
        }
    }

    static bool AdminMenu(NetworkStream s)
    {
        Console.WriteLine($"\n--- {currentUname} (ADMIN) ---\n1. Full Report\n2. View Available\n3. Add Room\n4. List Users\n5. Edit User\n6. Delete User\n7. Logout");
        string c = Console.ReadLine();
        if (c == "7") return true;
        if (c == "1") { Send(s, "VIEW"); Console.WriteLine(Receive(s)); }
        else if (c == "2") { Send(s, "VIEW_AVAILABLE"); Console.WriteLine(Receive(s)); }
        else if (c == "4") { Send(s, "LISTUSERS"); Console.WriteLine(Receive(s)); }
        else if (c == "5")
        {
            Console.Write("User ID: "); string id = Console.ReadLine();
            Console.Write("New Name: "); string n = Console.ReadLine();
            Console.Write("New Pass: "); string p = Console.ReadLine();
            Console.Write("New Role: "); string r = Console.ReadLine();
            Send(s, $"UPDATEUSER:{id}:{n}:{p}:{r}"); Console.WriteLine(Receive(s));
        }
        else if (c == "6") { Console.Write("User ID: "); Send(s, "DELETEUSER:" + Console.ReadLine()); Console.WriteLine(Receive(s)); }
        return false;
    }

    static void Send(NetworkStream s, string m) { byte[] d = Encoding.UTF8.GetBytes(m); s.Write(d, 0, d.Length); }
    static string Receive(NetworkStream s) { byte[] b = new byte[10000]; int r = s.Read(b, 0, b.Length); return r > 0 ? Encoding.UTF8.GetString(b, 0, r).Trim() : ""; }
}