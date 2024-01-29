namespace ShibuyaTools.Core;

public interface IBinaryAsset<T> where T : IBinaryAsset<T>
{
    abstract static T Read(BinaryReader reader);
    void WriteTo(BinaryWriter writer);
}
