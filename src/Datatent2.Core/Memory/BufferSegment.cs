using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Memory
{
    internal class BufferSegment : IMemoryOwner<byte>
    {
        private byte[]? _rental;

        public BufferSegment(int minBufferSize)
        {
            _rental = ArrayPool<byte>.Shared.Rent(minBufferSize);
        }

        public Span<byte> Span
        {
            get
            {
                if (_rental == null)
                    throw new ObjectDisposedException(nameof(BufferSegment));
                return new Span<byte>(_rental);
            }
        }

        public Memory<byte> Memory
        {
            get
            {
                if (_rental == null)
                    throw new ObjectDisposedException(nameof(BufferSegment));

                return new Memory<byte>(_rental);
            }
        }

        public void Clear()
        {
            Span.Clear();
        }

        public void Dispose()
        {
            if (_rental != null)
                ArrayPool<byte>.Shared.Return(_rental);
            _rental = null;
        }
    }
}
