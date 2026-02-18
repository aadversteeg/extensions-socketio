using System.Collections.Generic;

namespace Ave.Extensions.SocketIO.Serialization;

/// <summary>
/// Holds the result of serializing event data, including JSON text and extracted binary attachments.
/// </summary>
public class SerializationResult
{
    /// <summary>
    /// Gets or sets the serialized JSON string.
    /// </summary>
    public string Json { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of binary attachments extracted during serialization.
    /// </summary>
    public ICollection<byte[]> Bytes { get; set; } = new List<byte[]>();
}
