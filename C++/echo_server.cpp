#include <winsock2.h>
#include <iostream>
#pragma comment(lib, "ws2_32.lib")

#define PORT 8080
#define BUF_SIZE 4096

int main() {
    WSADATA wsa;
    SOCKET server, client;
    sockaddr_in addr, clientAddr;
    int clientSize = sizeof(clientAddr);
    char buffer[BUF_SIZE];

    WSAStartup(MAKEWORD(2,2), &wsa);

    server = socket(AF_INET, SOCK_STREAM, 0);

    int flag = 1, bufSize = 64 * 1024;
    setsockopt(server, IPPROTO_TCP, TCP_NODELAY, (char*)&flag, sizeof(flag));
    setsockopt(server, SOL_SOCKET, SO_SNDBUF, (char*)&bufSize, sizeof(bufSize));
    setsockopt(server, SOL_SOCKET, SO_RCVBUF, (char*)&bufSize, sizeof(bufSize));

    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = INADDR_ANY;
    addr.sin_port = htons(PORT);

    bind(server, (sockaddr*)&addr, sizeof(addr));
    listen(server, 1);

    std::cout << "Echo Server running...\n";
    client = accept(server, (sockaddr*)&clientAddr, &clientSize);

    int bytes = recv(client, buffer, BUF_SIZE, 0);
    send(client, buffer, bytes, 0); // Echo lại

    closesocket(client);
    closesocket(server);
    WSACleanup();
    return 0;
}