#include <winsock2.h>
#include <iostream>
#pragma comment(lib, "ws2_32.lib")

#define PORT 9090
#define BUF_SIZE 4096

int main() {
    WSADATA wsa;
    SOCKET server, client;
    sockaddr_in addr;
    char buffer[BUF_SIZE];

    WSAStartup(MAKEWORD(2,2), &wsa);
    server = socket(AF_INET, SOCK_STREAM, 0);

    int flag = 1;
    setsockopt(server, SOL_SOCKET, SO_KEEPALIVE, (char*)&flag, sizeof(flag));

    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = INADDR_ANY;
    addr.sin_port = htons(PORT);

    bind(server, (sockaddr*)&addr, sizeof(addr));
    listen(server, 1);

    std::cout << "Chat server started...\n";
    client = accept(server, NULL, NULL);

    while (true) {
        int bytes = recv(client, buffer, BUF_SIZE, 0);
        if (bytes <= 0) break;
        buffer[bytes] = '\0';
        std::cout << "Client: " << buffer << "\n";
        send(client, buffer, bytes, 0);
    }

    closesocket(client);
    closesocket(server);
    WSACleanup();
    return 0;
}
