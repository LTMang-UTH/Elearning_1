using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// ChatServer: Máy chủ chat TCP hỗ trợ nhiều client cùng lúc
/// </summary>
class ChatServer
{
    // Danh sách các client đang kết nối
    static List<TcpClient> clients = new List<TcpClient>();

    // Lưu tên người dùng tương ứng với client
    static Dictionary<TcpClient, string> userNames = new Dictionary<TcpClient, string>();

    static void Main(string[] args)
    {
        // Tạo TCP Server lắng nghe cổng 6000
        TcpListener server = new TcpListener(IPAddress.Any, 6000);
        server.Start();

        Console.WriteLine("=== MÁY CHỦ CHAT TCP ===");
        Console.WriteLine("Máy chủ đang chạy trên cổng 6000...");
        Console.WriteLine("-------------------------");

        // Server luôn chạy
        while (true)
        {
            // Chấp nhận client kết nối
            TcpClient client = server.AcceptTcpClient();
            clients.Add(client);

            // Tạo thread riêng xử lý client
            Thread t = new Thread(() => XuLyClient(client));
            t.Start();
        }
    }

    /// <summary>
    /// Xử lý một client cụ thể
    /// </summary>
    static void XuLyClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            // ===== NHẬN USERNAME =====
            stream.Read(buffer, 0, buffer.Length);
            string userName = Encoding.UTF8.GetString(buffer).Trim('\0');

            userNames.Add(client, userName);

            string joinMsg = $"[{LayThoiGian()}] 🔔 {userName} đã tham gia phòng chat.";
            GuiTinNhan(joinMsg);

            Console.WriteLine(joinMsg);

            // ===== NHẬN TIN NHẮN CHAT =====
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Nếu client gõ /quit thì thoát
                if (message.Trim() == "/quit")
                    break;

                string fullMessage =
                    $"[{LayThoiGian()}] {userName}: {message}";

                GuiTinNhan(fullMessage);
                Console.WriteLine(fullMessage);
            }
        }
        catch
        {
            // Bỏ qua lỗi
        }
        finally
        {
            // ===== CLIENT RỜI PHÒNG =====
            string leaveMsg =
                $"[{LayThoiGian()}] ❌ {userNames[client]} đã rời phòng chat.";

            clients.Remove(client);
            userNames.Remove(client);

            GuiTinNhan(leaveMsg);
            Console.WriteLine(leaveMsg);

            client.Close();
        }
    }

    /// <summary>
    /// Gửi tin nhắn đến tất cả client
    /// </summary>
    static void GuiTinNhan(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        foreach (TcpClient c in clients)
        {
            try
            {
                c.GetStream().Write(data, 0, data.Length);
            }
            catch { }
        }
    }

    /// <summary>
    /// Lấy thời gian hiện tại (HH:mm:ss)
    /// </summary>
    static string LayThoiGian()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }
}
