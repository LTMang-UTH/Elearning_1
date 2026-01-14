using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// ChatClient: Client chat TCP
/// </summary>
class ChatClient
{
    static void Main(string[] args)
    {
        // Kết nối đến server
        TcpClient client = new TcpClient("127.0.0.1", 6000);
        NetworkStream stream = client.GetStream();

        Console.WriteLine("=== CHAT CLIENT ===");

        // ===== NHẬP TÊN NGƯỜI DÙNG =====
        Console.Write("Nhập tên của bạn: ");
        string? userName = Console.ReadLine();

        // Gửi username lên server
        
byte[] nameData = Encoding.UTF8.GetBytes(userName + "\n");
stream.Write(nameData, 0, nameData.Length);


        Console.WriteLine("Đã kết nối tới máy chủ chat.");
        Console.WriteLine("Gõ /quit để thoát.");
        Console.WriteLine("-------------------------");

        // ===== THREAD NHẬN TIN NHẮN =====
        Thread nhanThread = new Thread(() =>
        {
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(message);
                }
                catch
                {
                    break;
                }
            }
        });

        nhanThread.Start();

        // ===== GỬI TIN NHẮN =====
        while (true)
        {
            string? message = Console.ReadLine();
            if (message == null) break;
            
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);

            if (message == "/quit")
                break;
        }

        client.Close();
        Console.WriteLine("Đã ngắt kết nối khỏi máy chủ.");
    }
}
