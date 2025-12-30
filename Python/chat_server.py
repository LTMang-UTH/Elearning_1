import socket
import threading

clients = set()
clients_lock = threading.Lock()

def broadcast(message, sender=None):
    """Gửi tin nhắn đến tất cả client (trừ sender)"""
    with clients_lock:
        for client_socket in clients:
            if client_socket != sender:
                try:
                    client_socket.send(message.encode('utf-8'))
                except:
                    pass

def handle_client(client_socket, addr):
    """Xử lý từng client trong thread riêng"""
    addr_str = f"{addr[0]}:{addr[1]}"
    print(f" {addr_str} đã kết nối")
    
    with clients_lock:
        clients.add(client_socket)
    
    # Gửi thông báo join cho mọi người (trừ người mới)
    broadcast(f"*** {addr_str} đã vào phòng chat! ***\n", client_socket)
    
    # Gửi chào mừng riêng cho người mới
    try:
        client_socket.send(" Chào mừng bạn đến với Chat Server Python! \n".encode('utf-8'))
    except:
        pass
    
    try:
        while True:
            data = client_socket.recv(1024)
            if not data:
                break
            
            message = data.decode('utf-8').strip()
            if message.lower() == "/quit":
                break
            
            broadcast(f"{addr_str}: {message}\n", client_socket)
    except:
        pass
    finally:
        with clients_lock:
            clients.discard(client_socket)
        broadcast(f"*** {addr_str} đã rời phòng chat. ***\n")
        print(f" {addr_str} ngắt kết nối")
        client_socket.close()

def main():
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(('127.0.0.1', 8888))
    server.listen(5)
    
    print(" Python Chat Server đang chạy tại 127.0.0.1:8888")
    print("Mở nhiều terminal → gõ: telnet 127.0.0.1 8888 để chat!")
    
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
