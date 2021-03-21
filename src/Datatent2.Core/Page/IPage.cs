using System;

namespace Datatent2.Core.Page
{
    internal interface IPage : IEquatable<BasePage?>, IDisposable
    {
        uint Id { get; }
        PageType Type { get; }
        bool IsDirty { get; set; }
        bool IsFull { get; }
        PageFillFactor FillFactor { get; }
        void SetPreviousPage(uint pageId);
        void SetNextPage(uint pageId);
        bool IsInsertPossible(ushort length);
        void Defrag();
        Span<byte> GetDataByIndex(byte directoryIndex);
        ushort GetMaxContiguounesFreeSpace();
        Span<byte> Insert(ushort length, out byte entryIndex);
        bool Delete(byte entryIndex);
    }
}