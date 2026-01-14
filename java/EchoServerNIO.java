import java.net.InetSocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/*
 * EchoServerNIO
 * -------------
 * Server TCP bat dong bo su dung Java NIO
 * - Khong blocking I/O
 * - Su dung Selector lam Event Loop
 * - Xu ly nhieu client trong 1 thread
 */
public class EchoServerNIO {

    // Port rieng cho Echo Server (khong trung Chat Server)
    private static final int PORT = 9999;

    // Dem so client de gan ID
    private static int clientCounter = 0;

    // Luu danh sach client va ID tuong ung
    // SocketChannel -> clientId
    private static final Map<SocketChannel, Integer> clients = new HashMap<>();

    public static void main(String[] args) throws Exception {

        // Tao Selector
        // Selector chinh la EVENT LOOP cua Java NIO
        Selector selector = Selector.open();

        // Tao ServerSocketChannel (socket server)
        ServerSocketChannel server = ServerSocketChannel.open();

        // Bind server vao port
        server.bind(new InetSocketAddress(PORT));

        // Bat buoc chuyen sang non-blocking
        server.configureBlocking(false);

        // Dang ky server voi selector
        // OP_ACCEPT: cho phep nhan ket noi moi
        server.register(selector, SelectionKey.OP_ACCEPT);

        System.out.println("======================================");
        System.out.println(" ECHO SERVER NIO (Async / Non-blocking)");
        System.out.println(" Dang lang nghe tai port " + PORT);
        System.out.println("======================================");

        // VONG LAP CHINH - EVENT LOOP
        while (true) {

            // select() se block cho den khi co su kien I/O
            // Tuong duong await trong asyncio
            selector.select();

            // Lay tap cac su kien da san sang
            Iterator<SelectionKey> keys = selector.selectedKeys().iterator();

            while (keys.hasNext()) {
                SelectionKey key = keys.next();
                keys.remove(); // xoa sau khi xu ly

                // Neu co client moi ket noi
                if (key.isAcceptable()) {
                    acceptClient(server, selector);
                }

                // Neu client gui du lieu len
                if (key.isReadable()) {
                    readAndEcho(key);
                }
            }
        }
    }

    /*
     * Chap nhan client moi
     * - Tao SocketChannel
     * - Gan ID
     * - Dang ky OP_READ de nhan du lieu
     */
    private static void acceptClient(ServerSocketChannel server, Selector selector) throws Exception {

        // Chap nhan ket noi tu client
        SocketChannel client = server.accept();

        // Chuyen client sang non-blocking
        client.configureBlocking(false);

        // Tao ID moi cho client
        int clientId = ++clientCounter;
        clients.put(client, clientId);

        // Dang ky client voi selector
        // OP_READ: cho phep doc du lieu
        // attach ByteBuffer de luu du lieu
        client.register(selector, SelectionKey.OP_READ, ByteBuffer.allocate(1024));

        System.out.println("[SERVER] Client #" + clientId + " da ket noi");

        // Gui thong bao chao mung ve client
        sendToClient(
            client,
            "[SERVER] Xin chao Client #" + clientId + "!\n"
        );
    }

    /*
     * Doc du lieu tu client va echo lai
     */
    private static void readAndEcho(SelectionKey key) throws Exception {

        // Lay socket client tu key
        SocketChannel client = (SocketChannel) key.channel();

        // Lay buffer gan voi client
        ByteBuffer buffer = (ByteBuffer) key.attachment();

        // Doc du lieu tu socket
        int bytesRead = client.read(buffer);

        // Neu bytesRead == -1 -> client da dong ket noi
        if (bytesRead == -1) {
            disconnect(client);
            return;
        }

        // Chuyen buffer sang che do doc
        buffer.flip();

        // Decode byte -> String (UTF-8 de hien thi tieng Viet)
        String message = StandardCharsets.UTF_8
                .decode(buffer)
                .toString()
                .trim();

        // Xoa buffer de doc lan sau
        buffer.clear();

        // Neu client go "quit" thi ngat ket noi
        if (message.equalsIgnoreCase("quit")) {
            disconnect(client);
            return;
        }

        // Lay ID cua client
        int clientId = clients.get(client);

        // Tao chuoi echo co format dep
        String echoMsg =
            "[Echo] Client #" + clientId + ": " + message + "\n";

        // In ra man hinh server
        System.out.print(echoMsg);

        // Gui lai cho chinh client (echo)
        sendToClient(client, echoMsg);
    }

    /*
     * Ngat ket noi client
     */
    private static void disconnect(SocketChannel client) throws Exception {

        int id = clients.get(client);

        // Xoa client khoi danh sach
        clients.remove(client);

        // Dong socket
        client.close();

        System.out.println("[SERVER] Client #" + id + " da ngat ket noi");
    }

    /*
     * Gui du lieu ve client
     */
    private static void sendToClient(SocketChannel client, String msg) throws Exception {

        // Encode String -> ByteBuffer (UTF-8)
        client.write(StandardCharsets.UTF_8.encode(msg));
    }
}
