using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using ZCraft.Source.Networking;

namespace ZCraft.Source.Serialization;

/// <summary>
/// Единый байтовый пакер: пишет и читает примитивы в один self-growing буфер.
/// Все многобайтовые значения — Little-Endian.
/// Не потокобезопасен. Переиспользуй через Reset() или пул.
/// </summary>
public sealed class BytePacker : IDisposable
{
	#nullable enable
	private byte[] _buffer;
	private int _position;      // текущая позиция в байтах
	private int _readStart;     // начало логических данных (для Wrap)
	private int _readEnd;       // конец логических данных (сколько записано / валидно)
	private bool _isReading;

	public BytePacker(int initialCapacity = 1024)
	{
		if (initialCapacity < 16) initialCapacity = 16;
		_buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
	}

	// ---- Состояние ----

	public byte[] Buffer => _buffer;
	public int Position => _position;
	public int Length => _isReading ? _readEnd - _readStart : _readEnd;
	public int Remaining => _isReading ? _readEnd - _position : _buffer.Length - _position;
	public bool IsReading => _isReading;
	public bool IsWriting => !_isReading;

	public bool IsFromPool = false;

	// ---- Режимы ----

	public void ResetMode(bool readMode)
	{
		if (_isReading) _readEnd = Math.Max(_readEnd, _position);
		_isReading = readMode;
	}

	public void ResetPosition()
	{
		_position = _isReading ? _readStart : 0;
		if (!_isReading) _readEnd = 0;
	}

	public void ResetPositionAndMode(bool readMode)
	{
		ResetMode(readMode);
		ResetPosition();
	}

	/// <summary>Полный сброс для переиспользования из пула.</summary>
	public void Reset()
	{
		_position = 0;
		_readStart = 0;
		_readEnd = 0;
		_isReading = false;
	}

	// ---- Обёртка над чужим срезом (без копирования, только чтение) ----

	/// <summary>
	/// Обёртка над чужим срезом (копирует в свой буфер, режим чтения).
	/// Пакер должен быть чистым (Reset) либо уже в режиме чтения.
	/// </summary>
	public void Wrap(ReadOnlySpan<byte> data)
	{
		if (!_isReading && _position != 0)
			throw new InvalidOperationException("Wrap requires a clean packer (call Reset first).");

		if (data.Length > _buffer.Length)
		{
			ArrayPool<byte>.Shared.Return(_buffer);
			int newSize = _buffer.Length;
			while (newSize < data.Length) newSize <<= 1;
			_buffer = ArrayPool<byte>.Shared.Rent(newSize);
		}

		data.CopyTo(_buffer);
		_readStart = 0;
		_readEnd = data.Length;
		_position = 0;
		_isReading = true;
	}

	// ---- Запись: примитивы ----

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Span<byte> EnsureWrite(int size)
	{
		if (_isReading) throw new InvalidOperationException("Packer is in read mode.");
		if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));

		int required = _position + size;
		if (required > _buffer.Length)
			Grow(required);

		var slice = _buffer.AsSpan(_position, size);
		_position += size;
		if (_position > _readEnd) _readEnd = _position;
		return slice;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Grow(int required)
	{
		int newSize = _buffer.Length;
		while (newSize < required) newSize <<= 1;
		if (newSize < 0) newSize = required; // overflow guard

		byte[] newBuf = ArrayPool<byte>.Shared.Rent(newSize);
		System.Array.Copy(_buffer, newBuf, _position);
		ArrayPool<byte>.Shared.Return(_buffer);
		_buffer = newBuf;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteByte(byte value) => EnsureWrite(1)[0] = value;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteSByte(sbyte value) => EnsureWrite(1)[0] = unchecked((byte)value);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteBool(bool value) => EnsureWrite(1)[0] = value ? (byte)1 : (byte)0;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteShort(short value) => BinaryPrimitives.WriteInt16LittleEndian(EnsureWrite(2), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteUShort(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(EnsureWrite(2), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteInt(int value) => BinaryPrimitives.WriteInt32LittleEndian(EnsureWrite(4), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteUInt(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(EnsureWrite(4), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteLong(long value) => BinaryPrimitives.WriteInt64LittleEndian(EnsureWrite(8), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteULong(ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(EnsureWrite(8), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteFloat(float value) => BinaryPrimitives.WriteSingleLittleEndian(EnsureWrite(4), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteDouble(double value) => BinaryPrimitives.WriteDoubleLittleEndian(EnsureWrite(8), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteBytes(ReadOnlySpan<byte> bytes)
	{
		if (bytes.IsEmpty) return;
		bytes.CopyTo(EnsureWrite(bytes.Length));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteString(string? value)
	{
		if (value is null) { WriteInt(-1); return; }
		int byteCount = Encoding.UTF8.GetByteCount(value);
		WriteInt(byteCount);
		if (byteCount == 0) return;
		var dst = EnsureWrite(byteCount);
		Encoding.UTF8.GetBytes(value, dst);
	}

	// ---- Чтение: примитивы ----

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ReadOnlySpan<byte> EnsureRead(int size)
	{
		if (!_isReading) throw new InvalidOperationException("Packer is in write mode.");
		if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
		if (_position + size > _readEnd)
			throw new IndexOutOfRangeException("BytePacker buffer underflow");
		var slice = _buffer.AsSpan(_position, size);
		_position += size;
		return slice;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte ReadByte() => EnsureRead(1)[0];
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public sbyte ReadSByte() => unchecked((sbyte)EnsureRead(1)[0]);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ReadBool() => EnsureRead(1)[0] != 0;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public short ReadShort() => BinaryPrimitives.ReadInt16LittleEndian(EnsureRead(2));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort ReadUShort() => BinaryPrimitives.ReadUInt16LittleEndian(EnsureRead(2));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReadInt() => BinaryPrimitives.ReadInt32LittleEndian(EnsureRead(4));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint ReadUInt() => BinaryPrimitives.ReadUInt32LittleEndian(EnsureRead(4));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long ReadLong() => BinaryPrimitives.ReadInt64LittleEndian(EnsureRead(8));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ulong ReadULong() => BinaryPrimitives.ReadUInt64LittleEndian(EnsureRead(8));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float ReadFloat() => BinaryPrimitives.ReadSingleLittleEndian(EnsureRead(4));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(EnsureRead(8));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReadBytes(Span<byte> destination)
	{
		if (destination.IsEmpty) return;
		EnsureRead(destination.Length).CopyTo(destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string? ReadString()
	{
		int byteCount = ReadInt();
		if (byteCount < 0) return null;
		if (byteCount == 0) return string.Empty;
		var slice = EnsureRead(byteCount);
		return Encoding.UTF8.GetString(slice);
	}

	// ---- Simusdev's Extensions ----

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteVarUInt(uint value) => WriteVarULong(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteVarULong(ulong value)
	{
		while (value >= 0x80)
		{
			EnsureWrite(1)[0] = (byte)(value | 0x80);
			value >>= 7;
		}
		EnsureWrite(1)[0] = (byte)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint ReadVarUInt() => (uint)ReadVarULong();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ulong ReadVarULong()
	{
		ulong result = 0;
		int shift = 0;
		while (true)
		{
			byte b = EnsureRead(1)[0];
			result |= (ulong)(b & 0x7F) << shift;
			if ((b & 0x80) == 0) break;
			shift += 7;
			if (shift >= 64) throw new InvalidOperationException("VarULong overflow");
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ZigZagEncode(int v) => (uint)((v << 1) ^ (v >> 31));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ZigZagDecode(uint v) => (int)(v >> 1) ^ -(int)(v & 1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong ZigZagEncode(long v) => (ulong)((v << 1) ^ (v >> 63));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ZigZagDecode(ulong v) => (long)(v >> 1) ^ -(long)(v & 1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteVarInt(int value) => WriteVarUInt(ZigZagEncode(value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteVarLong(long value) => WriteVarULong(ZigZagEncode(value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReadVarInt() => ZigZagDecode(ReadVarUInt());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long ReadVarLong() => ZigZagDecode(ReadVarULong());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteVector3(Godot.Vector3 v)
	{
		WriteFloat(v.X);
		WriteFloat(v.Y);
		WriteFloat(v.Z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Godot.Vector3 ReadVector3()
	{
		return new Godot.Vector3(ReadFloat(), ReadFloat(), ReadFloat());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteQuaternion(Godot.Quaternion q)
	{
		WriteFloat(q.X);
		WriteFloat(q.Y);
		WriteFloat(q.Z);
		WriteFloat(q.W);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Godot.Quaternion ReadQuaternion()
	{
		return new Godot.Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteNetworkId(NetworkId networkId)
	{
		WriteByte((byte)networkId.Type);
		WriteULong(networkId.Value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public NetworkId ReadNetworkId()
	{
		return new NetworkId((NetworkId.TypeEnum)ReadByte(), ReadULong());
	}


	// ---- Доступ к результату ----

	/// <summary>Валидные записанные данные (для отправки по сети).</summary>
	public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _readEnd);

	/// <summary>Валидные данные, ещё не прочитанные (от текущей позиции).</summary>
	public ReadOnlySpan<byte> RemainingSpan => _buffer.AsSpan(_position, _readEnd - _position);

	// ---- Dispose ----

	public void Dispose()
	{
		if (_buffer != null)
		{
			ArrayPool<byte>.Shared.Return(_buffer);
			_buffer = null!;
		}
	}
}
