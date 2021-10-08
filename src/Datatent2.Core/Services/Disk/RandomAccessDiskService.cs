using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace Datatent2.Core.Services.Disk
{
    internal class RandomAccessDiskService : DiskService
    {
        private SafeFileHandle _safeFileHandle;

        public RandomAccessDiskService(DatatentSettings settings, ILogger logger) : base(settings, logger)
        {
            _safeFileHandle = File.OpenHandle(settings.DatabasePath!, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.Read, FileOptions.RandomAccess,
                Constants.PAGE_SIZE * 50);
            
        }

        protected override IBufferSegment ReadPageBuffer(uint pageId)
        {
            var bufferSegment = BufferPoolFactory.Get().Rent(Constants.PAGE_SIZE);
            RandomAccess.Read(_safeFileHandle, bufferSegment.Span, BasePage.PageOffset(pageId));

            return bufferSegment;
        }

        protected override void WritePageBuffer(uint pageId, IBufferSegment bufferSegment)
        {
            RandomAccess.Write(_safeFileHandle, bufferSegment.Span, BasePage.PageOffset(pageId));
        }

        public override void Dispose()
        {
            base.Dispose();
            _safeFileHandle.Dispose();
        }
    }
}
