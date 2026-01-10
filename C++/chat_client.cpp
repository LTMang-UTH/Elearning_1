#include <winsock2.h>
#include <iostream>
#pragma comment(lib, "ws2_32.lib")

#define PORT 9090
#define BUF_SIZE 4096

int main() {
    WSADATA wsa;
    SOCKET client;
    sockaddr_in addr;
    char buffer[BUF_SIZE];

    WSAStartup(MAKEWORD(2,2), &wsa);
    client = socket(AF_INET, SOCK_STREAM, 0);

    addr.sin_family = AF_INET;
    addr.sin_port = htons(PORT);
    addr.sin_addr.s_addr = inet_addr("127.0.0.1");

    connect(client, (sockaddr*)&addr, sizeof(addr));

    while (true) {
        std::cout << "You: ";
        std::cin.getline(buffer, BUF_SIZE);
        send(client, buffer, strlen(buffer), 0);
        recv(client, buffer, BUF_SIZE, 0);
        std::cout << "Server: " << buffer << "\n";
    }

    closesocket(client);
    WSACleanup();
    return 0;
}
