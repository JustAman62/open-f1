using System.IO.Compression;
using System.Text;

namespace OpenF1.Data;

public static class DecompressUtilities
{
    public static string DeflateDecompress(string data)
    {
        using var inputStream = new MemoryStream(Convert.FromBase64String(data));
        using var compressionStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var reader = new StreamReader(compressionStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
