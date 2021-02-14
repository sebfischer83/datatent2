//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace Datatent2.Core.Memory
//{
//    public unsafe class UnmanagedMemoryPool : MemoryPool<byte>
//    {
//        public override int MaxBufferSize => Int32.MaxValue;

//        public new static Impl Shared { get; } = new UnmanagedMemoryPool.Impl();

//        protected override void Dispose(bool disposing)
//        {
//            Shared.Release();
//        }

//        public override IMemoryOwner<byte> Rent(int minBufferSize) => RentCore(minBufferSize);

//        private Rental RentCore(int minBufferSize) => Shared.Rent(minBufferSize);

//        public sealed class Impl : UnmanagedMemoryPool
//        {
//            private int _length;
//            private IntPtr _start;
//            private Dictionary<int, int> _rentedObjects = new Dictionary<int, int>();

//            public Impl()
//            {
//                _length = 1024 * 1024 * 1024;
//                _start = Marshal.AllocHGlobal(_length);
//            }

//            public void Release()
//            {
//                Marshal.FreeHGlobal(_start);
//            }

//            public new Rental Rent(int minBufferSize)
//            {
//                var span = new Span<byte>((_start + minBufferSize).ToPointer(), minBufferSize);
//                return new Rental(span);
//            }
//        }

//        // Struct implements the interface so it can be boxed if necessary.
//        public class Rental : IMemoryOwner<byte>
//        {
//            private Span<byte> _rental;

//            public Rental(Span<byte> span)
//            {
//                _rental = span;
//                MemoryMarshal.
//            }

//            public Memory<byte> Memory
//            {
//                get
//                {
//                    if (_rental == null)
//                        throw new ObjectDisposedException(nameof(Rental));
//                    new Memory<byte>
//                }
//            }

//            public void Dispose()
//            {
//                if (_rental != null)
//                {
//                    _rental = null;
//                }
//            }
//        }
//    }
//}
