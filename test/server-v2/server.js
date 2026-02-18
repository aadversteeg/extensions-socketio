var http = require("http");
var socketIO = require("socket.io");

var port = parseInt(process.argv[2]) || 3001;

var httpServer = http.createServer();
var io = socketIO(httpServer, {
  pingInterval: 300,
  pingTimeout: 200,
  origins: "*:*",
});

function setupNamespace(nsp) {
  nsp.on("connection", function (socket) {
    // Echo: on "message" -> emit "message-back" with same args
    socket.on("message", function () {
      var args = Array.prototype.slice.call(arguments);
      socket.emit.apply(socket, ["message-back"].concat(args));
    });

    // Echo with 1 parameter
    socket.on("1:emit", function (data) {
      socket.emit("1:emit", data);
    });

    // Echo with 2 parameters
    socket.on("2:emit", function (d1, d2) {
      socket.emit("2:emit", d1, d2);
    });

    // Ack: on "message-with-ack" -> call ack callback with same args
    socket.on("message-with-ack", function () {
      var args = Array.prototype.slice.call(arguments);
      var ack = args.pop();
      if (typeof ack === "function") {
        ack.apply(null, args);
      }
    });

    // Ack with 1 parameter — callback with same data
    socket.on("1:ack", function (data, cb) {
      cb(data);
    });

    // Return header value via ack callback
    socket.on("get_header", function (key, cb) {
      cb(socket.handshake.headers[key]);
    });

    // Server-side ack: server emits with callback, client responds, server echoes result
    socket.on("begin-ack-on-client", function () {
      socket.emit("ack-on-client", function (arg1, arg2) {
        socket.emit("end-ack-on-client", arg1, arg2);
      });
    });

    // Server-initiated disconnect on request
    socket.on("force-disconnect", function () {
      socket.disconnect(true);
    });
  });
}

// Default namespace
setupNamespace(io);

// Custom namespace
setupNamespace(io.of("/custom"));

httpServer.listen(port, function () {
  console.log("Socket.IO v2 test server listening on port " + port);
});
