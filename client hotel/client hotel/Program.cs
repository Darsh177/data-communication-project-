using System;
using System.Net.Sockets;
using System.Text;

class HotelClient
{
    static void Main()
    {
        try
        {
            using (TcpClient client = new TcpClient("127.0.0.1", 8000))
            using (NetworkStream stream = client.GetStream())
            {
                // بنعمل Clear مرة واحدة بس في بداية تشغيل البرنامج
                Console.Clear();
                Console.WriteLine("=== WELCOME TO HOTEL SECURE SYSTEM ===");

                while (true)
                {
                    // طباعة المنيو تحت البيانات السابقة
                    Console.WriteLine("\n-------------------------------");
                    Console.WriteLine("MAIN MENU:");
                    Console.WriteLine("1. View Available Rooms & Schedule");
                    Console.WriteLine("2. Book a Room Immediately");
                    Console.WriteLine("3. Exit");
                    Console.Write("Your Choice: ");

                    string choice = Console.ReadLine();

                    if (choice == "1")
                    {
                        // طلب العرض
                        Send(stream, "VIEW");
                        string response = Receive(stream);

                        // بنطبع الجدول مباشرة
                        Console.WriteLine(response);
                        Console.WriteLine("\n[Schedule Displayed Above] - Press Enter to show Menu again...");
                        Console.ReadLine();
                        // هنا مش هنعمل Clear، فالمنيو هتنطبع تحت الجدول في اللفة الجاية
                    }
                    else if (choice == "2")
                    {
                        Console.WriteLine("\n--- Fast Booking ---");
                        Console.Write("Room Number: "); string r = Console.ReadLine();
                        Console.Write("Start Day (1-7): "); string sd = Console.ReadLine();
                        Console.Write("Start Period (1-3): "); string sp = Console.ReadLine();
                        Console.Write("End Day (1-7): "); string ed = Console.ReadLine();
                        Console.Write("End Period (1-3): "); string ep = Console.ReadLine();

                        Send(stream, $"BOOK:{r}:{sd}:{sp}:{ed}:{ep}");
                        string response = Receive(stream);

                        Console.ForegroundColor = response.Contains("SUCCESS") ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.WriteLine("\nServer Response: " + response);
                        Console.ResetColor();

                        Console.Write("\nPress Enter to continue...");
                        Console.ReadLine();
                    }
                    else if (choice == "3")
                    {
                        Send(stream, "EXIT");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Invalid choice! Try again.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("\n[!] Connection Lost: " + ex.Message);
        }

        Console.WriteLine("\nApplication Closed. Press any key...");
        Console.ReadKey();
    }

    // دوال المساعدة للارسال والاستقبال
    static void Send(NetworkStream s, string m)
    {
        byte[] d = Encoding.UTF8.GetBytes(m);
        s.Write(d, 0, d.Length);
    }

    static string Receive(NetworkStream s)
    {
        byte[] b = new byte[4096];
        int r = s.Read(b, 0, b.Length);
        return Encoding.UTF8.GetString(b, 0, r).Trim();
    }
}