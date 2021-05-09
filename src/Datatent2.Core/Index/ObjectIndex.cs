using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Page;

namespace Datatent2.Core.Index
{
    /// <summary>
    /// Meta data of the index
    /// </summary>
    internal class ObjectIndex
    {
        private string _field = string.Empty;
        private string _o = string.Empty;
        private string _name = string.Empty;
        public IndexType Type { get; set; }
        public IndexUsage Usage { get; set; }

        public PageAddress Head { get; set; }
        public PageAddress Tail { get; set; }

        public string Field
        {
            get => _field;
            set => _field = value;
        }

        public string Object
        {
            get => _o;
            set => _o = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public uint CalculateLength()
        {
            uint length = 0;
            length += 1; // indextype
            length += 1; // index usage
            length += Constants.PAGE_ADDRESS_SIZE * 2; // Head + Tail

            return length;
        }
    }

    internal enum IndexType : byte
    {
        Heap = 1,
        SkipList = 2
    }

    internal enum IndexUsage : byte
    {
        Primary = 1,
        Unique = 2,
        Search = 3
    }
}
