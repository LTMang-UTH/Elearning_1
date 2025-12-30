const net = require("net");
const readline = require("readline");

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
});

const client = new net.Socket();

client.connect(8888, "127.0.0.1", () => {
  console.log("✅ Đã kết nối đến Echo Server");
  rl.setPrompt("Bạn: ");
  rl.prompt();
});

client.on("data", (data) => {
  console.log("Echo từ server:", data.toString().trim());
  rl.prompt();
});

client.on("close", () => {
  console.log("❌ Kết nối đã đóng");
  rl.close();
});

rl.on("line", (input) => {
  if (input.trim().toLowerCase() === "quit") {
    client.end();
    rl.close();
  } else {
    client.write(input + "\n");
  }
});
