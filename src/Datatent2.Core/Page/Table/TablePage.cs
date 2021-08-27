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

        private byte[] _nameBytes;
        private byte _offsetAfterName;

        private const int MAININDEXPAGEADDRESS = 0;

        public uint MainIndexPageAddress { get; set; }

        public override ushort MaxFreeUsableBytes => (ushort)(Constants.PAGE_SIZE - _offsetAfterName);

#pragma warning disable CS8618
        public TablePage(IBufferSegment buffer) : base(buffer) => Load();
#pragma warning restore CS8618

        public TablePage(IBufferSegment buffer, uint id, string name) : base(buffer, id, PageType.Table)
        {
            if (name.Length > 25)
            {
                throw new ArgumentException($"{nameof(name)} must not exceed 25 characters.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"{nameof(name)} must not be null or empty.");
            }
            Name = name;
            _nameBytes = Encoding.UTF8.GetBytes(name);
            _offsetAfterName = (byte)(Constants.PAGE_HEADER_SIZE + 1 + _nameBytes.Length);

            Header = new PageHeader(id, PageType.Table);

            Save();
        }

        // Definition of fields
        // 0 Length of name in bytes x
        // 1 - x Name as bytes

        private void Load()
        {
            var bytes = Buffer.Span[Constants.PAGE_HEADER_SIZE..];
            var nameLength = bytes.ReadByte(0);
            _nameBytes = bytes.ReadBytes(1, nameLength);
            Name = Encoding.UTF8.GetString(_nameBytes);
            _offsetAfterName = (byte)(Constants.PAGE_HEADER_SIZE + 1 + _nameBytes.Length);
            bytes = bytes[_offsetAfterName..];

            MainIndexPageAddress = bytes.ReadUInt32(MAININDEXPAGEADDRESS);
        }

        public void Save()
        {
            Header.ToBuffer(Buffer.Span);
            var bytes = Buffer.Span[Constants.PAGE_HEADER_SIZE..];
            bytes.WriteByte(0, (byte)_nameBytes.Length);
            bytes.WriteBytes(1, _nameBytes);

            bytes = bytes[_offsetAfterName..];
            bytes.WriteUInt32(MAININDEXPAGEADDRESS, MainIndexPageAddress);
        }
    }
}
