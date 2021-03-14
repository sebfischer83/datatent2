using System;
using System.Buffers;

namespace Datatent2.Core.Memory
{
    internal interface IBufferSegment : IMemoryOwner<byte>
    {
        public void Clear();

        public uint Length { get; }

        public Span<byte> Span { get; }
    }
}