// tcp_client.js
const net = require("net");

const client = net.createConnection({ port: 9103, host: "127.0.0.1" }, () => {
  client.setNoDelay(true);
  client.setKeepAlive(true, 60000);
});

client.on("data", (data) => {
  console.log("Server:", data.toString().trim());
  client.write("ping from node client\n");
  client.end();
});
