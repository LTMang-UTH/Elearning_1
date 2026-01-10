#include <winsock2.h>
#include <iostream>
#pragma comment(lib, "ws2_32.lib")

#define PORT 8080
#define BUF_SIZE 4096

int main() {
    WSADATA wsa;
    SOCKET client;
    sockaddr_in serverAddr;
    char buffer[BUF_SIZE] = "Hello Echo TCP";

    WSAStartup(MAKEWORD(2,2), &wsa);

    client = socket(AF_INET, SOCK_STREAM, 0);

    int flag = 1;
    setsockopt(client, IPPROTO_TCP, TCP_NODELAY, (char*)&flag, sizeof(flag));

    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(PORT);
    serverAddr.sin_addr.s_addr = inet_addr("127.0.0.1");

    connect(client, (sockaddr*)&serverAddr, sizeof(serverAddr));

    send(client, buffer, strlen(buffer), 0);
    recv(client, buffer, BUF_SIZE, 0);

    std::cout << "Echo from server: " << buffer << "\n";

    closesocket(client);
    WSACleanup();
    return 0;
}
