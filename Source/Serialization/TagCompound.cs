using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZCraft.Source.Serialization;

public class TagCompound
{
    private Dictionary<string, object> _base = new();

    public void Serialize(BytePacker packer)
    {
        packer.WriteInt(_base.Count);

        foreach(var pair in _base)
        {
            packer.WriteString(pair.Key);
            ObjectSerializer.Serialize(pair.Value, packer);
        }
    }

    public void Deserialize(BytePacker packer)
    {
        _base.Clear();

        int count = packer.ReadInt();
        for (int i = 0; i < count; i++)
        {
            string key = packer.ReadString();
            object value = ObjectSerializer.Deserialize(packer);
            _base[key] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBool(string key, bool value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBool(string key, bool @default = false)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (bool)result;
        }

        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetByte(string key, byte value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetByte(string key, byte @default = 0)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (byte)result;
        }

        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSByte(string key, sbyte value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte GetSByte(string key, sbyte @default = 0)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (sbyte)result;
        }

        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShort(string key, short value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short GetShort(string key, short @default = 0)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (short)result;
        }

        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUShort(string key, ushort value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetUShort(string key, ushort @default = 0)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (ushort)result;
        }

        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetInt(string key, int value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInt(string key, int @default = 0)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (int)result;
        }
        
        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUInt(string key, uint value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetUInt(string key, uint @default = 0)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (uint)result;
        }
        
        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLong(string key, long value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetLong(string key, long @default = 0)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (long)result;
        }
        
        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFloat(string key, float value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetFloat(string key, float @default = 0.0f)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (float)result;
        }
        
        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDouble(string key, double value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetDouble(string key, double @default = 0.0)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (double)result;
        }
        
        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetString(string key, string value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetString(string key, string @default = null)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (string)result;
        }
        
        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBytes(string key, byte[] value)
    {
        _base[key] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] GetBytes(string key, byte[] @default = null)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (byte[])result;
        }
        
        return @default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTagCompound(string key, TagCompound value)
    {
        _base[key] = value;
    }
    
    #nullable enable
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TagCompound? GetTagCompoundOrNull(string key)
    {
        if (_base.TryGetValue(key, out var result))
        {
            return (TagCompound)result;
        }
        
        return null;
    }
    #nullable disable


}
