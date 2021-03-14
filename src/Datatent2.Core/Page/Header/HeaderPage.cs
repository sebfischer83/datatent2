using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Dawn;

namespace Datatent2.Core.Page
{
    internal class HeaderPage : BasePage
    {
        private static HeaderPage? _instance = null;

        public static HeaderPage Instance => _instance!;

        private HeaderPageData _headerPageData;

        private uint _pageIdSequence;

        public uint HighestPageId => _headerPageData.HighestPageId;

        protected HeaderPage(IBufferSegment buffer) : base(buffer)
        {
            Guard.Argument(Header.PageId).Zero();
            Guard.Argument(Header.Type).Equal(PageType.Header);
            LoadData();
        }

        protected HeaderPage(IBufferSegment buffer, uint id, PageType pageType) : base(buffer, id, pageType)
        {
            _headerPageData = new HeaderPageData();
        }

        private void LoadData()
        {
            _headerPageData = MemoryMarshal.Read<HeaderPageData>(Buffer.Span);
            _pageIdSequence = HighestPageId;
        }

        private void SaveData()
        {
            MemoryMarshal.Write(Buffer.Span, ref _headerPageData);
        }

        public uint GetNextPageId()
        {
            _pageIdSequence++;
            var id = _pageIdSequence;
            return id;
        }

        public void SetHighestPageId(uint id)
        {
            if (id <= _headerPageData.HighestPageId)
                return;
            _headerPageData = new HeaderPageData(_headerPageData, id);
            SaveData();
        }
        
        public static HeaderPage LoadHeaderPage(IBufferSegment bufferSegment)
        {
            _instance = new HeaderPage(bufferSegment);
            return Instance;
        }

        public static HeaderPage CreateHeaderPage(IBufferSegment bufferSegment)
        {
            _instance =
                new HeaderPage(bufferSegment, 0, PageType.Header);
            return Instance;
        }
    }
}
