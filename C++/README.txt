1. Yêu cầu:
- Windows
- Môi trường biên dịch hỗ trợ Winsock (MinGW64 / Visual Studio)

2. Danh sách file:
- tcp_server.cpp  → chương trình server TCP
- tcp_client.cpp  → chương trình client TCP + kiểm thử tối ưu TCP

3. Cách biên dịch:
Nếu dùng MinGW (g++):
    g++ tcp_server.cpp -o server.exe -lws2_32
    g++ tcp_client.cpp -o client.exe -lws2_32


4. Cách chạy:
    Mở CMD thứ nhất:
        server.exe

    Mở CMD thứ hai:
        client.exe

5. Các kỹ thuật tối ưu TCP được minh họa trong client.cpp:
    - Tắt Nagle Algorithm (TCP_NODELAY)
    - Bật KeepAlive (SO_KEEPALIVE)
    - Thiết lập timeout gửi / nhận
    - Cho phép tái sử dụng địa chỉ socket (SO_REUSEADDR)

6. Mặc định server lắng nghe trên:
    127.0.0.1
    Port: 8080
