#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>

#pragma comment(lib, "ws2_32.lib")

#define PORT 8080
#define BUFFER_SIZE 4096

int main() {
    // Khởi tạo Winsock
    WSADATA wsaData;
    int wsaerr = WSAStartup(MAKEWORD(2, 2), &wsaData);
    if (wsaerr != 0) {
        std::cerr << "WSAStartup failed: " << wsaerr << std::endl;
        return 1;
    }

    // Tạo socket TCP
    SOCKET server = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (server == INVALID_SOCKET) {
        std::cerr << "Socket creation failed." << std::endl;
        WSACleanup();
        return 1;
    }

    // ------------------------------
    // TỐI ƯU TCP TRÊN WINDOWS
    // ------------------------------

    // (1) Disable Nagle (TCP_NODELAY)
    BOOL flag = TRUE;
    setsockopt(server, IPPROTO_TCP, TCP_NODELAY, (char*)&flag, sizeof(flag));

    // (2) Tăng buffer send/recv
    int bufSize = 4 * 1024 * 1024;
    setsockopt(server, SOL_SOCKET, SO_SNDBUF, (char*)&bufSize, sizeof(bufSize));
    setsockopt(server, SOL_SOCKET, SO_RCVBUF, (char*)&bufSize, sizeof(bufSize));

    // (3) Keep-Alive
    BOOL ka = TRUE;
    setsockopt(server, SOL_SOCKET, SO_KEEPALIVE, (char*)&ka, sizeof(ka));

    // (4) Non-blocking mode
    u_long mode = 1;
    ioctlsocket(server, FIONBIO, &mode);

    // ------------------------------
    // Thiết lập thông tin server
    // ------------------------------
    sockaddr_in serverAddr;
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(PORT);
    serverAddr.sin_addr.s_addr = INADDR_ANY;

    if (bind(server, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
        std::cerr << "Bind failed!" << std::endl;
        closesocket(server);
        WSACleanup();
        return 1;
    }

    listen(server, SOMAXCONN);
    std::cout << "Server đang chạy trên port " << PORT << "...\n";

    // ------------------------------
    // LOOP LẮNG NGHE CLIENT
    // ------------------------------
    SOCKET client;
    sockaddr_in clientAddr;
    int clientSize = sizeof(clientAddr);

    char buffer[BUFFER_SIZE];

    while (true) {
        client = accept(server, (sockaddr*)&clientAddr, &clientSize);

        if (client != INVALID_SOCKET) {
            std::cout << "Client kết nối!\n";

            while (true) {
                int bytesReceived = recv(client, buffer, BUFFER_SIZE, 0);

                if (bytesReceived > 0) {
                    buffer[bytesReceived] = '\0';
                    std::cout << "Client: " << buffer << std::endl;

                    // phản hồi lại
                    std::string msg = "Server received your message.\n";
                    send(client, msg.c_str(), msg.length(), 0);
                }
                else if (bytesReceived == 0) {
                    std::cout << "Client đã rời đi.\n";
                    closesocket(client);
                    break;
                }
                else {
                    int error = WSAGetLastError();
                    if (error != WSAEWOULDBLOCK) {
                        std::cout << "Lỗi nhận dữ liệu: " << error << std::endl;
                        closesocket(client);
                        break;
                    }
                }
            }
        }

        Sleep(10);
    }

    closesocket(server);
    WSACleanup();
    return 0;
}
