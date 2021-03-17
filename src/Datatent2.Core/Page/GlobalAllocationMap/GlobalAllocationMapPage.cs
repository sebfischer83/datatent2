using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
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

        protected uint PagesPerGam = 0;
        protected long LastIssuedId = -1;

        public override bool IsFull
        {
            get
            {
                Span<ulong> span = MemoryMarshal.Cast<byte, ulong>(Buffer.Span);
                var lastItem = span[^1];
                return lastItem == ulong.MaxValue;
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
                if (LastIssuedId > -1 && LastIssuedId < PagesPerGam)
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
                // so next id is 65025, 130049, 195073, ... when the page size is 8192

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
            Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(Buffer.Span.Slice(Constants.PAGE_HEADER_SIZE));

            var lastItem = longSpan[^1];
            if (lastItem == ulong.MaxValue)
            {
                return -1;
            }

            int iterCount = longSpan.Length;
            for (int i = 0; i < iterCount; i += 4)
            {
                ref ulong l4 = ref longSpan[i + 3];
                // when l4 is max value all others before too
                if (l4 == ulong.MaxValue)
                    continue;

                ref ulong l1 = ref longSpan[i];
                ref ulong l2 = ref longSpan[i + 1];
                ref ulong l3 = ref longSpan[i + 2];

                int mult = i + 1;
                int res = -1;
                if (l1 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l1);

                    res = (64 - count) + 1;
                }
                else if (l2 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l2);
                    res = (64) - count + 64 + 1;
                }
                else if (l3 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l3);
                    res = (64) - count + 128 + 1;
                }
                else if (l4 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l4);
                    res = (64) - count + 192 + 1;
                }

                if (i > 0 && res != -1)
                    res += (64 * i);

                return res;
            }

            return -1;
        }
    }
}
