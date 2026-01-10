# TCP Optimization with C++

## Giới thiệu
Đề tài tìm hiểu các phương pháp tối ưu hóa giao thức TCP thông qua
việc xây dựng chương trình Client – Server bằng ngôn ngữ C++.

## Nội dung
- Echo TCP: Server nhận dữ liệu và trả lại đúng dữ liệu cho Client.
- Chat TCP: Client và Server giao tiếp hai chiều qua kết nối TCP.

## Kỹ thuật tối ưu TCP
- TCP_NODELAY: Giảm độ trễ khi gửi dữ liệu nhỏ.
- SO_SNDBUF, SO_RCVBUF: Tăng kích thước bộ đệm gửi/nhận.
- SO_KEEPALIVE: Giữ kết nối TCP ổn định.

## Biên dịch & chạy (Windows – MinGW)
```bash
g++ echo_server.cpp -o echo_server -lws2_32
g++ echo_client.cpp -o echo_client -lws2_32
g++ chat_server.cpp -o chat_server -lws2_32
g++ chat_client.cpp -o chat_client -lws2_32
