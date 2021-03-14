using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dawn;

namespace Datatent2.Core.Memory
{
    /// <summary>
    /// A MemoryManager that can manage an pointer to unmanaged memory
    /// </summary>
    /// <remarks>
    /// Credits: https://stackoverflow.com/questions/52190423/c-sharp-access-unmanaged-array-using-memoryt-or-arraysegmentt
    /// https://github.com/mgravell/Pipelines.Sockets.Unofficial/blob/master/src/Pipelines.Sockets.Unofficial/UnsafeMemory.cs
    /// </remarks>
    public sealed unsafe class UnmanagedMemoryManager<T> : MemoryManager<T>
        where T : unmanaged
    {
        private readonly T* _pointer;
        private readonly int _length;

        public UnmanagedMemoryManager(Span<T> span)
        {
            fixed (T* ptr = &MemoryMarshal.GetReference(span))
            {
                _pointer = ptr;
                _length = span.Length;
            }
        }

        public UnmanagedMemoryManager(T* pointer, int length)
        {
            Guard.Argument(length).Min(0);
            _pointer = pointer;
            _length = length;
        }

        public UnmanagedMemoryManager(IntPtr pointer, int length) : this((T*)pointer.ToPointer(), length) { }

        protected override void Dispose(bool disposing)
        {
            
        }

        public override Span<T> GetSpan() => new Span<T>(_pointer, _length);

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            Guard.Argument(elementIndex < 0 || elementIndex >= _length);
            return new MemoryHandle(_pointer + elementIndex);
        }

        public override void Unpin()
        {
            
        }
    }
}
