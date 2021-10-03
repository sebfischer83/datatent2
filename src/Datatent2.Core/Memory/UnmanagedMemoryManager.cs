// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

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

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="span"></param>
        public UnmanagedMemoryManager(Span<T> span)
        {
            fixed (T* ptr = &MemoryMarshal.GetReference(span))
            {
                _pointer = ptr;
                _length = span.Length;
            }
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="length"></param>
        public UnmanagedMemoryManager(T* pointer, int length)
        {
            Guard.Argument(length).Min(0);
            _pointer = pointer;
            _length = length;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="length"></param>
        public UnmanagedMemoryManager(IntPtr pointer, int length) : this((T*)pointer.ToPointer(), length) { }


        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            
        }

        /// <inheritdoc />
        public override Span<T> GetSpan() => new Span<T>(_pointer, _length);

        /// <inheritdoc />
        public override MemoryHandle Pin(int elementIndex = 0)
        {
            Guard.Argument(elementIndex < 0 || elementIndex >= _length);
            return new MemoryHandle(_pointer + elementIndex);
        }

        /// <inheritdoc />
        public override void Unpin()
        {
            
        }
    }
}
