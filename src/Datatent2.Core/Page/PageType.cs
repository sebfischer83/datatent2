namespace Datatent2.Core.Page
{
    internal enum PageType : byte
    {
        Empty = 0,
        Header = 1,
        Data = 2,
        Index = 3,
        GlobalAllocationMap = 4,
        AllocationInformation = 5
    }

    internal enum PageFillFactor : byte
    {
        Zero = 0,
        ZeroToFifty = 1,
        FiftyToSeventy = 2,
        SeventyToNinetyNine = 3,
        NinetyNineToFull = 4
    }
}