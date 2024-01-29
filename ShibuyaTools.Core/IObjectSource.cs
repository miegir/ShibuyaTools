namespace ShibuyaTools.Core;

public interface IObjectSource<T>
{
    T Deserialize();
}
