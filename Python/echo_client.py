import socket

def main():
    client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    try:
        client.connect(('127.0.0.1', 8888))
        print("Đã kết nối đến Echo Server")
        
        while True:
            message = input("Bạn: ")
            
            if message.strip().lower() == 'quit':
                break
            
            client.send((message + '\n').encode('utf-8'))
            
            # Nhận echo từ server
            data = client.recv(1024)
            print(f"Echo từ server: {data.decode('utf-8').strip()}")
            
    except KeyboardInterrupt:
        print("\n Đang ngắt kết nối...")
    except Exception as e:
        print(f"Lỗi: {e}")
    finally:
        print(" Kết nối đã đóng")
        client.close()

if __name__ == "__main__":
    main()
