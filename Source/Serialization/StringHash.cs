using System;

namespace ZCraft.Source.Serialization;

public static class StringHash
{
    private const uint FnvOffsetBasis32 = 2166136261;
    private const uint FnvPrime32 = 16777619;
    private const ulong FnvOffsetBasis64 = 14695981039346656037;
    private const ulong FnvPrime64 = 1099511628211;

    public static uint Fnv1a32(string str)
    {
        uint hash = FnvOffsetBasis32;
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            // Хешируем оба байта char (little-endian)
            hash = (hash ^ (byte)c) * FnvPrime32;
            hash = (hash ^ (byte)(c >> 8)) * FnvPrime32;
        }
        return hash;
    }

    public static ulong Fnv1a64(string str)
    {
        ulong hash = FnvOffsetBasis64;
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            hash = (hash ^ (byte)c) * FnvPrime64;
            hash = (hash ^ (byte)(c >> 8)) * FnvPrime64;
        }
        return hash;
    }
}