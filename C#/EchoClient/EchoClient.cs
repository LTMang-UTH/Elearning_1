using System;
using System.Net.Sockets;
using System.Text;

class EchoClient
{
    static void Main(string[] args)
    {
        TcpClient client = new TcpClient("127.0.0.1", 5000);
        NetworkStream stream = client.GetStream();

        Console.WriteLine("Đã kết nối tới máy chủ Echo.");
        Console.WriteLine("Gõ 'quit' để thoát.");

        while (true)
        {
            Console.Write("Nhập tin nhắn: ");
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string message = Console.ReadLine();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (string.IsNullOrEmpty(message))
                continue;

            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);

            if (message.ToLower() == "quit")
            {
                Console.WriteLine("Đang ngắt kết nối...");
                break;
            }

            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            Console.WriteLine("Server phản hồi: " +
                Encoding.UTF8.GetString(buffer, 0, bytesRead));
        }

        client.Close();
        Console.WriteLine("Đã đóng kết nối với máy chủ.");
    }
}
