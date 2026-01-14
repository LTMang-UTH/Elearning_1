# Java NIO Async Chat & Echo Server

## 1. Giới thiệu
Dự án minh họa lập trình mạng bất đồng bộ (Asynchronous / Non-blocking I/O) trong Java bằng Java NIO.
Mục tiêu là hiểu rõ Event Loop, non-blocking socket và cách xây dựng hệ thống client/server realtime
mà không cần tạo nhiều thread cho mỗi kết nối.

## 2. Công nghệ sử dụng
- Java 17+ / JDK 23
- Java NIO: Selector, ServerSocketChannel, SocketChannel, ByteBuffer
- Charset UTF-8 (hỗ trợ tiếng Việt)
- Terminal / Command Line

## 3. Kiến trúc hệ thống
Mô hình Event Loop với một Selector duy nhất quản lý nhiều kết nối:
Client → Selector (Event Loop) → Server

Không sử dụng blocking I/O cho network, chỉ dùng 1 thread xử lý I/O.

## 4. Cấu trúc thư mục
.
├── ChatServerNIO.java
├── ChatClientNIO.java
├── EchoServerNIO.java
├── EchoClientNIO.java
└── README.md

## 5. Chức năng chính

### Echo Server (Port 9999)
- Nhận tin nhắn từ client và gửi lại ngay lập tức (echo)
- Mỗi client được gán ID riêng
- Hỗ trợ lệnh 'quit' để ngắt kết nối

### Chat Server (Port 8888)
- Cho phép nhiều client chat đồng thời
- Broadcast tin nhắn realtime tới tất cả client khác
- Hiển thị ID người gửi
- Thông báo khi client join / leave
- Hỗ trợ lệnh /quit

## 6. Hướng dẫn chạy chương trình

### Chạy Echo Server
java EchoServerNIO.java

Mở terminal khác chạy Echo Client:
java EchoClientNIO.java

### Chạy Chat Server
java ChatServerNIO.java

Mở nhiều terminal để chạy Chat Client:
java ChatClientNIO.java

## 7. Ví dụ kết quả

Chat:
[SERVER] Client #1 da vao phong chat
[Client #1] Xin chao moi nguoi
[Client #2] Hello

Echo:
[Echo] Client #1: Test message

## 8. Khái niệm lý thuyết áp dụng

- Blocking I/O: KHÔNG sử dụng
- Non-blocking I/O: SocketChannel
- Event Loop: selector.select()
- Coroutine (tương đương): state machine trong event loop
- Async/await (tương đương): Selector điều phối I/O
- Futures/Promises (tương đương): SelectionKey quản lý trạng thái

## 9. So sánh với mô hình Blocking

Blocking / Multi-thread:
- 1 client = 1 thread
- Tốn nhiều RAM, context switch cao
- Khó mở rộng

Java NIO:
- 1 thread quản lý nhiều client
- Tiết kiệm tài nguyên
- Khả năng scale tốt, realtime
