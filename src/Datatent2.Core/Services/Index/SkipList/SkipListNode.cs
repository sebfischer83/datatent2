﻿using Datatent2.Contracts;
using Datatent2.Core.Page;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Datatent2.Core.Services.Index.SkipList
{
    /// <summary>
    /// Represents the node of the skip list.
    /// </summary>
    internal readonly struct SkipListNode// : IEquatable<SkipListNode>
    {
        /// <summary>
        /// The key of the node.
        /// </summary>
        internal readonly object Key;

        internal readonly PageAddress PageAddress;

        internal readonly PageAddress[] Forward;

        private readonly byte _dataLength;

        /// <summary>
        /// TypeCode of TKey, can expressed as a byte
        /// </summary>
        internal readonly SkipListNodeTypeCode TypeCode;

        private readonly byte[] _keyBytes;


        /// <summary>
        /// Initializes a new instance as the starting point for a new SkipListIndexService.
        /// </summary>
        /// <param name="level">The level.</param>
        public SkipListNode(int level)
        {
            TypeCode = SkipListNodeTypeCode.Start;
            _keyBytes = Array.Empty<byte>();
            _dataLength = 0;
            Key = default!;
            PageAddress = PageAddress.Empty;
            Forward = new PageAddress[level];
        }

        private SkipListNode(Span<byte> span)
        {
            int pos = 0;
            TypeCode = (SkipListNodeTypeCode)span.ReadByte(pos);
            pos += sizeof(byte);
            PageAddress = PageAddress.FromBuffer(span, pos);
            pos += Constants.PAGE_ADDRESS_SIZE;
            var lengthForward = span.ReadByte(pos);
            Forward = new PageAddress[lengthForward];
            pos += sizeof(byte);

            var a = MemoryMarshal.Cast<byte, PageAddress>(span.Slice(pos));

            for (int i = 0; i < lengthForward; i++)
            {
                Forward[i] = a[i];
                pos += Constants.PAGE_ADDRESS_SIZE;
            }

            _dataLength = span.ReadByte(pos);
            pos += sizeof(byte);
            _keyBytes = span.ReadBytes(pos, _dataLength);

            switch (TypeCode)
            {
                case SkipListNodeTypeCode.Start:
                    Key = default!;
                    break;
                case SkipListNodeTypeCode.Guid:
                    Key = BoxingSafeConverter<Guid, Guid>.Instance.Convert(new Guid(_keyBytes));
                    break;
                case SkipListNodeTypeCode.SByte:
                    Key = BoxingSafeConverter<sbyte, sbyte>.Instance.Convert(Unsafe.As<byte[], sbyte>(ref _keyBytes));
                    break;
                case SkipListNodeTypeCode.Byte:
                    Key = BoxingSafeConverter<byte, byte>.Instance.Convert(Unsafe.As<byte[], byte>(ref _keyBytes));
                    break;
                //case SkipListNodeTypeCode.Int16:
                //    break;
                //case SkipListNodeTypeCode.UInt16:
                //    break;
                case SkipListNodeTypeCode.Int32:
                    Key = BoxingSafeConverter<int, int>.Instance.Convert(MemoryMarshal.Read<int>(_keyBytes));
                    break;
                //case SkipListNodeTypeCode.UInt32:
                //    break;
                case SkipListNodeTypeCode.Int64:
                    Key = BoxingSafeConverter<long, long>.Instance.Convert(MemoryMarshal.Read<long>(_keyBytes));
                    //Key = (TKey)(object)Unsafe.As<byte[], long>(ref _keyBytes);
                    break;
                //case SkipListNodeTypeCode.UInt64:
                //    break;
                //case SkipListNodeTypeCode.Single:
                //    break;
                //case SkipListNodeTypeCode.Double:
                //    break;
                //case SkipListNodeTypeCode.Decimal:
                //    break;
                //case SkipListNodeTypeCode.DateTime:
                //    break;
                //case SkipListNodeTypeCode.String:
                //    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkipListNode{TKey}"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="pageAddress">The page address.</param>
        /// <param name="level">The level.</param>
        public SkipListNode(object key, PageAddress pageAddress, int level)
        {
            Key = key;
            PageAddress = pageAddress;
            Forward = new PageAddress[level];

            switch (key)
            {
                case sbyte sb:
                    TypeCode = SkipListNodeTypeCode.SByte;
                    _keyBytes = new byte[sizeof(sbyte)];
                    MemoryMarshal.Write(_keyBytes, ref sb);
                    break;
                case byte bt:
                    TypeCode = SkipListNodeTypeCode.Byte;
                    _keyBytes = new byte[sizeof(byte)];
                    MemoryMarshal.Write(_keyBytes, ref bt);
                    break;
                case short st:
                    TypeCode = SkipListNodeTypeCode.Int16;
                    _keyBytes = new byte[sizeof(short)];
                    MemoryMarshal.Write(_keyBytes, ref st);
                    break;
                case int it:
                    TypeCode = SkipListNodeTypeCode.Int32;
                    _keyBytes = new byte[sizeof(int)];
                    MemoryMarshal.Write(_keyBytes, ref it);
                    break;
                case long la:
                    TypeCode = SkipListNodeTypeCode.Int64;
                    _keyBytes = new byte[sizeof(long)];
                    MemoryMarshal.Write(_keyBytes, ref la);
                    break;
                case ushort us:
                    TypeCode = SkipListNodeTypeCode.UInt16;
                    _keyBytes = new byte[sizeof(ushort)];
                    MemoryMarshal.Write(_keyBytes, ref us);
                    break;
                case uint ut:
                    TypeCode = SkipListNodeTypeCode.UInt32;
                    _keyBytes = new byte[sizeof(uint)];
                    MemoryMarshal.Write(_keyBytes, ref ut);
                    break;
                case ulong ul:
                    TypeCode = SkipListNodeTypeCode.UInt64;
                    _keyBytes = new byte[sizeof(ulong)];
                    MemoryMarshal.Write(_keyBytes, ref ul);
                    break;
                case Guid g:
                    TypeCode = SkipListNodeTypeCode.Guid;
                    _keyBytes = new byte[Marshal.SizeOf<Guid>()];
                    ((Span<byte>)_keyBytes).WriteGuid(0, g);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key));
            }
            _dataLength = (byte)_keyBytes.Length;
        }

        /// <summary>
        /// Calculates the size of the node for serialization.
        /// </summary>
        /// <returns>An uint.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint CalculateSize()
        {
            uint size = sizeof(byte) + (uint)_dataLength + Constants.PAGE_ADDRESS_SIZE + sizeof(byte) + sizeof(byte);
            size += Constants.PAGE_ADDRESS_SIZE * (uint)Forward.Length;
            return size;
        }

        public static SkipListNode FromBytes(Span<byte> span)
        {
            return new SkipListNode(span);
        }

        /// <summary>
        /// Layout
        /// 0 TypeCode byte
        /// 1-9 PageAddress PageAddress
        /// 10 Forward Length
        /// 11-X PageAddresses bytes
        /// X+1 Data Length byte
        /// X+2-Y Data bytes
        /// </summary> 
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var size = CalculateSize();
            var bytes = (Span<byte>)new byte[size];

            int pos = 0;
            bytes.WriteByte(pos, (byte)TypeCode);
            pos += sizeof(byte);
            PageAddress.ToBuffer(bytes, pos);
            pos += Constants.PAGE_ADDRESS_SIZE;
            bytes.WriteByte(pos, (byte)Forward.Length);
            pos += sizeof(byte);

            var a = MemoryMarshal.AsBytes(Forward.AsSpan());

            bytes.WriteBytes(pos, a);
            pos += Constants.PAGE_ADDRESS_SIZE * Forward.Length;
            bytes.WriteByte(pos, (byte)_dataLength);
            pos += sizeof(byte);
            bytes.WriteBytes(pos, _keyBytes);

            return bytes.ToArray();
        }
    }

    public sealed class BoxingSafeConverter<TIn, TOut>
    {
        public static readonly BoxingSafeConverter<TIn, TOut> Instance = new BoxingSafeConverter<TIn, TOut>();
        private readonly Func<TIn, TOut> convert;

        public Func<TIn, TOut> Convert => convert;

        private BoxingSafeConverter()
        {
            if (typeof(TIn) != typeof(TOut))
            {
                throw new InvalidOperationException("Both generic type parameters must represent the same type.");
            }
            var paramExpr = Expression.Parameter(typeof(TIn));
            convert =
                Expression.Lambda<Func<TIn, TOut>>(paramExpr, // this conversion is legal as typeof(TIn) = typeof(TOut)
                        paramExpr)
                    .Compile();
        }
    }
}
