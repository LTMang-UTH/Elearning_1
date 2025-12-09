#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#pragma comment(lib, "ws2_32.lib")

int main() {
    WSADATA wsa;
    SOCKET clientSocket;
    sockaddr_in serverAddr;

    // Khởi động Winsock
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
        std::cout << "WSAStartup failed\n";
        return 1;
    }

    // Tạo socket
    clientSocket = socket(AF_INET, SOCK_STREAM, 0);
    if (clientSocket == INVALID_SOCKET) {
        std::cout << "Socket creation failed\n";
        return 1;
    }

    // Cấu hình server muốn kết nối
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(8080);
    inet_pton(AF_INET, "127.0.0.1", &serverAddr.sin_addr); // Kết nối localhost

    // Connect
    if (connect(clientSocket, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
        std::cout << "Connect failed\n";
        return 1;
    }

    std::cout << "Da ket noi toi server!\n";

    // Gửi dữ liệu
    const char* msg = "Hello from C++ Client!";
    send(clientSocket, msg, strlen(msg), 0);

    // Nhận phản hồi
    char buffer[1024] = {0};
    int bytesReceived = recv(clientSocket, buffer, sizeof(buffer), 0);
    if (bytesReceived > 0) {
        std::cout << "Server tra ve: " << buffer << "\n";
    }

    // Đóng socket
    closesocket(clientSocket);
    WSACleanup();

    return 0;
}
