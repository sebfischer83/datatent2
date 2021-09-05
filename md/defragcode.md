<div style="font-size:14px">

```cs 
public virtual void Defrag()
{
     // nothing to do
     if (Header.UnalignedFreeBytes == 0)
         return;

     var regions = GetFreeRegions();
     var freeRegions = regions.Count;
     // maximum number of loops to is the initial number of regions with free space
     int maxLoops = regions.Count;

     while (freeRegions > 0 && maxLoops > 0)
     {
         // take the first free region
         var region = regions[0];

         // last free region in the file
         byte nextFreeEntry = HighestDirectoryEntryId;
         if (regions.Count > 1)
         {
             // next free entry starts here, so all between these indexes need to be moved
             var nextRegion = regions[1];
             nextFreeEntry = nextRegion.Item1;
         }

         var between = GetAllDirectoryEntriesBetween(region.Item2, nextFreeEntry);
         var startOffset = between[0].Entry.DataOffset;
         var endOffset = between[^1].Entry.EndPositionOfData();

         var entry = SlotEntry.FromBuffer(Buffer.Span,
             SlotEntry.GetEntryPosition(region.Item1));
         var spanTarget = Buffer.Span.Slice(entry.EndPositionOfData());
         var spanSource = Buffer.Span.Slice(startOffset, endOffset - startOffset);
         // copy all bytes
         spanTarget.WriteBytes(0, spanSource);
         Buffer.Span.Slice(endOffset - region.Item3, region.Item3).Clear();
         for (int i = 0; i < between.Count; i++)
         {
             var entryToChange = SlotEntry.FromBuffer(Buffer.Span, between[i].Index);
             entryToChange = new SlotEntry((ushort) (entryToChange.DataOffset - region.Item3),
                 entryToChange.DataLength);
             entryToChange.ToBuffer(Buffer.Span, SlotEntry.GetEntryPosition(between[i].Index));
         }


         var pageHeader = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
             (ushort)(Header.UsedBytes),
             (byte) (Header.ItemCount - 1),
             Header.NextFreePosition,
             (ushort) (regions.Sum(tuple => tuple.Item3) -  region.Item3),
             HighestDirectoryEntryId);
         pageHeader.ToBuffer(Buffer.Span, 0);
         Header = pageHeader;

         regions = GetFreeRegions();
         freeRegions = regions.Count;
         maxLoops -= 1;
     }

     IsDirty = true;
}
```

</div>