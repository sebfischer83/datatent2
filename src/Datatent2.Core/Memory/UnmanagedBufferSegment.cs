using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Internal;

namespace Datatent2.Core.Memory
{
    internal class UnmanagedBufferSegment : IBufferSegment
    {
        public int Key { get; }
        private readonly UnmanagedBufferPool _pool;
        private bool _disposed;

        public unsafe byte* GetPointer()
        {
            return (byte*) _pool.GetPointerToSlot(Key);
        }

        public UnmanagedBufferSegment(Memory<byte> memory, int key, UnmanagedBufferPool pool)
        {
            Memory = memory;
            Key = key;
            _pool = pool;
        }

        public void Dispose()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnmanagedBufferSegment));

            _pool.Return(this);
            _disposed = true;
        }

        public Memory<byte> Memory { get; }

        public void Clear()
        {
            Memory.Span.Clear();
        }

        public uint Length => (uint) Memory.Length;

        public Span<byte> Span => Memory.Span;
    }
}
