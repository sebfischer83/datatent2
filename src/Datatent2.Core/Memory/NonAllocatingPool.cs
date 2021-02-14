using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Memory
{
    public class NonAllocatingPool<T> : MemoryPool<T>
    {
        public override int MaxBufferSize => Int32.MaxValue;

        public new static Impl Shared { get; } = new NonAllocatingPool<T>.Impl();

        protected override void Dispose(bool disposing) { }

        public override IMemoryOwner<T> Rent(int minBufferSize) => RentCore(minBufferSize);

        private Rental RentCore(int minBufferSize) => new Rental(minBufferSize);

        public sealed class Impl : NonAllocatingPool<T>
        {
            public new Rental Rent(int minBufferSize) => RentCore(minBufferSize);
        }

        // Struct implements the interface so it can be boxed if necessary.
        public struct Rental : IMemoryOwner<T>
        {
            private T[] _rental;

            public Rental(int minBufferSize)
            {
                _rental = ArrayPool<T>.Shared.Rent(minBufferSize);
            }

            public Memory<T> Memory
            {
                get
                {
                    if (_rental == null)
                        throw new ObjectDisposedException(nameof(Rental));

                    return new Memory<T>(_rental);
                }
            }

            public void Dispose()
            {
                if (_rental != null)
                {
                    ArrayPool<T>.Shared.Return(_rental);
                    _rental = null;
                }
            }
        }
    }
}
