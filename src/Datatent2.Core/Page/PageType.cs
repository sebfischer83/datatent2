// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

namespace Datatent2.Core.Page
{
    internal enum PageType : byte
    {
        Undefined = 0,
        Header = 1,
        Data = 2,
        Index = 3,
        GlobalAllocationMap = 4,
        AllocationInformation = 5,
        Table = 6,
        Overflow = 7,
        FreePageList = 8,
        Free = 255
    }

    internal enum PageFillFactor : byte
    {
        Zero = 0,
        ZeroToFifty = 1,
        FiftyToSeventy = 2,
        SeventyToNinetyFive = 3,
        NinetyFiveToNinetyNine = 4,
        Full = 5
    }
}