const net = require("net");

const client = net.createConnection({ port: 8888 }, () => {
  console.log("Connected to server");
});

client.on("data", (data) => {
  console.log(data.toString());
});

// 👉 đọc dữ liệu từ bàn phím và gửi lên server
process.stdin.on("data", (data) => {
  client.write(data);
});

client.on("end", () => {
  console.log("Disconnected from server");
});
