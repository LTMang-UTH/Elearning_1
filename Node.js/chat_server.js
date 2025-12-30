const net = require("net");

const clients = new Set();

const server = net.createServer((socket) => {
  const addr = `${socket.remoteAddress}:${socket.remotePort}`;
  console.log(`✅ ${addr} đã kết nối`);
  clients.add(socket);

  // Gửi thông báo join cho mọi người (trừ người mới)
  broadcast(`*** ${addr} đã vào phòng chat! ***\n`, socket);

  // Gửi chào mừng riêng cho người mới
  socket.write("🌟 Chào mừng bạn đến với Chat Server Node.js! 🌟\n");

  socket.on("data", (data) => {
    const message = data.toString().trim();
    if (message.toLowerCase() === "/quit") {
      socket.end();
      return;
    }
    broadcast(`${addr}: ${message}\n`, socket);
  });

  socket.on("end", () => {
    clients.delete(socket);
    broadcast(`*** ${addr} đã rời phòng chat. ***\n`);
    console.log(`❌ ${addr} ngắt kết nối`);
  });
});

function broadcast(message, sender = null) {
  for (const client of clients) {
    if (client !== sender && client.writable) {
      client.write(message);
    }
  }
}

server.listen(8888, "127.0.0.1", () => {
  console.log("🚀 Node.js Chat Server đang chạy tại 127.0.0.1:8888");
  console.log("Mở nhiều terminal → gõ: telnet 127.0.0.1 8888 để chat!");
});
