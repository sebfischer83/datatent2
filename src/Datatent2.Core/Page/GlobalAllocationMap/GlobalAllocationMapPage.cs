using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Dawn;

namespace Datatent2.Core.Page.GlobalAllocationMap
{
    /// <summary>
    /// 65024 pages
    /// </summary>
    internal class GlobalAllocationMapPage : BasePage
    {
        protected SpinLock SpinLock;
        public const int PAGES_PER_GAM = (Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE) * 8;

        protected long LastIssuedId = -1;

        public override bool IsFull
        {
            get
            {
                var lastItem = Buffer.Span[^1];
                return lastItem == byte.MaxValue;
            }
        }

        public GlobalAllocationMapPage(IBufferSegment buffer) : base(buffer)
        {
        }

        public GlobalAllocationMapPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.GlobalAllocationMap)
        {
        }

        public uint AcquirePageId()
        {
            bool isTaken = false;
            Guard.Argument(IsFull).False();

            try
            {
                SpinLock.Enter(ref isTaken);
                int localId = -1;
                if (LastIssuedId > -1 && LastIssuedId < PAGES_PER_GAM)
                {
                    // that page has already issued an id, so we can take the next on
                    localId = (int) LastIssuedId + 1;
                }
                else
                {
                    localId = FindLocalEmptyPageId();
                }
                if (localId == -1)
                    throw new Exception();

                // first GAM page is always at id 1, so the next always follows after 65024 pages
                // so next id is 65026, 130050, 195074, ... when the page size is 8192

                var newId = (uint)localId + Id;

                MarkPageAsAllocated(localId);

                IsDirty = true;
                LastIssuedId = localId;

                return newId;
            }
            finally
            {
                SpinLock.Exit();
            }
        }

        public void RemovePageFromGam(uint id)
        {
            // TODO: implement
        }

        protected void MarkPageAsAllocated(int localId)
        {
            var dataBuffer = Buffer.Span.Slice(Constants.PAGE_HEADER_SIZE);
            if (localId < 9)
            {
                ref byte b = ref dataBuffer[0];
                b = (byte)(b | (1 << localId - 1));
                return;
            }

            var bytePos = Math.DivRem(localId, 8, out var remainder);
            if (remainder > 0)
            {
                ref byte b2 = ref dataBuffer[bytePos];
                b2 = (byte)(b2 | (1 << remainder - 1));
            }
            else
            {
                // when remainder == 0, we need to set the last bit of the byte before
                ref byte b2 = ref dataBuffer[bytePos - 1];
                b2 = (byte)(b2 | (1 << 7));
            }
        }

        public int FindLocalEmptyPageId()
        {
            Span<ulong> span = MemoryMarshal.Cast<byte, ulong>(Buffer.Span.Slice(Constants.PAGE_HEADER_SIZE));
            int min = 0;
            int max = span.Length - 1;
            int index = -1;

            while (min <= max)
            {
                int mid = mid = (int)unchecked((uint)(min + max) >> 1);
                ref var b = ref span[mid];

                if (b != ulong.MaxValue)
                {
                    if (mid == 0)
                    {
                        index = 0;
                        break;
                    }

                    ref var b1 = ref span[mid - 1];
                    if (b1 == ulong.MaxValue)
                    {
                        index = mid;
                        break;
                    }

                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            if (index > -1)
            {
                int res = 0;
                ref var l = ref span[index];
                var count = BitOperations.LeadingZeroCount((ulong)l);
                res = (64 - count) + 1;
                if (index > 0 && res != -1)
                    res += (64 * (index));
                return res;
            }

            return index;
        }
    }
}
