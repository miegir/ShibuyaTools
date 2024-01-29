namespace ShibuyaTools.Core;

public sealed class FileDestination(string path) : IFileStreamSource
{
    public FileState FileState => FileState.FromPath(path);
    public FileStream OpenRead() => File.OpenRead(path);

    public static implicit operator FileDestination(string path) => new(path);
}
