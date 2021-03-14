using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Page
{
    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct HeaderPageData
    {
        [FieldOffset(HEADER_VERSION_NUMBER)]
        public readonly byte Version;

        [FieldOffset(HEADER_CREATION_TIME)]
        public readonly long CreationTime;

        [FieldOffset(HEADER_HIGHEST_PAGE_ID)]
        public readonly uint HighestPageId;

        private const int HEADER_VERSION_NUMBER = 0; // 0 byte
     
        private const int HEADER_CREATION_TIME = 1; // 1-8 long

        private const int HEADER_HIGHEST_PAGE_ID = 9; // 9-12 uint

        public HeaderPageData(in HeaderPageData old, uint highestPageId)
        {
            Version = old.Version;
            CreationTime = old.CreationTime;
            HighestPageId = highestPageId;
        }
    }
}
