using System;
using System.Collections.Generic;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Player;

public class PlayerProfile
{
    private int _peerId = -1;
    private string _nickName = "player";

    public int PeerId => _peerId;
    public string NickName => _nickName;

    private static Dictionary<int, PlayerProfile> _profileByPeer = new();

    private static readonly List<PlayerProfile> _profileList = new(); 

    public static List<PlayerProfile> All => _profileList;

    public PlayerProfile(int peerId, string nickName)
    {
        _peerId = peerId;
        _nickName = nickName;
    }

    public PlayerProfile() {}

    public static void AddProfileToBase(PlayerProfile profile)
    {
        _profileByPeer.TryAdd(profile.PeerId, profile);
        _profileList.Add(profile);
    }

    public static void RemoveProfileFromBase(PlayerProfile profile)
    {
        _profileByPeer.Remove(profile.PeerId);
        _profileList.Remove(profile);
    }

    #nullable enable
    public static PlayerProfile? GetProfileByPeerId(int id)
    {
        if (_profileByPeer.TryGetValue(id, out var profile))
        {
            return profile;
        }

        return null;
    }
    #nullable disable

    public void Serialize(BytePacker packer)
    {
        packer.WriteInt(_peerId);
        packer.WriteString(_nickName);
    }

    public void Deserialize(BytePacker packer)
    {
        _peerId = packer.ReadInt();
        _nickName = packer.ReadString();
    }


}
