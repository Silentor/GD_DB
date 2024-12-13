using Newtonsoft.Json;

namespace GDDB.Serialization;

/// <summary>
/// Stub class because Source Gen does not need to serialize GDObjects (Folders only)
/// </summary>
public class ObjectsJsonSerializer
{
    public GDObject Deserialize(JsonReader json )
    {
        throw new NotImplementedException();
    }
}