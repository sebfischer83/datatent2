using System;
using System.Runtime.InteropServices;
using Dawn;

namespace Datatent2.Core.Page.AllocationInformation
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_SPECIFIC_HEADER_SIZE)]
    internal readonly struct AllocationInformationPageHeader
    {
        [FieldOffset(AIM_IN_GAM)]
        public readonly ushort AimInGam;
        [FieldOffset(GAM_IN_FILE)]
        public readonly uint GamInFile;
        
        private const int AIM_IN_GAM = 0; // ushort 0-1
        private const int GAM_IN_FILE = 0; // uint 2-5

        public AllocationInformationPageHeader(ushort aimInGam, uint gamInFile)
        {
            AimInGam = aimInGam;
            GamInFile = gamInFile;
        }

        public static AllocationInformationPageHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<AllocationInformationPageHeader>(span.Slice(Constants.PAGE_COMMON_HEADER_SIZE));
        }

        public static AllocationInformationPageHeader FromBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            return FromBuffer(span.Slice(offset + Constants.PAGE_COMMON_HEADER_SIZE));
        }

        public void ToBuffer(Span<byte> span)
        {
            Guard.Argument(span.Length).Min(Constants.PAGE_SPECIFIC_HEADER_SIZE);
            AllocationInformationPageHeader a = this;
            MemoryMarshal.Write(span.Slice(Constants.PAGE_COMMON_HEADER_SIZE), ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            ToBuffer(span.Slice(offset + Constants.PAGE_COMMON_HEADER_SIZE));
        }
    }
}