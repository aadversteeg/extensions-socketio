# Ave.Extensions.SocketIO

A Socket.IO client library for .NET, supporting both HTTP long-polling and WebSocket transports with Engine.IO v3 and v4 protocols.

## Packages

| Package | Description |
|---------|-------------|
| `Ave.Extensions.SocketIO` | Core abstractions and protocol types |
| `Ave.Extensions.SocketIO.Client` | Client implementation with HTTP polling and WebSocket support |
| `Ave.Extensions.SocketIO.Serialization.NewtonsoftJson` | Optional Newtonsoft.Json serializer (System.Text.Json is used by default) |
| `Ave.Extensions.SocketIO.Server` | Server implementation (placeholder) |

**Target frameworks:** `netstandard2.1` and `net10.0`

## Quick Start

```csharp
using Ave.Extensions.SocketIO.Client;

// Create a client
var client = new SocketIOClient(new Uri("http://localhost:3000"));

// Register event handlers before connecting
client.On("message", async ctx =>
{
    var text = ctx.GetValue<string>(0);
    Console.WriteLine($"Received: {text}");
});

client.OnConnected += (sender, e) => Console.WriteLine("Connected!");
client.OnDisconnected += (sender, reason) => Console.WriteLine($"Disconnected: {reason}");

// Connect
await client.ConnectAsync();

// Emit events
await client.EmitAsync("message", new object[] { "Hello, server!" });

// Disconnect when done
await client.DisconnectAsync();
client.Dispose();
```

## Configuration

Configure the client via `SocketIOClientOptions`:

```csharp
var options = new SocketIOClientOptions
{
    EIO = EngineIOVersion.V4,              // Engine.IO version (default: V4)
    Transport = TransportProtocol.WebSocket, // Transport protocol (default: Polling)
    AutoUpgrade = true,                     // Auto-upgrade from polling to WebSocket (default: true)
    Reconnection = true,                    // Automatic reconnection (default: true)
    ReconnectionAttempts = 10,              // Max reconnection attempts (default: 10)
    ReconnectionDelayMax = 5000,            // Max delay between attempts in ms (default: 5000)
    ConnectionTimeout = TimeSpan.FromSeconds(30), // Connection timeout (default: 30s)
    Path = "socket.io",                     // Server path
    Query = new NameValueCollection { { "token", "abc123" } },
    ExtraHeaders = new Dictionary<string, string> { { "Authorization", "Bearer token" } },
    Auth = new { userId = "123" },          // Authentication credentials
};

var client = new SocketIOClient(new Uri("http://localhost:3000"), options);
```

## Event Handling

### Listening for Events

```csharp
// Listen for a specific event
client.On("chat", async ctx =>
{
    var sender = ctx.GetValue<string>(0);
    var message = ctx.GetValue<string>(1);
    Console.WriteLine($"{sender}: {message}");
});

// Listen for an event only once
client.Once("welcome", async ctx =>
{
    var greeting = ctx.GetValue<string>(0);
    Console.WriteLine(greeting);
});

// Listen for all events
client.OnAny(async (eventName, ctx) =>
{
    Console.WriteLine($"Event: {eventName}, Raw: {ctx.RawText}");
});

// Prepend a handler (runs before other OnAny handlers)
client.PrependAny(async (eventName, ctx) =>
{
    Console.WriteLine($"[First] {eventName}");
});

// Remove an OnAny handler
Func<string, IEventContext, Task> handler = async (name, ctx) => { };
client.OnAny(handler);
client.OffAny(handler);
```

### Emitting Events

```csharp
// Emit without data
await client.EmitAsync("ping");

// Emit with data
await client.EmitAsync("message", new object[] { "hello", 42, true });

// Emit with acknowledgement
await client.EmitAsync("request", new object[] { "data" }, async ack =>
{
    var response = ack.GetValue<string>(0);
    Console.WriteLine($"Server acknowledged: {response}");
});

// All emit methods support CancellationToken
await client.EmitAsync("message", new object[] { "hello" }, cancellationToken);
```

### Connection Events

```csharp
client.OnConnected += (sender, e) => Console.WriteLine("Connected");
client.OnDisconnected += (sender, reason) => Console.WriteLine($"Disconnected: {reason}");
client.OnError += (sender, error) => Console.WriteLine($"Error: {error}");
client.OnReconnectAttempt += (sender, attempt) => Console.WriteLine($"Reconnecting: attempt {attempt}");
client.OnReconnectError += (sender, ex) => Console.WriteLine($"Reconnect failed: {ex.Message}");
client.OnPing += (sender, e) => Console.WriteLine("Ping sent");
client.OnPong += (sender, duration) => Console.WriteLine($"Pong received: {duration.TotalMilliseconds}ms");
```

### Disconnect Reasons

When the `OnDisconnected` event fires, the reason will be one of:

| Reason | Description |
|--------|-------------|
| `io server disconnect` | The server forcefully disconnected the client |
| `io client disconnect` | The client intentionally disconnected |
| `ping timeout` | The connection was lost due to ping timeout |
| `transport close` | The transport was closed |
| `transport error` | A transport error occurred |

The client will automatically attempt to reconnect for transport-related disconnects when `Reconnection` is enabled. It will not reconnect for intentional disconnects (`io client disconnect` or `io server disconnect`).

## Namespaces

Connect to a specific namespace by including it in the URI path:

```csharp
var client = new SocketIOClient(new Uri("http://localhost:3000/admin"));
await client.ConnectAsync();
```

## Serialization

### System.Text.Json (Default)

The client uses `System.Text.Json` by default. No additional configuration is needed.

### Newtonsoft.Json

To use Newtonsoft.Json instead, install `Ave.Extensions.SocketIO.Serialization.NewtonsoftJson` and configure it:

```csharp
using Ave.Extensions.SocketIO.Serialization.NewtonsoftJson;

// With default settings
var client = new SocketIOClient(
    new Uri("http://localhost:3000"),
    services => services.AddNewtonsoftJsonSerializer());

// With custom settings
var client = new SocketIOClient(
    new Uri("http://localhost:3000"),
    services => services.AddNewtonsoftJsonSerializer(new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatString = "yyyy-MM-ddTHH:mm:ss"
    }));
```

## Supported Features

- Engine.IO v3 (Socket.IO 2.x) and v4 (Socket.IO 4.x/5.x)
- HTTP long-polling transport
- WebSocket transport
- Automatic transport upgrade (polling to WebSocket)
- Automatic reconnection with configurable attempts and delay
- Namespaces
- Event acknowledgements
- Binary message support
- Authentication credentials
- Custom headers and query parameters
- Dependency injection via `Microsoft.Extensions.DependencyInjection`
- `CancellationToken` support on all async operations