using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Datatent2.Core.Services.Disk
{ 
    internal abstract class DiskService
    {
        protected Stream? _stream;

        public BufferSegment GetPageBuffer(uint pageId)
        {
            var bufferSegment = BufferPool.Shared.Rent(Constants.PAGE_SIZE);
            _stream.Seek(BasePage.PageOffset(pageId), SeekOrigin.Begin);
            _stream.Read(bufferSegment.Span.Slice(0, Constants.PAGE_SIZE));

            return bufferSegment;
        }
    }
}
