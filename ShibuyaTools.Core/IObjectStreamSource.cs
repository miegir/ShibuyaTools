namespace ShibuyaTools.Core;

public interface IObjectStreamSource : IStreamSource
{
    bool Exists { get; }
    DateTime LastWriteTimeUtc { get; }
}
