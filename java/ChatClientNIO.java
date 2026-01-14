import java.net.InetSocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.*;
import java.nio.charset.StandardCharsets;
import java.util.Scanner;

public class ChatClientNIO {

    public static void main(String[] args) throws Exception {

        /* =============================
           1. KẾT NỐI ĐẾN SERVER (NON-BLOCKING)
           ============================= */

        // SocketChannel thay cho Socket truyền thống
        SocketChannel socket = SocketChannel.open();

        // Kết nối tới Chat Server
        socket.connect(new InetSocketAddress("127.0.0.1", 8888));

        // BẮT BUỘC: chuyển sang non-blocking
        socket.configureBlocking(false);


        /* =============================
           2. TẠO SELECTOR (EVENT LOOP CLIENT)
           ============================= */

        // Selector dùng để lắng nghe dữ liệu từ server
        Selector selector = Selector.open();

        // Client chỉ quan tâm sự kiện READ (server gửi tin)
        socket.register(selector, SelectionKey.OP_READ);


        System.out.println("======================================");
        System.out.println(" Da ket noi toi Chat Server");
        System.out.println(" Go /quit de thoat");
        System.out.println("======================================");


        /* =============================
           3. THREAD GỬI TIN NHẮN (BLOCKING STDIN)
           ============================= */

        // Vì Scanner đọc bàn phím là BLOCKING
        // → tách riêng 1 thread chỉ để đọc stdin
        new Thread(() -> {
            try {
                // UTF-8 để gõ tiếng Việt không lỗi font
                Scanner sc = new Scanner(System.in, "UTF-8");

                while (true) {
                    // Chờ người dùng nhập (blocking)
                    String msg = sc.nextLine();

                    // Gửi tin nhắn lên server
                    socket.write(
                        StandardCharsets.UTF_8.encode(msg + "\n")
                    );
                }
            } catch (Exception e) {
                // Bỏ qua lỗi khi socket bị đóng
            }
        }).start();


        /* =============================
           4. EVENT LOOP NHẬN TIN NHẮN
           ============================= */

        // Buffer dùng chung để đọc dữ liệu từ server
        ByteBuffer buffer = ByteBuffer.allocate(1024);

        while (true) {

            // select() chờ đến khi server gửi dữ liệu
            // → KHÔNG busy-wait, KHÔNG tốn CPU
            selector.select();

            for (SelectionKey key : selector.selectedKeys()) {

                // Khi socket có dữ liệu để đọc
                if (key.isReadable()) {

                    SocketChannel ch =
                        (SocketChannel) key.channel();

                    buffer.clear();

                    // Đọc dữ liệu non-blocking
                    int bytes = ch.read(buffer);

                    if (bytes > 0) {

                        // Chuyển buffer sang chế độ đọc
                        buffer.flip();

                        // Decode UTF-8 để hiển thị tiếng Việt
                        String msg =
                            StandardCharsets.UTF_8
                                .decode(buffer)
                                .toString();

                        // In ra terminal ngay lập tức (realtime)
                        System.out.print(msg);
                    }
                }
            }

            // BẮT BUỘC: xóa key đã xử lý
            selector.selectedKeys().clear();
        }
    }
}
