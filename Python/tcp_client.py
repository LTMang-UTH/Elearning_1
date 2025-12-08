import socket
import time

HOST = "127.0.0.1"   
PORT = 5000          

def main():
    # Tạo socket TCP
    client_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    # Tối ưu TCP: tắt Nagle, tăng buffer
    client_sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
    client_sock.setsockopt(socket.SOL_SOCKET, socket.SO_SNDBUF, 64 * 1024)
    client_sock.setsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF, 64 * 1024)

    # Kết nối tới server
    client_sock.connect((HOST, PORT))
    print(f"[+] Connected to {HOST}:{PORT}")

    # In kích thước buffer thực tế
    print("Send buffer:", client_sock.getsockopt(socket.SOL_SOCKET, socket.SO_SNDBUF))
    print("Recv buffer:", client_sock.getsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF))

    # Tạo dữ liệu 1MB để gửi
    message = b"A" * (1024 * 1024)  # gửi 1MB

    start = time.time()
    # Gửi hết dữ liệu tới server
    client_sock.sendall(message)

    received = 0
    # Nhận lại dữ liệu từ server theo block 64KB
    while received < len(message):
        data = client_sock.recv(64 * 1024)
        if not data:
            break
        received += len(data)

    end = time.time()
    print(f"[+] Sent and received {received} bytes in {end - start:.4f} seconds")

    # Đóng socket
    client_sock.close()

if __name__ == "__main__":
    main()
