using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Page;

namespace Datatent2.Core.Algo.SkipList
{
    /// <summary>
    /// Represents the node of the skip list.
    /// </summary>
    internal readonly struct SkipListNode<TKey>
    {
        /// <summary>
        /// The key of the node.
        /// </summary>
        internal readonly TKey Key;

        internal readonly PageAddress PageAddress;

        private readonly SkipListNode<PageAddress>[] forward;

        private readonly int _dataLength;

        /// <summary>
        /// TypeCode of TKey, can expressed as a byte
        /// </summary>
        private readonly TypeCode _typeCode;

        private readonly byte[] _keyBytes;

        public SkipListNode(TKey key, PageAddress pageAddress, int level)
        {
            Key = key;
            PageAddress = pageAddress;
            forward = new SkipListNode<PageAddress>[level];
            
            switch (key)
            {
                case string s:
                    _typeCode = TypeCode.String;
                    _keyBytes = Encoding.UTF8.GetBytes(s);
                    _dataLength = _keyBytes.Length;
                    break;
                case sbyte sb:
                   
                    break;
                case byte bt:
                    
                    break;
                case short st:
                   
                    break;
                case int it:
                    
                    break;
                case long la:
                   
                    break;
                case ushort us:
                    
                    break;
                case uint ut:
                    
                    break;
                case ulong ul:
                   
                    break;
                case Guid g:
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key));
            }
        }
    }
}
