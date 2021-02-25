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
    /// <summary>
    /// Buffer always a complete page, only dispose when not in use anymore
    /// </summary>
    internal class BufferSegment : IMemoryOwner<byte>
    {
        private byte[]? _rental;

        public uint Length => (uint)(_rental?.Length ?? 0);

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
                ArrayPool<byte>.Shared.Return(_rental, true);
            _rental = null;
        }
    }
}
