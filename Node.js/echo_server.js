const net = require("net");

const server = net.createServer((socket) => {
  console.log(
    "Client kết nối:",
    socket.remoteAddress + ":" + socket.remotePort
  );

  socket.on("data", (data) => {
    console.log("Nhận:", data.toString().trim());
    socket.write(data); // Echo lại
  });

  socket.on("end", () => {
    console.log("Client ngắt kết nối");
  });
});

server.listen(8888, "127.0.0.1", () => {
  console.log("✅ Echo Server đang chạy tại http://127.0.0.1:8888");
  console.log("Dùng telnet hoặc echo_client.js để test nhé!");
});
