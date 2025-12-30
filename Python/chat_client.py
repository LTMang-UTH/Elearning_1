import socket
import sys
import threading

def receive_messages(client_socket):
    """Thread để nhận tin nhắn từ server"""
    try:
        while True:
            data = client_socket.recv(1024)
            if not data:
                break
            print(data.decode('utf-8'), end='')
    except:
        pass

def main():
    client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    try:
        client.connect(('127.0.0.1', 8888))
        print("Connected to server")
        
        # Tạo thread để nhận tin nhắn
        receive_thread = threading.Thread(target=receive_messages, args=(client,))
        receive_thread.daemon = True
        receive_thread.start()
        
        # Đọc dữ liệu từ bàn phím và gửi lên server
        while True:
            message = input()
            client.send((message + '\n').encode('utf-8'))
            
            if message.strip().lower() == '/quit':
                break
                
    except KeyboardInterrupt:
        print("\n Đang ngắt kết nối...")
    except Exception as e:
        print(f"Lỗi: {e}")
    finally:
        print(" Đã ngắt kết nối khỏi server")
        client.close()

if __name__ == "__main__":
    main()
