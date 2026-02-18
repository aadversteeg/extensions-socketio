using System;
using System.Collections.Generic;

namespace Ave.Extensions.SocketIO.Client.Protocol.Http;

/// <summary>
/// Represents an HTTP request.
/// </summary>
public class HttpRequest
{
    /// <summary>
    /// Gets or sets the request URI.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public RequestMethod Method { get; set; } = RequestMethod.Get;

    /// <summary>
    /// Gets or sets the body type.
    /// </summary>
    public RequestBodyType BodyType { get; set; } = RequestBodyType.Text;

    /// <summary>
    /// Gets or sets the request headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the body bytes.
    /// </summary>
    public byte[]? BodyBytes { get; set; }

    /// <summary>
    /// Gets or sets the body text.
    /// </summary>
    public string? BodyText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a connection request.
    /// </summary>
    public bool IsConnect { get; set; }
}

/// <summary>
/// HTTP request method.
/// </summary>
public enum RequestMethod
{
    /// <summary>HTTP GET method.</summary>
    Get,
    /// <summary>HTTP POST method.</summary>
    Post,
}

/// <summary>
/// HTTP request body type.
/// </summary>
public enum RequestBodyType
{
    /// <summary>Text body.</summary>
    Text,
    /// <summary>Binary body.</summary>
    Bytes,
}
