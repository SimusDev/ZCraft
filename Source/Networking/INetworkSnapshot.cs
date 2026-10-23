namespace ZCraft.Source.Networking
{
    public interface INetworkSnapshot
    {
        void NetworkSnapshotSerialize(Serialization.BufferStream stream) { }
        void NetworkSnapshotDeserialize(Serialization.BufferStream stream) { }
    }


}
