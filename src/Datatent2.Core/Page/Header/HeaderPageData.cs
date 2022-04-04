// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Datatent2.Contracts;
using System.Runtime.InteropServices;

namespace Datatent2.Core.Page.Header
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_SPECIFIC_HEADER_SIZE)]
    internal readonly struct HeaderPageData
    {
        [FieldOffset(HEADER_VERSION_NUMBER)]
        public readonly ushort Version;

        [FieldOffset(HEADER_CREATION_TIME)]
        public readonly long CreationTime;
        
        private const int HEADER_VERSION_NUMBER = 0; // 0 ushort

        private const int HEADER_CREATION_TIME = 2; // 2-9 long

        public HeaderPageData(ushort databaseVersionNumber, long creationTime)
        {
            Version = databaseVersionNumber;
            CreationTime = creationTime;
        }

        public HeaderPageData(in HeaderPageData old)
        {
            Version = old.Version;
            CreationTime = old.CreationTime;
        }
    }
}
