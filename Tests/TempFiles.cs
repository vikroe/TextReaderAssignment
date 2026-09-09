using System.Text;

namespace Tests;

internal sealed class TempFiles : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("btr-tests-").FullName;

    public string Write(string name, ReadOnlySpan<byte> bytes)
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, bytes.ToArray());
        return path;
    }

    public string Write(string name, string text) => Write(name, Encoding.UTF8.GetBytes(text));

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
