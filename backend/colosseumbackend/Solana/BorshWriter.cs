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
