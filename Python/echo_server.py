import socket
import threading

def handle_client(client_socket, addr):
    """Xử lý từng client trong thread riêng"""
    addr_str = f"{addr[0]}:{addr[1]}"
    print(f"Client kết nối: {addr_str}")
    
    try:
        while True:
            data = client_socket.recv(1024)
            if not data:
                break
            
            message = data.decode('utf-8').strip()
            print(f"Nhận: {message}")
            
            # Echo lại
            client_socket.send(data)
    except:
        pass
    finally:
        print("Client ngắt kết nối")
        client_socket.close()

def main():
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(('127.0.0.1', 8888))
    server.listen(5)
    
    print(" Echo Server đang chạy tại http://127.0.0.1:8888")
    print("Dùng telnet hoặc echo_client.py để test nhé!")
    
    try:
        while True:
            client_socket, addr = server.accept()
            thread = threading.Thread(target=handle_client, args=(client_socket, addr))
            thread.daemon = True
            thread.start()
    except KeyboardInterrupt:
        print("\n Server đang tắt...")
    finally:
        server.close()

if __name__ == "__main__":
    main()
