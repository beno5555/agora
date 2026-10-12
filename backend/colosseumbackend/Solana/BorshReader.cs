using System.Buffers.Binary;
using Solnet.Wallet;

namespace colosseumbackend.Solana;

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
