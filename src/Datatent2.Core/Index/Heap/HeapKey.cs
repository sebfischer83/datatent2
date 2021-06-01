// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Datatent2.Contracts;
using Datatent2.Core.Page;

namespace Datatent2.Core.Index.Heap
{
    // doesn't work with struct, because alignment of the bytes maybe split in 2 structs?
    internal record HeapKey : IEquatable<HeapKey>
    {
        public const int MAX_STRING_KEY_LENGTH = 32;

        public readonly HeapKeyType Type;

        public readonly PageAddress PageAddress;

        public readonly byte DataLength;

        public byte[] ValueBytes { get; init; }

        public Guid GuidValue => new Guid(ValueBytes);

        public ulong UnsignedNumericalValue => BitConverter.ToUInt64(ValueBytes);

        public long NumericalValue => BitConverter.ToInt64(ValueBytes);

        public string StringValue => Encoding.UTF8.GetString(ValueBytes);

        private const int TYPE = 0; // byte index type
        private const int PAGE_ADDRESS = 1; // 1-8

        private const int DATA_LENGTH = 9; // byte
        private const int DATA_CONTENT = 10; // byte

        public int Length => 1 + Constants.PAGE_ADDRESS_SIZE + 1 + DataLength;

        public HeapKey(PageAddress pageAddress, string value)
        {
            Type = HeapKeyType.String;
            PageAddress = pageAddress;
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > MAX_STRING_KEY_LENGTH)
                throw new ArgumentOutOfRangeException(nameof(value));
            DataLength = (byte)bytes.Length;
            ValueBytes = bytes;
        }

        public HeapKey(PageAddress pageAddress, long value)
        {
            Type = HeapKeyType.Numeric;
            PageAddress = pageAddress;
            DataLength = 8;
            ValueBytes = BitConverter.GetBytes(value);
        }

        public HeapKey(PageAddress pageAddress, ulong value)
        {
            Type = HeapKeyType.UnsignedNumeric;
            PageAddress = pageAddress;
            DataLength = 8;
            ValueBytes = BitConverter.GetBytes(value);
        }

        public HeapKey(PageAddress pageAddress, Guid value)
        {
            Type = HeapKeyType.Guid;
            PageAddress = pageAddress;
            DataLength = 16;
            ValueBytes = value.ToByteArray();
        }

        private HeapKey(PageAddress pageAddress, byte dataLength, HeapKeyType type, byte[] valueBytes)
        {
            Type = type;
            PageAddress = pageAddress;
            DataLength = dataLength;
            ValueBytes = valueBytes;
        }

        public void Write(Span<byte> span, int offset = 0)
        {
            var start = span[offset..];
            start.WriteByte(TYPE, (byte)Type);
            PageAddress.ToBuffer(start, PAGE_ADDRESS);
            start.WriteByte(DATA_LENGTH, DataLength);
            start.WriteBytes(DATA_CONTENT, ValueBytes);
        }

        public static HeapKey? Read(Span<byte> span, int offset = 0)
        {
            var start = span[offset..];

            if (start[0] == 0)
                return null;

            var type = (HeapKeyType) start.ReadByte(TYPE);
            var pageAddress = Page.PageAddress.FromBuffer(start, PAGE_ADDRESS);
            var dataLength = start.ReadByte(DATA_LENGTH);
            var data = start.ReadBytes(DATA_CONTENT, dataLength);
            var key = new HeapKey(pageAddress, dataLength, type, data);

            return key;
        }

        public static IList<HeapKey> ReadAllKeys(Span<byte> span)
        {
            List<HeapKey> heapKeys = new List<HeapKey>();
            int offset = 0;
            var s = span;
            while (true)
            {
                var key = Read(s, offset);
                if (key != null)
                    heapKeys.Add(key);
                else
                    break;
                offset += key.Length;
            }

            return heapKeys;
        }


        public virtual bool Equals(HeapKey? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type && PageAddress.Equals(other.PageAddress) && DataLength == other.DataLength;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) Type, PageAddress, DataLength);
        }
    }
}