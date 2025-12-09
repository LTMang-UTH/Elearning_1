<?php
echo "Client dang chay...\n";

// 1. Tạo socket
$socket = socket_create(AF_INET, SOCK_STREAM, SOL_TCP);

// =====================================
//  PHƯƠNG PHÁP TỐI ƯU HÓA TCP
// =====================================

// 1) Tắt Nagle Algorithm -> giảm độ trễ
socket_set_option($socket, SOL_TCP, TCP_NODELAY, 1);

// 2) Bật KeepAlive -> giữ kết nối ổn định
socket_set_option($socket, SOL_SOCKET, SO_KEEPALIVE, 1);

// 3) Timeout cho nhận dữ liệu (3 giây)
socket_set_option($socket, SOL_SOCKET, SO_RCVTIMEO, ["sec" => 3, "usec" => 0]);

// 4) Timeout cho gửi dữ liệu (3 giây)
socket_set_option($socket, SOL_SOCKET, SO_SNDTIMEO, ["sec" => 3, "usec" => 0]);

// 5) Cho phép tái sử dụng địa chỉ socket
socket_set_option($socket, SOL_SOCKET, SO_REUSEADDR, 1);

// =====================================
//  KẾT NỐI ĐẾN SERVER
// =====================================

if (!socket_connect($socket, "127.0.0.1", 8080)) {
    die("Khong the ket noi toi server!\n");
}

echo "Da ket noi toi server!\n";

// =====================================
//  GỬI DỮ LIỆU
// =====================================

$send = "Xin chao server! Day la client voi TCP toi uu.";
socket_write($socket, $send);

echo "Da gui: $send\n";

// Đợi server trả về
$response = socket_read($socket, 1024);
echo "Server phan hoi: $response\n";

// Đóng socket
socket_close($socket);
?>
