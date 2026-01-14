using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class EchoServer
{
    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();

        Console.WriteLine("Máy chủ Echo đang chạy trên cổng 5000...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Một client vừa kết nối.");

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Nhận được từ client: " + message);

                if (message.Trim().ToLower() == "quit")
                {
                    Console.WriteLine("Client yêu cầu ngắt kết nối.");
                    break;
                }

                // Gửi lại dữ liệu cho client (Echo)
                stream.Write(buffer, 0, bytesRead);
            }

            client.Close();
            Console.WriteLine("Client đã ngắt kết nối.");
        }
    }
}
