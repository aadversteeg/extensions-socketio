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

    // Ack: on "message-with-ack" -> call ack callback with same args
    socket.on("message-with-ack", function () {
      var args = Array.prototype.slice.call(arguments);
      var ack = args.pop();
      if (typeof ack === "function") {
        ack.apply(null, args);
      }
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
