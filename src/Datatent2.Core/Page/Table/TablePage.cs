using Datatent2.Contracts;
using Datatent2.Core.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Page.Table
{
    /// <summary>
    /// Holds the information
    /// </summary>
    internal class TablePage : BasePage
    {
        public string Name { get; private set; }

        private readonly byte[] _nameBytes;
        private readonly byte _offsetAfterName;

        public override ushort MaxFreeUsableBytes => (ushort) (Constants.PAGE_SIZE - _offsetAfterName);

        public TablePage(IBufferSegment buffer) : base(buffer)
        {
            var bytes = buffer.Span.Slice(Constants.PAGE_HEADER_SIZE);
            var nameLength = bytes.ReadByte(0);
            _nameBytes = bytes.ReadBytes(1, nameLength);
            Name = Encoding.UTF8.GetString(_nameBytes);
            _offsetAfterName = (byte)(Constants.PAGE_HEADER_SIZE + 1 + _nameBytes.Length);
        }

        public TablePage(IBufferSegment buffer, uint id, string name) : base(buffer, id, PageType.Table)
        {
            if (name.Length > 25)
            {
                throw new ArgumentException($"{nameof(name)} should not exceed 25 characters.");
            }
            Name = name;
            _nameBytes = Encoding.UTF8.GetBytes(name);
            _offsetAfterName = (byte)(Constants.PAGE_HEADER_SIZE + 1 + _nameBytes.Length);
        }

        private void Load()
        {

        }

        private void Save()
        {

        }
    }
}
