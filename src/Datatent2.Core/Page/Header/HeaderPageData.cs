// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System.Runtime.InteropServices;

namespace Datatent2.Core.Page.Header
{
    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct HeaderPageData
    {
        [FieldOffset(HEADER_VERSION_NUMBER)]
        public readonly byte Version;

        [FieldOffset(HEADER_CREATION_TIME)]
        public readonly long CreationTime;
        
        private const int HEADER_VERSION_NUMBER = 0; // 0 byte
     
        private const int HEADER_CREATION_TIME = 1; // 1-8 long

        public HeaderPageData(in HeaderPageData old, uint highestPageId)
        {
            Version = old.Version;
            CreationTime = old.CreationTime;
        }
    }
}
