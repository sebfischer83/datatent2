using Datatent2.Contracts;
using Datatent2.Core.Page;
using System;
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
        private readonly SkipListNodeTypeCode _typeCode;

        private readonly byte[] _keyBytes;


        /// <summary>
        /// Initializes a new instance as the starting point for a new SkipListIndexService.
        /// </summary>
        /// <param name="level">The level.</param>
        public SkipListNode(int level)
        {
            _typeCode = SkipListNodeTypeCode.Start;
            _keyBytes = Array.Empty<byte>();
            _dataLength = 0;
            Key = default!;
            PageAddress = PageAddress.Empty;
            Forward = new PageAddress[level];
        }

        private SkipListNode(Span<byte> span)
        {
            int pos = 0;
            _typeCode = (SkipListNodeTypeCode)span.ReadByte(pos);
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

            switch (_typeCode)
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
                    _typeCode = SkipListNodeTypeCode.SByte;
                    _keyBytes = new byte[sizeof(sbyte)];
                    MemoryMarshal.Write(_keyBytes, ref sb);
                    break;
                case byte bt:
                    _typeCode = SkipListNodeTypeCode.Byte;
                    _keyBytes = new byte[sizeof(byte)];
                    MemoryMarshal.Write(_keyBytes, ref bt);
                    break;
                case short st:
                    _typeCode = SkipListNodeTypeCode.Int16;
                    _keyBytes = new byte[sizeof(short)];
                    MemoryMarshal.Write(_keyBytes, ref st);
                    break;
                case int it:
                    _typeCode = SkipListNodeTypeCode.Int32;
                    _keyBytes = new byte[sizeof(int)];
                    MemoryMarshal.Write(_keyBytes, ref it);
                    break;
                case long la:
                    _typeCode = SkipListNodeTypeCode.Int64;
                    _keyBytes = new byte[sizeof(long)];
                    MemoryMarshal.Write(_keyBytes, ref la);
                    break;
                case ushort us:
                    _typeCode = SkipListNodeTypeCode.UInt16;
                    _keyBytes = new byte[sizeof(ushort)];
                    MemoryMarshal.Write(_keyBytes, ref us);
                    break;
                case uint ut:
                    _typeCode = SkipListNodeTypeCode.UInt32;
                    _keyBytes = new byte[sizeof(uint)];
                    MemoryMarshal.Write(_keyBytes, ref ut);
                    break;
                case ulong ul:
                    _typeCode = SkipListNodeTypeCode.UInt64;
                    _keyBytes = new byte[sizeof(ulong)];
                    MemoryMarshal.Write(_keyBytes, ref ul);
                    break;
                case Guid g:
                    _typeCode = SkipListNodeTypeCode.Guid;
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
            bytes.WriteByte(pos, (byte)_typeCode);
            pos += sizeof(byte);
            PageAddress.ToBuffer(bytes, pos);
            pos += Constants.PAGE_ADDRESS_SIZE;
            bytes.WriteByte(pos, (byte)Forward.Length);
            pos += sizeof(byte);

            var a = MemoryMarshal.AsBytes(Forward.AsSpan());

            bytes.WriteBytes(pos, a);
            pos += Constants.PAGE_ADDRESS_SIZE * Forward.Length;
            //for (int i = 0; i < Forward.Length; i++)
            //{
            //    ref var address = ref Forward[i];

            //    address.ToBuffer(bytes, pos);

            //    pos += Constants.PAGE_ADDRESS_SIZE;
            //}
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




    //internal readonly struct SkipListNode<TKey> : IEquatable<SkipListNode<TKey>>
    //{
    //    /// <summary>
    //    /// The key of the node.
    //    /// </summary>
    //    internal readonly TKey Key;

    //    internal readonly PageAddress PageAddress;

    //    internal readonly PageAddress[] Forward;

    //    private readonly byte _dataLength;

    //    /// <summary>
    //    /// TypeCode of TKey, can expressed as a byte
    //    /// </summary>
    //    private readonly SkipListNodeTypeCode _typeCode;

    //    private readonly byte[] _keyBytes;


    //    /// <summary>
    //    /// Initializes a new instance as the starting point for a new SkipListIndexService.
    //    /// </summary>
    //    /// <param name="level">The level.</param>
    //    public SkipListNode(int level)
    //    {
    //        _typeCode = SkipListNodeTypeCode.Start;
    //        _keyBytes = Array.Empty<byte>();
    //        _dataLength = 0;
    //        Key = default!;
    //        if (typeof(TKey) == typeof(string))
    //            Key = (TKey)(object)String.Empty;
    //        PageAddress = PageAddress.Empty;
    //        Forward = new PageAddress[level];
    //    }

    //    private SkipListNode(Span<byte> span)
    //    {
    //        int pos = 0;
    //        _typeCode = (SkipListNodeTypeCode)span.ReadByte(pos);
    //        pos += sizeof(byte);
    //        PageAddress = PageAddress.FromBuffer(span, pos);
    //        pos += Constants.PAGE_ADDRESS_SIZE;
    //        var lengthForward = span.ReadByte(pos);
    //        Forward = new PageAddress[lengthForward];
    //        pos += sizeof(byte);

    //        for (int i = 0; i < lengthForward; i++)
    //        {
    //            Forward[i] = Core.Page.PageAddress.FromBuffer(span, pos);
    //            pos += Constants.PAGE_ADDRESS_SIZE;
    //        }

    //        _dataLength = span.ReadByte(pos);
    //        pos += sizeof(byte);
    //        _keyBytes = span.ReadBytes(pos, _dataLength);

    //        switch (_typeCode)
    //        {
    //            case SkipListNodeTypeCode.Start:
    //                Key = default!;
    //                if (typeof(TKey) == typeof(string))
    //                    Key = (TKey)(object)String.Empty;
    //                break;
    //            case SkipListNodeTypeCode.Guid:
    //                Key = BoxingSafeConverter<Guid, TKey>.Instance.Convert(new Guid(_keyBytes));
    //                break;
    //            case SkipListNodeTypeCode.SByte:
    //                Key = BoxingSafeConverter<sbyte, TKey>.Instance.Convert(Unsafe.As<byte[], sbyte>(ref _keyBytes));
    //                break;
    //            case SkipListNodeTypeCode.Byte:
    //                Key = BoxingSafeConverter<byte, TKey>.Instance.Convert(Unsafe.As<byte[], byte>(ref _keyBytes));
    //                break;
    //            //case SkipListNodeTypeCode.Int16:
    //            //    break;
    //            //case SkipListNodeTypeCode.UInt16:
    //            //    break;
    //            case SkipListNodeTypeCode.Int32:
    //                Key = BoxingSafeConverter<int, TKey>.Instance.Convert(MemoryMarshal.Read<int>(_keyBytes));
    //                break;
    //            //case SkipListNodeTypeCode.UInt32:
    //            //    break;
    //            case SkipListNodeTypeCode.Int64:
    //                Key = BoxingSafeConverter<long, TKey>.Instance.Convert(MemoryMarshal.Read<long>(_keyBytes));
    //                //Key = (TKey)(object)Unsafe.As<byte[], long>(ref _keyBytes);
    //                break;
    //            //case SkipListNodeTypeCode.UInt64:
    //            //    break;
    //            //case SkipListNodeTypeCode.Single:
    //            //    break;
    //            //case SkipListNodeTypeCode.Double:
    //            //    break;
    //            //case SkipListNodeTypeCode.Decimal:
    //            //    break;
    //            //case SkipListNodeTypeCode.DateTime:
    //            //    break;
    //            //case SkipListNodeTypeCode.String:
    //            //    break;
    //            default:
    //                throw new ArgumentOutOfRangeException();
    //        }
    //    }

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="SkipListNode{TKey}"/> class.
    //    /// </summary>
    //    /// <param name="key">The key.</param>
    //    /// <param name="pageAddress">The page address.</param>
    //    /// <param name="level">The level.</param>
    //    public SkipListNode(TKey key, PageAddress pageAddress, int level)
    //    {
    //        Key = key;
    //        PageAddress = pageAddress;
    //        Forward = new PageAddress[level];

    //        switch (key)
    //        {
    //            //case string s:
    //            //    _typeCode = TypeCode.String;
    //            //    _keyBytes = Encoding.UTF8.GetBytes(s);
    //            //    _dataLength = _keyBytes.Length;
    //            //    break;
    //            case sbyte sb:
    //                _typeCode = SkipListNodeTypeCode.SByte;
    //                _keyBytes = new byte[sizeof(sbyte)];
    //                MemoryMarshal.Write(_keyBytes, ref sb);
    //                break;
    //            case byte bt:
    //                _typeCode = SkipListNodeTypeCode.Byte;
    //                _keyBytes = new byte[sizeof(byte)];
    //                MemoryMarshal.Write(_keyBytes, ref bt);
    //                break;
    //            case short st:
    //                _typeCode = SkipListNodeTypeCode.Int16;
    //                _keyBytes = new byte[sizeof(short)];
    //                MemoryMarshal.Write(_keyBytes, ref st);
    //                break;
    //            case int it:
    //                _typeCode = SkipListNodeTypeCode.Int32;
    //                _keyBytes = new byte[sizeof(int)];
    //                MemoryMarshal.Write(_keyBytes, ref it);
    //                break;
    //            case long la:
    //                _typeCode = SkipListNodeTypeCode.Int64;
    //                _keyBytes = new byte[sizeof(long)];
    //                MemoryMarshal.Write(_keyBytes, ref la);
    //                break;
    //            case ushort us:
    //                _typeCode = SkipListNodeTypeCode.UInt16;
    //                _keyBytes = new byte[sizeof(ushort)];
    //                MemoryMarshal.Write(_keyBytes, ref us);
    //                break;
    //            case uint ut:
    //                _typeCode = SkipListNodeTypeCode.UInt32;
    //                _keyBytes = new byte[sizeof(uint)];
    //                MemoryMarshal.Write(_keyBytes, ref ut);
    //                break;
    //            case ulong ul:
    //                _typeCode = SkipListNodeTypeCode.UInt64;
    //                _keyBytes = new byte[sizeof(ulong)];
    //                MemoryMarshal.Write(_keyBytes, ref ul);
    //                break;
    //            case Guid g:
    //                _typeCode = SkipListNodeTypeCode.Guid;
    //                _keyBytes = new byte[Marshal.SizeOf<Guid>()];
    //                ((Span<byte>)_keyBytes).WriteGuid(0, g);
    //                break;
    //            default:
    //                throw new ArgumentOutOfRangeException(nameof(key));
    //        }
    //        _dataLength = (byte)_keyBytes.Length;
    //    }

    //    /// <summary>
    //    /// Calculates the size of the node for serialization.
    //    /// </summary>
    //    /// <returns>An uint.</returns>
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    private uint CalculateSize()
    //    {
    //        uint size = (uint)_dataLength + Constants.PAGE_ADDRESS_SIZE + sizeof(byte);
    //        size += Constants.PAGE_ADDRESS_SIZE * (uint)Forward.Length;
    //        return size;
    //    }

    //    public static SkipListNode<TKey> FromBytes(Span<byte> span)
    //    {
    //        return new SkipListNode<TKey>(span);
    //    }

    //    /// <summary>
    //    /// Layout
    //    /// 0 TypeCode byte
    //    /// 1-9 PageAddress PageAddress
    //    /// 10 Forward Length
    //    /// 11-X PageAddresses bytes
    //    /// X+1 Data Length byte
    //    /// X+2-Y Data bytes
    //    /// </summary> 
    //    /// <returns></returns>
    //    public byte[] ToBytes()
    //    {
    //        var size = CalculateSize();
    //        var bytes = (Span<byte>)new byte[size];

    //        int pos = 0;
    //        bytes.WriteByte(pos, (byte)_typeCode);
    //        pos += sizeof(byte);
    //        PageAddress.ToBuffer(bytes, pos);
    //        pos += Constants.PAGE_ADDRESS_SIZE;
    //        bytes.WriteByte(pos, (byte)Forward.Length);
    //        pos += sizeof(byte);

    //        for (int i = 0; i < Forward.Length; i++)
    //        {
    //            ref var address = ref Forward[i];

    //            address.ToBuffer(bytes, pos);

    //            pos += Constants.PAGE_ADDRESS_SIZE;
    //        }
    //        bytes.WriteByte(pos, (byte)_dataLength);
    //        pos += sizeof(byte);
    //        bytes.WriteBytes(pos, _keyBytes);

    //        return bytes.ToArray();
    //    }

    //    public bool Equals(SkipListNode<TKey> other)
    //    {
    //        return EqualityComparer<TKey>.Default.Equals(Key, other.Key);
    //    }

    //    public override bool Equals(object? obj)
    //    {
    //        return obj is SkipListNode<TKey> other && Equals(other);
    //    }

    //    public override int GetHashCode()
    //    {
    //        return EqualityComparer<TKey>.Default.GetHashCode(Key!);
    //    }
    //}

    //public sealed class BoxingSafeConverter<TIn, TOut>
    //{
    //    public static readonly BoxingSafeConverter<TIn, TOut> Instance = new BoxingSafeConverter<TIn, TOut>();
    //    private readonly Func<TIn, TOut> convert;

    //    public Func<TIn, TOut> Convert => convert;

    //    private BoxingSafeConverter()
    //    {
    //        if (typeof(TIn) != typeof(TOut))
    //        {
    //            throw new InvalidOperationException("Both generic type parameters must represent the same type.");
    //        }
    //        var paramExpr = Expression.Parameter(typeof(TIn));
    //        convert =
    //            Expression.Lambda<Func<TIn, TOut>>(paramExpr, // this conversion is legal as typeof(TIn) = typeof(TOut)
    //                    paramExpr)
    //                .Compile();
    //    }
    //}
}
