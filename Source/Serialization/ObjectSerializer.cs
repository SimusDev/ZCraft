using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using System.Reflection;
using Godot;

namespace ZCraft.Source.Serialization;

/// <summary>
/// Производительный объектный сериализатор с регистрацией типов.
/// В потоке пишется 2-байтовый TypeId, а не строка.
/// 
/// Использование:
///   ObjectSerializer.Register<Vector3>(1,
///       (v, p) => { p.WriteFloat(v.X); p.WriteFloat(v.Y); p.WriteFloat(v.Z); },
///       p => new Vector3(p.ReadFloat(), p.ReadFloat(), p.ReadFloat()));
///
///   ObjectSerializer.RegisterAllIObjectSerializable(typeof(ItemStack).Assembly);
///
///   ObjectSerializer.Serialize(obj, packer);
///   object result = ObjectSerializer.Deserialize(packer);
/// </summary>
public static class ObjectSerializer
{
    private const ushort NullMarker = 0;

    private static readonly Dictionary<Type, ushort> _typeToId = new();
    private static readonly Dictionary<ushort, Type> _idToType = new();
    private static readonly Dictionary<Type, Action<object, BytePacker>> _writers = new();
    private static readonly Dictionary<Type, Func<BytePacker, object>> _readers = new();

    private static ushort _nextId = 1;

    static ObjectSerializer()
    {
        Register<bool>(
            (v, p) => p.WriteBool(v),
            p => p.ReadBool());

        Register<byte>(
            (v, p) => p.WriteByte(v),
            p => p.ReadByte());
        
        Register<sbyte>(
            (v, p) => p.WriteSByte(v),
            p => p.ReadSByte());
        
        Register<ushort>(
            (v, p) => p.WriteUShort(v),
            p => p.ReadUShort());
        
        Register<short>(
            (v, p) => p.WriteShort(v),
            p => p.ReadShort());
        
        Register<int>(
            (v, p) => p.WriteInt(v),
            p => p.ReadInt());
        
        Register<uint>(
            (v, p) => p.WriteUInt(v),
            p => p.ReadUInt());
        
        Register<long>(
            (v, p) => p.WriteLong(v),
            p => p.ReadLong());
        
        Register<ulong>(
            (v, p) => p.WriteULong(v),
            p => p.ReadULong());
        
        Register<double>(
            (v, p) => p.WriteDouble(v),
            p => p.ReadDouble());
        
        Register<float>(
            (v, p) => p.WriteFloat(v),
            p => p.ReadFloat());
        
        Register<string>(
            (v, p) => p.WriteString(v),
            p => p.ReadString());
        
        Register<byte[]>(
            (v, p) =>
            {
                p.WriteVarInt(v.Length);
                p.WriteBytes(v);
            },
            p =>
            {
                int length = p.ReadVarInt();
                byte[] result = new byte[length];
                p.ReadBytes(result);
                return result;
            });
        
        Register<TagCompound>(
            (v, p) =>
            {
                v.Serialize(p);
            },
            p =>
            {
                var tagCompound = new TagCompound();
                tagCompound.Deserialize(p);
                return tagCompound;
            });

    }

    // ---------------------------------------------------------------
    //  Регистрация
    // ---------------------------------------------------------------

    /// <summary>
    /// Ручная регистрация типа с явным ID и делегатами.
    /// ID должен быть уникальным и > 0 (0 зарезервирован под null).
    /// </summary>
    private static void Register<T>(ushort id, Action<T, BytePacker> writer, Func<BytePacker, T> reader)
    {
        if (id == NullMarker)
            throw new ArgumentException("TypeId 0 зарезервирован под null", nameof(id));
        if (_idToType.ContainsKey(id))
            throw new InvalidOperationException($"TypeId {id} уже зарегистрирован за {_idToType[id]}");
        if (_typeToId.ContainsKey(typeof(T)))
            throw new InvalidOperationException($"Тип {typeof(T)} уже зарегистрирован");

        _typeToId[typeof(T)] = id;
        _idToType[id] = typeof(T);
        _writers[typeof(T)] = (obj, packer) => writer((T)obj, packer);
        _readers[typeof(T)] = packer => reader(packer);
    }

    /// <summary>
    /// Регистрация типа с автоматическим ID (следующий свободный).
    /// </summary>
    public static void Register<T>(Action<T, BytePacker> writer, Func<BytePacker, T> reader)
    {
        Register(_nextId, writer, reader);
        _nextId++;
    }

    /// <summary>
    /// Автоматически регистрирует все неабстрактные типы, реализующие IObjectSerializable,
    /// в указанной сборке. Рефлексия вызывается один раз при старте, не в горячем пути.
    /// </summary>
    public static void RegisterAllIObjectSerializable(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;
            if (!typeof(IObjectSerializable).IsAssignableFrom(type)) continue;
            if (_typeToId.ContainsKey(type)) continue;

            RegisterIObjectSerializable(type);
        }
    }

    /// <summary>
    /// Регистрирует конкретный тип, реализующий IObjectSerializable.
    /// </summary>
    public static void RegisterIObjectSerializable<T>() where T : IObjectSerializable, new()
    {
        RegisterIObjectSerializable(typeof(T));
    }

    private static void RegisterIObjectSerializable(Type type)
    {
        if (_typeToId.ContainsKey(type)) return;

        ushort id = _nextId++;
        _typeToId[type] = id;
        _idToType[id] = type;

        // Writer: вызываем интерфейс напрямую (без рефлексии)
        _writers[type] = (obj, packer) => ((IObjectSerializable)obj).Serialize(packer);

        // Reader: создаём инстанс через рефлексию (как ты просил)
        _readers[type] = packer =>
        {
            var instance = (IObjectSerializable)Activator.CreateInstance(type);
            instance.Deserialize(packer);
            return instance;
        };
    }

    // ---------------------------------------------------------------
    //  Сериализация
    // ---------------------------------------------------------------

    public static void Serialize(object obj, BytePacker packer)
    {
        if (obj == null)
        {
            packer.WriteUShort(NullMarker);
            return;
        }

        Type type = obj.GetType();

        if (!_typeToId.TryGetValue(type, out ushort id))
            throw new InvalidOperationException($"Тип {type} не зарегистрирован в ObjectSerializer");

        packer.WriteUShort(id);

        if (!_writers.TryGetValue(type, out var writer))
            throw new InvalidOperationException($"Нет writer'а для {type}");

        writer(obj, packer);
    }

    // ---------------------------------------------------------------
    //  Десериализация
    // ---------------------------------------------------------------

    /// <summary>
    /// Читает объект из потока. Тип определяется по TypeId из потока.
    /// </summary>
    
    public static object Deserialize(BytePacker packer)
    {
        ushort id = packer.ReadUShort();

        if (id == NullMarker)
            return null;

        if (!_idToType.TryGetValue(id, out Type type))
            throw new InvalidOperationException($"TypeId {id} не зарегистрирован в ObjectSerializer");

        if (!_readers.TryGetValue(type, out var reader))
            throw new InvalidOperationException($"Нет reader'а для {type}");

        return reader(packer);
    }

    /// </summary>
    public static T Deserialize<T>(BytePacker packer) where T : class
    {
        object result = Deserialize(packer);
        if (result == null) return null;
        if (result is not T typed)
            throw new InvalidCastException($"Ожидался {typeof(T)}, получен {result.GetType()}");
        return typed;
    }

    // ---------------------------------------------------------------
    //  Утилиты
    // ---------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRegistered(Type type) => _typeToId.ContainsKey(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetId(Type type, out ushort id) => _typeToId.TryGetValue(type, out id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetType(ushort id, out Type type) => _idToType.TryGetValue(id, out type);

}