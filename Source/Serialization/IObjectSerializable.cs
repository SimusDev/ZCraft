using System;

namespace ZCraft.Source.Serialization;

public interface IObjectSerializable
{
    void Serialize(BytePacker packer);
    void Deserialize(BytePacker packer);
}