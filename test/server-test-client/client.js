import { io } from "socket.io-client";

const port = parseInt(process.argv[2]);
const scenario = process.argv[3];
const args = process.argv[4] ? JSON.parse(process.argv[4]) : {};

if (!port || !scenario) {
  console.error("Usage: node client.js <port> <scenario> [argsJson]");
  process.exit(1);
}

const baseUrl = `http://localhost:${port}`;

function output(type, data = {}) {
  console.log(JSON.stringify({ type, ...data }));
}

function connect(nsp = "/", opts = {}) {
  return io(`${baseUrl}${nsp}`, {
    reconnection: false,
    transports: ["websocket"],
    forceNew: true,
    ...opts,
  });
}

function waitConnect(socket) {
  return new Promise((resolve, reject) => {
    socket.on("connect", () => resolve());
    socket.on("connect_error", (err) => reject(err));
  });
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Global timeout to prevent hanging
const globalTimeout = setTimeout(() => {
  output("error", { message: "Global timeout exceeded" });
  process.exit(1);
}, parseInt(args.timeout) || 15000);

const scenarios = {
  // ── Connection scenarios ──────────────────────────────────────

  "connect-default-ws": async () => {
    const socket = connect("/", { transports: ["websocket"] });
    await waitConnect(socket);
    output("connected", { id: socket.id });
    socket.disconnect();
    output("done", { success: true });
  },

  "connect-default-polling": async () => {
    const socket = connect("/", { transports: ["polling"] });
    await waitConnect(socket);
    output("connected", { id: socket.id });
    socket.disconnect();
    output("done", { success: true });
  },

  "connect-custom-ns": async () => {
    const socket = connect(args.namespace || "/custom");
    await waitConnect(socket);
    output("connected", { id: socket.id, namespace: args.namespace || "/custom" });
    socket.disconnect();
    output("done", { success: true });
  },

  "connect-with-query": async () => {
    const query = args.query || {};
    const auth = args.auth || {};
    const socket = connect("/", { query, auth });
    await waitConnect(socket);
    output("connected", { id: socket.id });
    socket.disconnect();
    output("done", { success: true });
  },

  // ── Event scenarios ───────────────────────────────────────────

  "emit-event": async () => {
    const socket = connect("/");
    await waitConnect(socket);
    output("connected", { id: socket.id });

    const listenEvent = args.listenEvent || "echo-back";
    const emitEvent = args.emitEvent || "message";
    const emitData = args.emitData || ["hello"];

    const result = new Promise((resolve) => {
      socket.on(listenEvent, (...data) => {
        output("event", { name: listenEvent, data });
        resolve();
      });
    });

    socket.emit(emitEvent, ...emitData);
    await result;
    socket.disconnect();
    output("done", { success: true });
  },

  "receive-event": async () => {
    const socket = connect("/");
    const eventName = args.event || "greeting";

    const result = new Promise((resolve) => {
      socket.on(eventName, (...data) => {
        output("event", { name: eventName, data });
        resolve();
      });
    });

    await waitConnect(socket);
    output("connected", { id: socket.id });

    // Tell server we're ready to receive
    if (args.signalReady) {
      socket.emit("ready");
    }

    await result;
    socket.disconnect();
    output("done", { success: true });
  },

  "on-any": async () => {
    const socket = connect("/");
    const events = [];

    socket.onAny((eventName, ...data) => {
      events.push({ name: eventName, data });
      output("event", { name: eventName, data });
    });

    await waitConnect(socket);
    output("connected", { id: socket.id });

    // Emit events for server to echo
    const emitEvents = args.events || ["test-event"];
    for (const evt of emitEvents) {
      socket.emit(evt, "data");
    }

    // Wait for all echoes
    await sleep(args.waitMs || 1000);
    output("done", { success: true, eventCount: events.length });
    socket.disconnect();
  },

  "once-handler": async () => {
    const socket = connect("/");
    let count = 0;
    const eventName = args.event || "once-event";

    socket.on(eventName, () => {
      count++;
      output("event", { name: eventName, count });
    });

    await waitConnect(socket);
    output("connected", { id: socket.id });

    // Tell server we're ready
    socket.emit("ready");

    // Wait for server to send events
    await sleep(args.waitMs || 1500);
    output("done", { success: true, count });
    socket.disconnect();
  },

  // ── Acknowledgement scenarios ─────────────────────────────────

  "emit-with-ack": async () => {
    const socket = connect("/");
    await waitConnect(socket);
    output("connected", { id: socket.id });

    const event = args.event || "message-with-ack";
    const data = args.data || ["hello"];

    const ackData = await socket.emitWithAck(event, ...data);
    output("ack", { data: Array.isArray(ackData) ? ackData : [ackData] });
    socket.disconnect();
    output("done", { success: true });
  },

  "receive-ack": async () => {
    const socket = connect("/");
    const event = args.event || "ask-client";
    const ackResponse = args.ackResponse || ["response"];

    socket.on(event, (...params) => {
      const callback = params.pop();
      if (typeof callback === "function") {
        callback(...ackResponse);
        output("ack-sent", { data: ackResponse });
      }
    });

    await waitConnect(socket);
    output("connected", { id: socket.id });
    socket.emit("ready");

    // Wait for server to emit with ack
    await sleep(args.waitMs || 2000);
    output("done", { success: true });
    socket.disconnect();
  },

  // ── Multi-client scenarios (rooms, broadcast) ─────────────────

  "multi-client": async () => {
    const clientCount = args.clientCount || 2;
    const nsp = args.namespace || "/";
    const sockets = [];
    const connectedIds = [];

    for (let i = 0; i < clientCount; i++) {
      const socket = connect(nsp);
      sockets.push(socket);

      const idx = i;
      socket.onAny((eventName, ...data) => {
        output("event", { client: idx, name: eventName, data });
      });

      socket.on("disconnect", (reason) => {
        output("disconnected", { client: idx, reason });
      });
    }

    // Wait for all to connect
    await Promise.all(
      sockets.map((s, i) =>
        waitConnect(s).then(() => {
          connectedIds.push({ index: i, id: s.id });
        })
      )
    );

    output("all-connected", {
      clients: connectedIds.sort((a, b) => a.index - b.index),
    });

    // Signal readiness with client identity
    for (let i = 0; i < sockets.length; i++) {
      sockets[i].emit("ready", `client${i}`);
    }

    // Wait for test to complete
    await sleep(args.waitMs || 2000);
    output("done", { success: true });

    // Cleanup
    sockets.forEach((s) => s.disconnect());
  },

  // ── Disconnect scenarios ──────────────────────────────────────

  "client-disconnect": async () => {
    const socket = connect("/");
    await waitConnect(socket);
    output("connected", { id: socket.id });
    await sleep(200);
    socket.disconnect();
    output("disconnected", { reason: "io client disconnect" });
    output("done", { success: true });
  },

  "server-disconnect": async () => {
    const socket = connect("/");

    const disconnected = new Promise((resolve) => {
      socket.on("disconnect", (reason) => {
        output("disconnected", { reason });
        resolve();
      });
    });

    await waitConnect(socket);
    output("connected", { id: socket.id });

    // Tell server to disconnect us
    socket.emit("trigger-disconnect");

    // Wait for the disconnect event from server
    await disconnected;
    output("done", { success: true });
  },

  // ── Middleware scenarios ───────────────────────────────────────

  "middleware-connect": async () => {
    const auth = args.auth || {};
    const socket = connect("/", { auth });

    socket.on("connect", () => {
      output("connected", { id: socket.id });
      output("done", { success: true });
      socket.disconnect();
    });

    socket.on("connect_error", (err) => {
      output("connect-error", { message: err.message });
      output("done", { success: false });
    });

    // Wait for either connect or error
    await sleep(3000);
  },
};

// ── Main ──────────────────────────────────────────────────────────

const fn = scenarios[scenario];
if (!fn) {
  output("error", { message: `Unknown scenario: ${scenario}` });
  process.exit(1);
}

try {
  await fn();
} catch (err) {
  output("error", { message: err.message });
}

clearTimeout(globalTimeout);
process.exit(0);
