// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Runtime.InteropServices;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Dawn;

namespace Datatent2.Core.Page.Header
{
    internal class HeaderPage : BasePage
    {
        private static HeaderPage? _instance = null;

        public static HeaderPage Instance => _instance!;

        public override ushort FreeContinuousBytes => Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE;

        public override ushort MaxFreeUsableBytes => FreeContinuousBytes;

        public override bool IsFull => false;

        private HeaderPageData _headerPageData;

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
        }

        internal override void SaveHeader()
        {
            base.SaveHeader();
            MemoryMarshal.Write(Buffer.Span, ref _headerPageData);
        }

        public static HeaderPage LoadHeaderPage(IBufferSegment bufferSegment)
        {
            _instance = new HeaderPage(bufferSegment);
            return Instance;
        }

        private void SetHeaderData()
        {
            var version = typeof(HeaderPage).Assembly.GetName().Version;
            if (version == null)
            {
                throw new InvalidOperationException(nameof(version));
            }
            var versions = version.GetMajorMinor();
            
            _headerPageData = new HeaderPageData(versions, DateTime.UtcNow.Ticks);
            SaveHeader();
        }

        public void InsertOrUpdateBlock(byte[] content)
        {
            if (content.Length > FreeContinuousBytes)
                throw new ArgumentException(nameof(content));
            var span = Buffer.Span;
            span.WriteBytes(Constants.PAGE_HEADER_SIZE, content);
        }

        public static HeaderPage CreateHeaderPage(IBufferSegment bufferSegment)
        {
            _instance =
                new HeaderPage(bufferSegment, 0, PageType.Header);
            _instance.SetHeaderData();

            return Instance;
        }
    }
}
