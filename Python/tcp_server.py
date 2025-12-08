import socket

HOST = "0.0.0.0"   
PORT = 5000        

def main():
    # Tạo socket TCP
    server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    # Cho phép dùng lại địa chỉ, tắt Nagle, tăng buffer
    server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
    server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_SNDBUF, 64 * 1024)
    server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF, 64 * 1024)

    server_sock.bind((HOST, PORT))
    server_sock.listen(5)
    print(f"[*] Server listening on {HOST}:{PORT}")

    while True:
        conn, addr = server_sock.accept()
        print(f"[+] New connection from {addr}")

        # Thử giảm latency ACK (nếu hệ điều hành hỗ trợ)
        try:
            conn.setsockopt(socket.IPPROTO_TCP, socket.TCP_QUICKACK, 1)
        except:
            pass

        total_bytes = 0
        while True:
            data = conn.recv(64 * 1024)   # Đọc block 64 KB
            if not data:
                break
            total_bytes += len(data)
            conn.sendall(data)            # Gửi lại (echo)

        print(f"[-] Connection closed from {addr}, received {total_bytes} bytes")
        conn.close()

if __name__ == "__main__":
    main()
