import java.net.InetSocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

public class ChatServerNIO {

    // Biến đếm để gán ID tăng dần cho mỗi client
    private static int clientCounter = 0;

    // Map lưu: SocketChannel -> clientId
    // Dùng để xác định ai gửi tin nhắn
    private static final Map<SocketChannel, Integer> clients = new HashMap<>();

    public static void main(String[] args) throws Exception {

        /* =============================
           1. TẠO SELECTOR (EVENT LOOP)
           ============================= */

        // Selector là "trái tim" của lập trình bất đồng bộ
        // Nó theo dõi tất cả socket non-blocking
        Selector selector = Selector.open();


        /* =============================
           2. TẠO SERVER SOCKET (NON-BLOCKING)
           ============================= */

        // ServerSocketChannel thay cho ServerSocket truyền thống
        ServerSocketChannel server = ServerSocketChannel.open();

        // Bind server vào port 8888
        server.bind(new InetSocketAddress(8888));

        // BẮT BUỘC: chuyển sang non-blocking
        server.configureBlocking(false);

        // Đăng ký server với selector, chỉ quan tâm sự kiện ACCEPT
        server.register(selector, SelectionKey.OP_ACCEPT);


        System.out.println("======================================");
        System.out.println(" CHAT SERVER NIO (Async / Non-blocking)");
        System.out.println(" Dang lang nghe tai port 8888");
        System.out.println("======================================");


        /* =============================
           3. EVENT LOOP CHÍNH
           ============================= */

        while (true) {

            // select() sẽ BLOCK nhẹ cho đến khi có sự kiện I/O
            // → không tốn CPU
            selector.select();

            // Lấy danh sách các sự kiện đã sẵn sàng
            Iterator<SelectionKey> keys =
                selector.selectedKeys().iterator();

            while (keys.hasNext()) {
                SelectionKey key = keys.next();
                keys.remove(); // BẮT BUỘC xóa key sau khi xử lý

                // Có client mới kết nối
                if (key.isAcceptable()) {
                    acceptClient(server, selector);
                }

                // Có client gửi dữ liệu
                if (key.isReadable()) {
                    readMessage(key);
                }
            }
        }
    }

    /* =============================
       4. XỬ LÝ CLIENT KẾT NỐI
       ============================= */
    private static void acceptClient(
        ServerSocketChannel server,
        Selector selector
    ) throws Exception {

        // Accept client mới
        SocketChannel client = server.accept();

        // Non-blocking
        client.configureBlocking(false);

        // Gán ID cho client
        int clientId = ++clientCounter;
        clients.put(client, clientId);

        // Đăng ký client với selector để đọc dữ liệu
        // Gắn kèm ByteBuffer làm attachment
        client.register(
            selector,
            SelectionKey.OP_READ,
            ByteBuffer.allocate(1024)
        );

        System.out.println(
            "[SERVER] Client #" + clientId + " da ket noi"
        );

        // Thông báo cho các client khác
        broadcast(
            "[SERVER] Client #" + clientId + " da vao phong chat\n",
            client
        );
    }

    /* =============================
       5. ĐỌC TIN NHẮN CLIENT
       ============================= */
    private static void readMessage(SelectionKey key)
        throws Exception {

        // Client gửi tin
        SocketChannel sender = (SocketChannel) key.channel();

        // Buffer gắn với client đó
        ByteBuffer buffer = (ByteBuffer) key.attachment();

        // Đọc dữ liệu (non-blocking)
        int bytesRead = sender.read(buffer);

        // Client đóng kết nối
        if (bytesRead == -1) {
            disconnect(sender);
            return;
        }

        // Chuyển buffer sang chế độ đọc
        buffer.flip();

        // Decode UTF-8 để hiển thị tiếng Việt đúng
        String message =
            StandardCharsets.UTF_8
                .decode(buffer)
                .toString()
                .trim();

        // Reset buffer cho lần đọc sau
        buffer.clear();

        // Client yêu cầu thoát
        if (message.equalsIgnoreCase("/quit")) {
            disconnect(sender);
            return;
        }

        int senderId = clients.get(sender);

        // Format tin nhắn
        String formatted =
            "[Client #" + senderId + "] " + message + "\n";

        // Log server
        System.out.print(formatted);

        // Gửi tin nhắn cho tất cả client khác (realtime)
        broadcast(formatted, sender);
    }

    /* =============================
       6. NGẮT KẾT NỐI CLIENT
       ============================= */
    private static void disconnect(SocketChannel client)
        throws Exception {

        int id = clients.get(client);

        // Xóa khỏi danh sách
        clients.remove(client);

        // Đóng socket
        client.close();

        System.out.println(
            "[SERVER] Client #" + id + " da ngat ket noi"
        );

        // Thông báo cho các client còn lại
        broadcast(
            "[SERVER] Client #" + id + " da roi phong chat\n",
            null
        );
    }

    /* =============================
       7. BROADCAST TIN NHẮN
       ============================= */
    private static void broadcast(
        String message,
        SocketChannel except
    ) throws Exception {

        // Encode message sang ByteBuffer
        ByteBuffer data =
            StandardCharsets.UTF_8.encode(message);

        // Gửi cho tất cả client
        for (SocketChannel client : clients.keySet()) {

            // Không gửi lại cho người gửi
            if (client != except && client.isOpen()) {

                // duplicate() để tránh position bị thay đổi
                client.write(data.duplicate());
            }
        }
    }
}
