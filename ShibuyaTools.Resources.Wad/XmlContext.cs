using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShibuyaTools.Resources.Wad;

[JsonSerializable(typeof(XmlTranslation[]))]
internal partial class XmlContext : JsonSerializerContext
{
    public static readonly XmlContext Relaxed = new(new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
    });
}
