#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#pragma comment(lib, "ws2_32.lib")

int main() {
    WSADATA wsa;
    SOCKET serverSocket, clientSocket;
    sockaddr_in serverAddr, clientAddr;
    int clientSize = sizeof(clientAddr);

    // Khởi động Winsock
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
        std::cout << "WSAStartup failed\n";
        return 1;
    }

    // Tạo socket
    serverSocket = socket(AF_INET, SOCK_STREAM, 0);
    if (serverSocket == INVALID_SOCKET) {
        std::cout << "Socket creation failed\n";
        return 1;
    }

    // Cấu hình server
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = INADDR_ANY;
    serverAddr.sin_port = htons(8080);

    // Bind
    if (bind(serverSocket, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
        std::cout << "Bind failed\n";
        return 1;
    }

    // Listen
    listen(serverSocket, 1);
    std::cout << "Server đang chạy trên port 8080...\n";

    // Chờ client kết nối
    clientSocket = accept(serverSocket, (sockaddr*)&clientAddr, &clientSize);
    if (clientSocket == INVALID_SOCKET) {
        std::cout << "Accept failed\n";
        return 1;
    }

    std::cout << "Client da ket noi!\n";

    // Nhận dữ liệu
    char buffer[1024] = {0};
    int bytesReceived = recv(clientSocket, buffer, sizeof(buffer), 0);
    if (bytesReceived > 0) {
        std::cout << "Client nhan: " << buffer << "\n";
    }

    // Gửi phản hồi
    const char* msg = "Hello from C++ Server!";
    send(clientSocket, msg, strlen(msg), 0);

    // Đóng kết nối
    closesocket(clientSocket);
    closesocket(serverSocket);
    WSACleanup();

    return 0;
}
