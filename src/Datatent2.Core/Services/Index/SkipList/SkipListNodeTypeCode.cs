// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

namespace Datatent2.Core.Services.Index.SkipList
{
    public enum SkipListNodeTypeCode : byte
    {
        Empty = 0,
        Start = 1,
        Guid = 2,
        Boolean = 3,
        Char = 4,
        SByte = 5,
        Byte = 6,
        Int16 = 7,
        UInt16 = 8,
        Int32 = 9,
        UInt32 = 10, // 0x0000000A
        Int64 = 11, // 0x0000000B
        UInt64 = 12, // 0x0000000C
        Single = 13, // 0x0000000D
        Double = 14, // 0x0000000E
        Decimal = 15, // 0x0000000F
        DateTime = 16, // 0x00000010
        String = 18, // 0x00000012
    }
}