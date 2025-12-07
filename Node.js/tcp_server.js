// tcp_server.js
const net = require("net");

const server = net.createServer((socket) => {
  socket.setNoDelay(true); // disable Nagle
  socket.setKeepAlive(true, 60000);
  socket.write("Hello from Node.js server\n");
  socket.on("data", (data) => {
    console.log("Received:", data.toString().trim());
  });
  socket.end();
});

server.listen(9103, "127.0.0.1", () => {
  console.log("Node.js server listening 127.0.0.1:9103");
});
