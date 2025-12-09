<?php
$server = socket_create(AF_INET, SOCK_STREAM, SOL_TCP);

// Cho phép reuse socket để tránh lỗi "Address already in use"
socket_set_option($server, SOL_SOCKET, SO_REUSEADDR, 1);

socket_bind($server, "127.0.0.1", 8080);
socket_listen($server);

echo "Server dang lang nghe tren 127.0.0.1:8080...\n";

while (true) {
    $client = socket_accept($server);
    echo "Client da ket noi!\n";

    // Đọc dữ liệu
    $input = socket_read($client, 1024);
    echo "Da nhan: $input\n";

    // Gửi lại phản hồi
    socket_write($client, "Server da nhan: " . $input);

    socket_close($client);
}
?>
