using System;
using System.Runtime.CompilerServices;

namespace ZCraft.Source.Networking
{
    public class NetworkStream
    {
        private byte[] _buffer;
        private int _position;
        private int _length;
        private readonly int _capacity;

        public NetworkStream(int capacity = 4096)
        {
            _capacity = capacity;
            _buffer = new byte[capacity];
            _position = 0;
            _length = 0;
        }

        public NetworkStream() { }
        public NetworkStream(byte[] buffer)
        {
            _buffer = buffer;
            Reset();
        }

        public int Length => _length;
        public int Position => _position;
        public int Remaining => _length - _position;
        public int Capacity => _capacity;
        public ReadOnlySpan<byte> Buffer => _buffer.AsSpan(0, _length);
        public Span<byte> WritableBuffer => _buffer.AsSpan(_position, _capacity - _position);

        // ============ MANAGING ============
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _position = 0;
            _length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLength(int length)
        {
            if (length > _capacity) throw new InvalidOperationException("Buffer overflow");
            _length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int position)
        {
            if (position > _length) throw new InvalidOperationException("Position out of range");
            _position = position;
        }

        public void SetBuffer(byte[] buffer)
        {
            _buffer = buffer;
            _length = _buffer.Length;
        }

        // ============ WRITING ============
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            EnsureCapacity(1);
            _buffer[_position++] = value;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value)
        {
            EnsureCapacity(4);
            Unsafe.WriteUnaligned(ref _buffer[_position], value);
            _position += 4;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint value)
        {
            EnsureCapacity(4);
            Unsafe.WriteUnaligned(ref _buffer[_position], value);
            _position += 4;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short value)
        {
            EnsureCapacity(2);
            Unsafe.WriteUnaligned(ref _buffer[_position], value);
            _position += 2;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16(ushort value)
        {
            EnsureCapacity(2);
            Unsafe.WriteUnaligned(ref _buffer[_position], value);
            _position += 2;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(long value)
        {
            EnsureCapacity(8);
            Unsafe.WriteUnaligned(ref _buffer[_position], value);
            _position += 8;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64(ulong value)
        {
            EnsureCapacity(8);
            Unsafe.WriteUnaligned(ref _buffer[_position], value);
            _position += 8;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSingle(float value)
        {
            EnsureCapacity(4);
            Unsafe.WriteUnaligned(ref _buffer[_position], value);
            _position += 4;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double value)
        {
            EnsureCapacity(8);
            Unsafe.WriteUnaligned(ref _buffer[_position], value);
            _position += 8;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            EnsureCapacity(data.Length);
            data.CopyTo(_buffer.AsSpan(_position));
            _position += data.Length;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }

            int byteCount = System.Text.Encoding.UTF8.GetByteCount(value);
            WriteInt32(byteCount);
            EnsureCapacity(byteCount);
            System.Text.Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _position);
            _position += byteCount;
            _length = Math.Max(_length, _position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVarInt(int value)
        {
            uint v = (uint)value;
            while (v >= 0x80)
            {
                WriteByte((byte)(v | 0x80));
                v >>= 7;
            }
            WriteByte((byte)v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int size)
        {
            if (_position + size > _capacity)
            {
                throw new InvalidOperationException($"Buffer overflow: need {_position + size}, capacity {_capacity}");
            }
        }

        // ============ READING ============
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (_position >= _length) throw new InvalidOperationException("End of stream");
            return _buffer[_position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            if (_position + 4 > _length) throw new InvalidOperationException("End of stream");
            int value = Unsafe.ReadUnaligned<int>(ref _buffer[_position]);
            _position += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            if (_position + 4 > _length) throw new InvalidOperationException("End of stream");
            uint value = Unsafe.ReadUnaligned<uint>(ref _buffer[_position]);
            _position += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            if (_position + 2 > _length) throw new InvalidOperationException("End of stream");
            short value = Unsafe.ReadUnaligned<short>(ref _buffer[_position]);
            _position += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            if (_position + 2 > _length) throw new InvalidOperationException("End of stream");
            ushort value = Unsafe.ReadUnaligned<ushort>(ref _buffer[_position]);
            _position += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            if (_position + 8 > _length) throw new InvalidOperationException("End of stream");
            long value = Unsafe.ReadUnaligned<long>(ref _buffer[_position]);
            _position += 8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            if (_position + 8 > _length) throw new InvalidOperationException("End of stream");
            ulong value = Unsafe.ReadUnaligned<ulong>(ref _buffer[_position]);
            _position += 8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            if (_position + 4 > _length) throw new InvalidOperationException("End of stream");
            float value = Unsafe.ReadUnaligned<float>(ref _buffer[_position]);
            _position += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            if (_position + 8 > _length) throw new InvalidOperationException("End of stream");
            double value = Unsafe.ReadUnaligned<double>(ref _buffer[_position]);
            _position += 8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadBytes(int count)
        {
            if (_position + count > _length) throw new InvalidOperationException("End of stream");
            var result = _buffer.AsSpan(_position, count);
            _position += count;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString()
        {
            int length = ReadInt32();
            if (length < 0) return null;
            if (length == 0) return string.Empty;

            if (_position + length > _length) throw new InvalidOperationException("End of stream");
            string value = System.Text.Encoding.UTF8.GetString(_buffer, _position, length);
            _position += length;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadVarInt()
        {
            int result = 0;
            int shift = 0;
            byte b;

            do
            {
                b = ReadByte();
                result |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            return result;
        }

        // ============ КОПИРОВАНИЕ ДАННЫХ ============
        public byte[] ToArray()
        {
            return _buffer.AsSpan(0, _length).ToArray(); 
        }

        public Span<byte> AsSpan()
        {
            return _buffer.AsSpan(0, _length);
        }

        public ReadOnlySpan<byte> AsReadOnlySpan()
        {
            return _buffer.AsSpan(0, _length);
        }
    }
}