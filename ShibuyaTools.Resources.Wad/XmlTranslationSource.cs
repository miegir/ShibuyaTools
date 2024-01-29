using System.Text.Json;
using ShibuyaTools.Core;

namespace ShibuyaTools.Resources.Wad;

internal class XmlTranslationSource(FileInfo info) : IObjectStreamSource
{
    public bool Exists => info.Exists;
    public DateTime LastWriteTimeUtc => info.LastWriteTime;
    public Stream OpenRead()
    {
        using var stream = info.OpenRead();
        var translations = JsonSerializer.Deserialize(stream, XmlContext.Relaxed.XmlTranslationArray) ?? [];
        return ObjectSerializer.SerializeToStream(translations);
    }
}
