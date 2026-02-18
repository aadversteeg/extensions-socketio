using System.Text;

namespace UnitTests.Extensions.SocketIO.Helpers;

public class TestFile
{
    public int Size { get; set; }
    public string Name { get; set; } = null!;
    public byte[] Bytes { get; set; } = null!;

    public static readonly TestFile IndexHtml = new TestFile
    {
        Size = 1024,
        Name = "index.html",
        Bytes = Encoding.UTF8.GetBytes("Hello World!"),
    };

    public static readonly TestFile NiuB = new TestFile
    {
        Size = 666,
        Name = "NiuB",
        Bytes = Encoding.UTF8.GetBytes("\U0001f42e\U0001f37a"),
    };
}
