# Python TCP Socket Examples

## Cấu trúc
- `echo_server.py` - Echo server đơn giản (nhận và gửi lại tin nhắn)
- `echo_client.py` - Echo client để test echo server
- `chat_server.py` - Chat server hỗ trợ nhiều client
- `chat_client.py` - Chat client để kết nối đến chat server

## Cách sử dụng

### Echo Server/Client
1. Chạy server:
   ```bash
   python echo_server.py
   ```

2. Chạy client (terminal khác):
   ```bash
   python echo_client.py
   ```

3. Gõ tin nhắn và nhận echo lại từ server. Gõ `quit` để thoát.

### Chat Server/Client
1. Chạy server:
   ```bash
   python chat_server.py
   ```

2. Chạy nhiều client (mỗi terminal riêng):
   ```bash
   python chat_client.py
   ```

3. Gõ tin nhắn để chat với các client khác. Gõ `/quit` để thoát.

## Tính năng
-  Hỗ trợ nhiều client đồng thời (multi-threading)
-  Broadcast tin nhắn đến tất cả client
- Thông báo khi có người vào/rời phòng chat
- Echo server đơn giản để test kết nối

## Yêu cầu
- Python 3.6+
- Module `socket` và `threading` (có sẵn trong Python)
