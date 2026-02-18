import { Server } from "socket.io";
import { createServer } from "http";

const port = parseInt(process.argv[2]) || 3000;

const httpServer = createServer();
const io = new Server(httpServer, {
  pingInterval: 300,
  pingTimeout: 200,
  connectTimeout: 1000,
  cors: {
    origin: "*",
  },
});

function setupNamespace(nsp) {
  nsp.on("connection", (socket) => {
    // Echo auth credentials back on connect
    socket.emit("auth", socket.handshake.auth);

    // Echo: on "message" → emit "message-back" with same args
    socket.on("message", (...args) => {
      socket.emit("message-back", ...args);
    });

    // Echo with 1 parameter
    socket.on("1:emit", (data) => {
      socket.emit("1:emit", data);
    });

    // Echo with 2 parameters
    socket.on("2:emit", (d1, d2) => {
      socket.emit("2:emit", d1, d2);
    });

    // Ack: on "message-with-ack" → call ack callback with same args
    socket.on("message-with-ack", (...args) => {
      const ack = args.pop();
      if (typeof ack === "function") {
        ack(...args);
      }
    });

    // Ack with 1 parameter — callback with same data
    socket.on("1:ack", (data, cb) => {
      cb(data);
    });

    // Return auth credentials via ack callback
    socket.on("get_auth", (cb) => {
      cb(socket.handshake.auth);
    });

    // Return header value via ack callback
    socket.on("get_header", (key, cb) => {
      cb(socket.handshake.headers[key]);
    });

    // Server-side ack: server emits with callback, client responds, server echoes result
    socket.on("begin-ack-on-client", () => {
      socket.emit("ack-on-client", (arg1, arg2) => {
        socket.emit("end-ack-on-client", arg1, arg2);
      });
    });

    // Server-initiated disconnect on request
    socket.on("force-disconnect", () => {
      socket.disconnect(true);
    });
  });
}

// Default namespace
setupNamespace(io);

// Custom namespace
setupNamespace(io.of("/custom"));

httpServer.listen(port, () => {
  console.log(`Socket.IO test server listening on port ${port}`);
});
