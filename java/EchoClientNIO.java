import java.net.InetSocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.*;
import java.nio.charset.StandardCharsets;
import java.util.Scanner;

public class EchoClientNIO {

    // Port Echo Server đang chạy
    private static final int PORT = 9999;

    public static void main(String[] args) throws Exception {

        /* =============================
           1. TẠO KẾT NỐI SOCKET (NIO)
           ============================= */

        // Mở SocketChannel (thay cho Socket truyền thống)
        SocketChannel socket = SocketChannel.open();

        // Kết nối tới Echo Server
        socket.connect(new InetSocketAddress("127.0.0.1", PORT));

        // BẮT BUỘC: chuyển socket sang chế độ non-blocking
        // Nếu không có dòng này thì Selector sẽ không hoạt động đúng
        socket.configureBlocking(false);


        /* =============================
           2. TẠO SELECTOR (EVENT LOOP)
           ============================= */

        // Selector dùng để theo dõi các sự kiện I/O
        Selector selector = Selector.open();

        // Đăng ký socket với selector
        // Chỉ quan tâm sự kiện READ (server gửi dữ liệu về)
        socket.register(selector, SelectionKey.OP_READ);


        /* =============================
           3. THÔNG BÁO TRẠNG THÁI CLIENT
           ============================= */

        System.out.println("======================================");
        System.out.println(" Da ket noi Echo Server (port " + PORT + ")");
        System.out.println(" Go 'quit' de thoat");
        System.out.println("======================================");


        /* ==================================================
           4. THREAD RIÊNG ĐỌC BÀN PHÍM (BLOCKING I/O)
           ================================================== */

        // Scanner đọc bàn phím là BLOCKING
        // → phải tách ra thread riêng để không làm treo Event Loop
        new Thread(() -> {
            try {
                // Scanner đọc UTF-8 để hiển thị tiếng Việt đúng
                Scanner sc = new Scanner(System.in, "UTF-8");

                while (true) {
                    // Đọc 1 dòng người dùng nhập
                    String msg = sc.nextLine();

                    // Gửi dữ liệu lên server
                    // encode UTF-8 → ByteBuffer → write (non-blocking)
                    socket.write(
                        StandardCharsets.UTF_8.encode(msg + "\n")
                    );
                }
            } catch (Exception e) {
                // Thường xảy ra khi socket bị đóng
            }
        }).start();


        /* =============================
           5. BUFFER NHẬN DỮ LIỆU
           ============================= */

        // Buffer dùng để nhận dữ liệu từ server
        ByteBuffer buffer = ByteBuffer.allocate(1024);


        /* =============================
           6. EVENT LOOP NHẬN ECHO
           ============================= */

        while (true) {

            // Chờ sự kiện I/O (READ)
            // Không tốn CPU, chỉ thức dậy khi có dữ liệu
            selector.select();

            // Duyệt các sự kiện đã sẵn sàng
            for (SelectionKey key : selector.selectedKeys()) {

                // Kiểm tra socket có dữ liệu để đọc hay không
                if (key.isReadable()) {

                    // Lấy SocketChannel từ key
                    SocketChannel ch = (SocketChannel) key.channel();

                    // Xóa dữ liệu cũ trong buffer
                    buffer.clear();

                    // Đọc dữ liệu từ server (non-blocking)
                    int bytes = ch.read(buffer);

                    // Nếu có dữ liệu
                    if (bytes > 0) {

                        // Chuyển buffer sang chế độ đọc
                        buffer.flip();

                        // Decode ByteBuffer → String UTF-8
                        String msg =
                            StandardCharsets.UTF_8
                                .decode(buffer)
                                .toString();

                        // In ra terminal
                        System.out.print(msg);
                    }
                }
            }

            // BẮT BUỘC: xóa danh sách key đã xử lý
            // Nếu không sẽ bị xử lý lặp vô hạn
            selector.selectedKeys().clear();
        }
    }
}
