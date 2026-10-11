using System.Buffers.Binary;
using Solnet.Wallet;

namespace colosseumbackend.Solana;

/// <summary>Minimal Borsh encoder for Anchor instruction arguments (little-endian, fixed-size fields).</summary>
public class BorshWriter
{
    private readonly List<byte> _bytes = [];

    public BorshWriter Bytes(ReadOnlySpan<byte> value)
    {
        _bytes.AddRange(value.ToArray());
        return this;
    }

    public BorshWriter U16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        return Bytes(buffer);
    }

    public BorshWriter U64(ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        return Bytes(buffer);
    }

    public BorshWriter I64(long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        return Bytes(buffer);
    }

    public BorshWriter Key(PublicKey value)
    {
        return Bytes(value.KeyBytes);
    }

    public byte[] ToArray()
    {
        return _bytes.ToArray();
    }
}

/// <summary>Sequential Borsh decoder for Anchor account data.</summary>
public ref struct BorshReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int                         _offset;

    public BorshReader(ReadOnlySpan<byte> data, int offset = 0)
    {
        _data   = data;
        _offset = offset;
    }

    public byte U8()
    {
        return _data[_offset++];
    }

    public bool Bool()
    {
        return U8() != 0;
    }

    public ushort U16()
    {
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_data[_offset..]);
        _offset += sizeof(ushort);
        return value;
    }

    public uint U32()
    {
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(_data[_offset..]);
        _offset += sizeof(uint);
        return value;
    }

    public ulong U64()
    {
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(_data[_offset..]);
        _offset += sizeof(ulong);
        return value;
    }

    public long I64()
    {
        long value = BinaryPrimitives.ReadInt64LittleEndian(_data[_offset..]);
        _offset += sizeof(long);
        return value;
    }

    public PublicKey Key()
    {
        var value = new PublicKey(_data.Slice(_offset, PublicKeyLength));
        _offset += PublicKeyLength;
        return value;
    }

    private const int PublicKeyLength = 32;
}
