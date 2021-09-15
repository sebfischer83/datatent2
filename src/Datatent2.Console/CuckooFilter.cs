using System;
using System.Collections.Generic;
using System.Data.HashFunction;
using System.Data.HashFunction.MurmurHash;
using System.Data.HashFunction.xxHash;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Sigil;
using Standart.Hash.xxHash;

namespace Datatent2.Console
{
    public class CuckooFilter
    {
        /// <summary>
        /// Number of bytes in a <see cref="uint"/>
        /// </summary>
        private const int uint32Bytes = sizeof(uint);

        /// <summary>
        /// Hashing algorithm.
        /// </summary>
        private readonly IHashAlgorithm hashAlgorithm;

        /// <summary>
        /// Random instance.
        /// </summary>
        internal readonly Random random;

        /// <summary>
        /// Pre-allocated buffer we hash value indices into.
        /// </summary>
        private readonly byte[] valuesBuffer = new byte[CuckooFilter.uint32Bytes];

        /// <summary>
        /// Pre-allocated buffer we hash the fingerprint into.
        /// </summary>
        private byte[] fingerprintBuffer;

        /// <summary>
        /// Pre-allocated buffer we use for swapping fingerprints into.
        /// </summary>
        private byte[] fingerprintSwapBuffer;

        /// <summary>
        /// Comparator function, see <see cref="CodegenHelpers.CreateFingerprintComparator"/>
        /// </summary>
        private readonly Func<byte[], int, byte[], int> comparator;

        /// <summary>
        /// Comparator function, see <see cref="CodegenHelpers.CreateZeroChecker"/>
        /// </summary>
        private readonly Func<byte[], int, bool> zeroCheck;

        /// <summary>
        /// Comparator function, see <see cref="CodegenHelpers.CreateInsertIntoBucket"/>
        /// </summary>
        private readonly Func<byte[], int, byte[], bool> InsertIntoBucket;

        /// <summary>
        /// Creates a new CuckooFilter.
        /// </summary>
        /// <param name="buckets">Number of Buckets to store</param>
        /// <param name="entriesPerBucket">Number of fingerprints stored
        /// in each bucket.</param>
        /// <param name="fingerprintLength">Length of the fingerprint to use</param>
        /// <param name="maxKicks">Maximum number of times to relocate a value
        /// on a collision.</param>
        /// <param name="hashAlgorithm">Hashing algorithm to use</param>
        /// <param name="randomSeed">Random seed value</param>
        public CuckooFilter(
            uint buckets,
            uint entriesPerBucket,
            uint fingerprintLength,
            uint? maxKicks = null,
            IHashAlgorithm hashAlgorithm = null,
            int? randomSeed = null)
        {
            this.Buckets = buckets;
            this.EntriesPerBucket = entriesPerBucket;
            this.hashAlgorithm = hashAlgorithm ?? XxHashAlgorithm.Instance;
            this.MaxKicks = maxKicks ?? buckets;

            if (CuckooFilter.UpperPower2(buckets) != buckets)
            {
                throw new ArgumentException("Buckets must be a power of 2", nameof(buckets));
            }

            this.fingerprintBuffer = new byte[fingerprintLength];
            this.Contents = CuckooFilter.CreateEmptyBucketData(buckets, entriesPerBucket, fingerprintLength);
            this.random = randomSeed == null
                ? new Random()
                : new Random(randomSeed.Value);
            this.BytesPerBucket = this.EntriesPerBucket * fingerprintLength;
            this.fingerprintSwapBuffer = new byte[this.fingerprintBuffer.Length];
            this.comparator = CodegenHelpers.CreateFingerprintComparator(fingerprintLength, entriesPerBucket);
            this.zeroCheck = CodegenHelpers.CreateZeroChecker(fingerprintLength);
            this.InsertIntoBucket = CodegenHelpers.CreateInsertIntoBucket(fingerprintLength, entriesPerBucket);
        }

        /// <summary>
        /// Creates a new CuckooFilter.
        /// </summary>
        /// <param name="contents">Contents of th efilter</param>
        /// <param name="entriesPerBucket">Number of fingerprints stored
        /// in each bucket.</param>
        /// <param name="fingerprintLength">Length of the fingerprint to use</param>
        /// <param name="maxKicks">Maximum number of times to relocate a value
        /// on a collision.</param>
        /// <param name="hashAlgorithm">Hashing algorithm to use</param>
        internal CuckooFilter(
            byte[] contents,
            uint entriesPerBucket,
            uint fingerprintLength,
            uint maxKicks,
            IHashAlgorithm hashAlgorithm = null)
        {
            this.Contents = contents;
            this.MaxKicks = maxKicks;
            this.EntriesPerBucket = entriesPerBucket;
            this.BytesPerBucket = entriesPerBucket * fingerprintLength;
            this.Buckets = (uint)contents.Length / this.BytesPerBucket;
            this.fingerprintBuffer = new byte[fingerprintLength];
            this.fingerprintSwapBuffer = new byte[this.fingerprintBuffer.Length];
            this.hashAlgorithm = hashAlgorithm ?? XxHashAlgorithm.Instance;
            this.zeroCheck = CodegenHelpers.CreateZeroChecker(fingerprintLength);
            this.comparator = CodegenHelpers.CreateFingerprintComparator(fingerprintLength, 4);
            this.InsertIntoBucket = CodegenHelpers.CreateInsertIntoBucket(fingerprintLength, entriesPerBucket);
        }

        /// <summary>
        /// Creates a new optimally-sized CuckooFilter with a target
        /// false-positive-at-capacity.
        /// </summary>
        /// <param name="capacity">Filter capacity</param>
        /// <param name="falsePositiveRate">Desired false positive rate.</param>
        /// <param name="hashAlgorithm">Hashing algorithm to use</param>
        /// <param name="randomSeed">Random seed value</param>
        public CuckooFilter(
            uint capacity,
            double falsePositiveRate,
            IHashAlgorithm hashAlgorithm = null,
            int? randomSeed = null)
        {
            this.hashAlgorithm = hashAlgorithm ?? XxHashAlgorithm.Instance;

            // "In summary, we choose (2, 4)-cuckoo filter (i.e., each item has
            // two candidate Buckets and each bucket has up to four fingerprints)
            // as the default configuration, because it achieves the best or
            // close - to - best space efficiency for the false positive
            // rates that most practical applications""
            this.EntriesPerBucket = 4;

            // Equation here from page 8, step 6, of the paper:
            // ceil(log_2 (2b / \epsilon)
            var desiredLength = Math.Log(2 * (float)this.EntriesPerBucket / falsePositiveRate, 2);
            var fingerprintLength = (uint)Math.Ceiling(desiredLength / 8);
            this.fingerprintBuffer = new byte[fingerprintLength];

            // Not explicitly defined in the paper, however this is the
            // algorithm used in the author's implementation:
            // https://github.com/efficient/cuckoofilter/blob/master/src/cuckoofilter.h#L89
            this.Buckets = CuckooFilter.UpperPower2(capacity / this.EntriesPerBucket);
            if ((double)capacity / this.Buckets / this.EntriesPerBucket > 0.96)
            {
                this.Buckets <<= 1;
            }

            this.MaxKicks = this.Buckets;
            this.random = randomSeed == null
                ? new Random()
                : new Random(randomSeed.Value);
            this.Contents = CuckooFilter.CreateEmptyBucketData(this.Buckets, this.EntriesPerBucket, fingerprintLength);
            this.BytesPerBucket = this.EntriesPerBucket * fingerprintLength;
            this.fingerprintSwapBuffer = new byte[this.fingerprintBuffer.Length];
            this.zeroCheck = CodegenHelpers.CreateZeroChecker(fingerprintLength);
            this.comparator = CodegenHelpers.CreateFingerprintComparator(fingerprintLength, 4);
            this.InsertIntoBucket = CodegenHelpers.CreateInsertIntoBucket(fingerprintLength, this.EntriesPerBucket);
        }

        /// <summary>
        /// Gets the number of Buckets the filter contains.
        /// </summary>
        public uint Buckets { get; }

        /// <summary>
        /// Number of bytes each bucket takes.
        /// </summary>
        public uint BytesPerBucket { get; }

        /// <summary>
        /// Gets the number of fingerprints to store per bucket.
        /// </summary>
        public uint EntriesPerBucket { get; }

        /// <summary>
        /// Gets the length of the fingerprint.
        /// </summary>
        public uint FingerprintLength
        {
            get
            {
                return (uint)this.fingerprintBuffer.Length;
            }
        }

        /// <summary>
        /// Gets the max number of times we'll try to kick and item from a
        /// bucket when we insert before giving it.
        /// </summary>
        public uint MaxKicks { get; }

        /// <summary>
        /// Gets the total size, in memory, of the filter.
        /// </summary>
        public uint Size
        {
            get
            {
                return (uint)this.Contents.Length;
            }
        }

        /// <summary>
        /// Contents of the cuckoo filter.
        /// </summary>
        internal byte[] Contents { get; }

        /// <summary>
        /// Returns a value indicating whether the filter probably contains
        /// the given item.
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>True if the filter contains the value, false otherwise.</returns>
        public bool Contains(byte[] value)
        {
            var fingerprint = this.GetFingerprint(value);
            this.hashAlgorithm.Hash(this.valuesBuffer, value, CuckooFilter.uint32Bytes);

            var index1 = CuckooFilter.ToInt32(this.valuesBuffer) & (this.Buckets - 1);
            if (this.comparator(this.Contents, this.IndexToOffset(index1), fingerprint) != -1)
            {
                return true;
            }

            var index2 = this.DeriveIndex2(fingerprint, index1);
            if (this.comparator(this.Contents, this.IndexToOffset(index2), fingerprint) != -1)
            {
                return true;
            }

            return false;
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
        public bool Equals(CuckooFilter other)
        {
            if (object.ReferenceEquals(null, other))
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Buckets == other.Buckets
                   && this.BytesPerBucket == other.BytesPerBucket
                   && CodegenHelpers.BytesEquals(this.Contents, other.Contents)
                   && this.EntriesPerBucket == other.EntriesPerBucket
                   && this.MaxKicks == other.MaxKicks;
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(null, obj))
            {
                return false;
            }

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((CuckooFilter)obj);
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var contentHash = new byte[4];
                this.hashAlgorithm.Hash(contentHash, this.Contents, 4);

                var hashCode = (int)this.Buckets;
                hashCode = (hashCode * 397) ^ (int)this.BytesPerBucket;
                hashCode = (hashCode * 397) ^ (contentHash[0] | (contentHash[1] << 8)
                                                              | (contentHash[2] << 16)
                                                              | (contentHash[3] << 24));
                hashCode = (hashCode * 397) ^ (int)this.EntriesPerBucket;
                hashCode = (hashCode * 397) ^ (int)this.MaxKicks;
                return hashCode;
            }
        }

        /// <summary>
        /// Inserts the value into the filter. Whereas <see cref="TryInsert"/>
        /// returns false if the value cannot be inserted, this throws.
        /// </summary>
        /// <param name="value">Value to insert</param>
        /// <exception cref="FilterFullException">Thrown if the filter is
        /// too full to accept the value.</exception>
        public void Insert(byte[] value)
        {
            if (!this.TryInsert(value))
            {
                throw new Exception();
            }
        }

        /// <summary>
        /// Removes a value from the filter.
        /// </summary>
        /// <param name="value">Value to remove</param>
        /// <returns>True if the filter contained the value, false otherwise.</returns>
        public bool Remove(byte[] value)
        {
            var fingerprint = this.GetFingerprint(value);
            this.hashAlgorithm.Hash(this.valuesBuffer, value, CuckooFilter.uint32Bytes);

            var index1 = CuckooFilter.ToInt32(this.valuesBuffer) & (this.Buckets - 1);

            var offset = this.IndexToOffset(index1);
            var removal = this.comparator(this.Contents, offset, fingerprint);
            if (removal != -1)
            {
                Array.Clear(this.Contents, offset + this.fingerprintBuffer.Length * removal, fingerprint.Length);
                return true;
            }


            var index2 = this.DeriveIndex2(fingerprint, index1);
            offset = this.IndexToOffset(index2);
            removal = this.comparator(this.Contents, offset, fingerprint);
            if (removal != -1)
            {
                Array.Clear(this.Contents, offset + this.fingerprintBuffer.Length * removal, fingerprint.Length);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to insert the value into the filter.
        /// </summary>
        /// <param name="value">Value to insert</param>
        /// <returns>True if it was inserted successfully, false if the
        /// filter was too full to do so.</returns>
        public bool TryInsert(byte[] value)
        {
            return this.TryInsertInner(value);
        }

        /// <summary>
        /// Attempts to insert the value into the filter.
        /// </summary>
        /// <param name="value">Value to insert</param>
        /// <returns>True if it was inserted successfully, false if the
        /// filter was too full to do so.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryInsertInner(byte[] value)
        {
            var fingerprint = this.GetFingerprint(value);
            this.hashAlgorithm.Hash(this.valuesBuffer, value, CuckooFilter.uint32Bytes);
            var index1 = BoundToBucketCount(CuckooFilter.ToInt32(this.valuesBuffer));

            if (this.InsertIntoBucket(this.Contents, IndexToOffset(index1), fingerprint))
            {
                return true;
            }

            var index2 = this.DeriveIndex2(fingerprint, index1);
            if (this.InsertIntoBucket(this.Contents, IndexToOffset(index2), fingerprint))
            {
                return true;
            }

            var targetIndex = this.random.Next(1) == 0
                ? index1
                : index2;

            for (var i = 0; i < this.MaxKicks; i++)
            {
                fingerprint = this.SwapIntoBucket(fingerprint, targetIndex);
                targetIndex = this.DeriveIndex2(fingerprint, targetIndex);

                if (this.InsertIntoBucket(this.Contents, IndexToOffset(targetIndex), fingerprint))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a nicely formatted version of the filter. Used for
        /// examining internal state while testing.
        /// </summary>
        /// <returns></returns>
        internal IList<IList<string>> DumpDebug()
        {
            var list = new List<IList<string>>();
            for (var offset = 0L; offset < this.Contents.Length; offset += this.BytesPerBucket)
            {
                var items = new List<string>();
                for (var i = 0; i < this.EntriesPerBucket; i++)
                {
                    var target = (int)offset + i * this.fingerprintBuffer.Length;
                    if (!this.zeroCheck(this.Contents, target))
                    {
                        items.Add(Encoding.ASCII.GetString(this.Contents, target, this.fingerprintBuffer.Length));
                    }
                }

                list.Add(items);
            }

            return list;
        }

        private static byte[] CreateEmptyBucketData(long buckets, uint itemsPerBucket, uint bytesPerItem)
        {
            return new byte[buckets * itemsPerBucket * bytesPerItem];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToInt32(byte[] data)
        {
            int x = data[0] << 24;
            x |= data[1] << 16;
            x |= data[2] << 8;
            x |= data[3];
            return x;
        }

        private static uint UpperPower2(uint x)
        {
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x |= x >> 32;
            x++;
            return x;
        }

        /// <summary>
        /// Ensures the value is less than or equal to the number of Buckets.
        /// </summary>
        /// <param name="value">Value to bound</param>
        /// <returns>Truncated value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long BoundToBucketCount(long value)
        {
            // this.Buckets is always a power of 2, so to ensure index1 is <=1
            // the number of Buckets, we mask it against Buckets - 1. So if
            // Buckets is 16 (0b10000), we mask it against (0b01111).
            return value & (this.Buckets - 1);
        }

        /// <summary>
        /// Get the alternative index for an item, given its primary
        /// index and fingerprint.
        /// </summary>
        /// <param name="fingerprint">Fingerprint value</param>
        /// <param name="index1">Primary index</param>
        /// <returns>The secondary index</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long DeriveIndex2(byte[] fingerprint, long index1)
        {
            this.hashAlgorithm.Hash(this.valuesBuffer, fingerprint, CuckooFilter.uint32Bytes);
            return index1 ^ this.BoundToBucketCount(CuckooFilter.ToInt32(this.valuesBuffer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] GetFingerprint(byte[] input)
        {
            this.hashAlgorithm.Hash(this.fingerprintBuffer, input, this.fingerprintBuffer.Length);
            if (this.zeroCheck(this.fingerprintBuffer, 0))
            {
                for (var i = 0; i < this.fingerprintBuffer.Length; i++)
                {
                    this.fingerprintBuffer[i] = 0xff;
                }
            }

            return this.fingerprintBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexToOffset(long index)
        {
            return (int)(index * this.BytesPerBucket);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] SwapIntoBucket(byte[] fingerprint, long bucket)
        {
            var subIndex = this.random.Next((int)this.EntriesPerBucket);
            var offset = this.IndexToOffset(bucket) + subIndex * fingerprint.Length;
            var newFingerprint = this.fingerprintSwapBuffer;

            Array.Copy(this.Contents, offset, this.fingerprintSwapBuffer, 0, fingerprint.Length);
            Array.Copy(fingerprint, 0, this.Contents, offset, fingerprint.Length);
            this.fingerprintSwapBuffer = fingerprint;
            this.fingerprintBuffer = newFingerprint;

            return newFingerprint;
        }
    }

    internal static class CodegenHelpers
    {
        public static bool BytesEquals(byte[] a, byte[] b)
        {
            return CodegenHelpers.BytesEquals(a, 0, b, 0, a.Length);
        }

        /// <summary>
        /// Returns whether the two arrays are equal. It assumes that both
        /// are at least the "length" long.
        /// </summary>
        /// <param name="a">First array</param>
        /// <param name="offsetA">Offset in first array to look from</param>
        /// <param name="b">Second array</param>
        /// <param name="offsetB">Offset in second array to look from</param>
        /// <param name="length">Number of bytes in each to compare</param>
        /// <returns>True if all bytes are equal, false otherwise</returns>
        public static bool BytesEquals(byte[] a, int offsetA, byte[] b, int offsetB, int length)
        {
            var baseOffset = 0;
            for (; baseOffset + Vector<byte>.Count <= length; baseOffset += Vector<byte>.Count)
            {
                var aVec = new Vector<byte>(a, baseOffset + offsetA);
                var bVec = new Vector<byte>(b, baseOffset + offsetB);
                if (aVec != bVec)
                {
                    return false;
                }
            }

            var remaining = length - baseOffset;
            for (var i = 0; i < remaining; i++)
            {
                if (a[baseOffset + offsetA + i] != b[baseOffset + offsetB + i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns whether the array is all zeroes from the given offset
        /// and length.
        /// </summary>
        /// <param name="array">Array to check</param>
        /// <param name="offset">Offset to look at</param>
        /// <param name="length">Number of bytes to check</param>
        /// <returns>The number of bytes that are zero</returns>
        public static bool IsZero(byte[] array, int offset, int length)
        {
            var zeroVector = new Vector<byte>(0);

            var baseOffset = 0;
            for (; baseOffset + Vector<byte>.Count <= length; baseOffset += Vector<byte>.Count)
            {
                if (new Vector<byte>(array, baseOffset + offset) != zeroVector)
                {
                    return false;
                }
            }

            var remaining = length - baseOffset;
            for (var i = 0; i < remaining; i++)
            {
                if (array[offset + baseOffset + i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a function that checks if the fingerprint at the given
        /// offset (arg 2) in the first argument (arg 1) is equal to the
        /// fingerprint in arg 3.
        /// </summary>
        /// <param name="fingerprintSize">Number of bytes in the fingerprint</param>
        /// <param name="entriesPerBucket">Number of entries in each bucket</param>
        /// <returns>The created delegate</returns>
        public static Func<byte[], int, byte[], int> CreateFingerprintComparator(uint fingerprintSize, uint entriesPerBucket)
        {
            var e1 = Emit<Func<byte[], int, byte[], int>>.NewDynamicMethod();

            Label nextStatement = null;
            for (var entryIndex = 0; entryIndex < entriesPerBucket; entryIndex++)
            {
                if (nextStatement != null)
                {
                    e1.MarkLabel(nextStatement);
                }

                nextStatement = e1.DefineLabel();
                for (var byteIndex = 0; byteIndex < fingerprintSize; byteIndex++)
                {
                    // Retrieve a[offset + i]
                    e1.LoadArgument(0);
                    e1.LoadArgument(1);

                    var offset = (int)(fingerprintSize * entryIndex + byteIndex);
                    if (offset > 0)
                    {
                        e1.LoadConstant(offset);
                        e1.Add();
                    }
                    e1.LoadElement<byte>();

                    // Retrieve b[i]
                    e1.LoadArgument(2);
                    e1.LoadConstant(byteIndex);
                    e1.LoadElement<byte>();

                    // Go to the false return of not equal
                    e1.UnsignedBranchIfNotEqual(nextStatement);
                }

                e1.LoadConstant(entryIndex);
                e1.Return();
            }

            e1.MarkLabel(nextStatement);
            e1.LoadConstant(-1);
            e1.Return();

            return e1.CreateDelegate();
        }

        /// <summary>
        /// Creates a function that checks if the fingerprint at the given
        /// offset (arg 2) in the first argument (arg 1) is zero.
        /// </summary>
        /// <param name="fingerprintSize">Number of bytes in the fingerprint</param>
        /// <returns>The created delegate</returns>
        public static Func<byte[], int, bool> CreateZeroChecker(uint fingerprintSize)
        {
            var e1 = Emit<Func<byte[], int, bool>>.NewDynamicMethod();
            var returnFalse = e1.DefineLabel();

            for (var byteIndex = 0; byteIndex < fingerprintSize; byteIndex++)
            {
                // Load the contents array:
                e1.LoadArgument(0);

                // Set the index we want to offset + i:
                e1.LoadArgument(1);
                if (byteIndex > 0)
                {
                    e1.LoadConstant(byteIndex);
                    e1.Add();
                }
                e1.LoadElement<byte>();


                // Go to the next statement (the next entry check) if it's not 0.
                e1.LoadConstant(0);
                e1.UnsignedBranchIfNotEqual(returnFalse);
            }

            // Got down here? We're good, true true.
            e1.LoadConstant(true);
            e1.Return();

            // False branch:
            e1.MarkLabel(returnFalse);
            e1.LoadConstant(false);
            e1.Return();

            return e1.CreateDelegate();
        }

        /// <summary>
        /// Creates a function that checks inserts a fingerprint (arg 3) into
        /// the index (arg 2) in the contents (arg 1) if there's any available
        /// unassigned fingerprint slot. Returns true if the insertion was
        /// made successfully.
        /// </summary>
        /// <param name="fingerprintSize">Number of bytes in the fingerprint</param>
        /// <param name="entriesPerBucket">Number of entries in each bucket</param>
        /// <returns>The created delegate</returns>
        public static Func<byte[], int, byte[], bool> CreateInsertIntoBucket(uint fingerprintSize, uint entriesPerBucket)
        {
            var e = Emit<Func<byte[], int, byte[], bool>>.NewDynamicMethod();

            Label nextStatement = null;
            for (var entryIndex = 0; entryIndex < entriesPerBucket; entryIndex++)
            {
                if (nextStatement != null)
                {
                    e.MarkLabel(nextStatement);
                }

                nextStatement = e.DefineLabel();

                // First, check that all bytes in the target spot are 0.
                for (var byteIndex = 0; byteIndex < fingerprintSize; byteIndex++)
                {
                    // Load the contents array:
                    e.LoadArgument(0);

                    // Set the index we want to offset + i:
                    e.LoadArgument(1);
                    var offset = (int)(fingerprintSize * entryIndex + byteIndex);
                    if (offset > 0)
                    {
                        e.LoadConstant(offset);
                        e.Add();
                    }

                    // Load the element at contents[offset]:
                    e.LoadElement<byte>();

                    // Go to the next statement (the next entry check) if it's not 0.
                    e.LoadConstant(0);
                    e.UnsignedBranchIfNotEqual(nextStatement);
                }

                // If we're here, we found a free space that we can put our
                // fingerprint in.
                for (var byteIndex = 0; byteIndex < fingerprintSize; byteIndex++)
                {
                    // Element setting takes three values: the array to load
                    // into, the offset, and the data to load.

                    // Array to load into:
                    e.LoadArgument(0);

                    // Target offset in the array:
                    e.LoadArgument(1);
                    var offset = (int)(fingerprintSize * entryIndex + byteIndex);
                    if (offset > 0)
                    {
                        e.LoadConstant(offset);
                        e.Add();
                    }

                    // Load the value to be loaded from the fingerprint:
                    e.LoadArgument(2);
                    e.LoadConstant(byteIndex);
                    e.LoadElement<byte>();

                    // Store it!
                    e.StoreElement<byte>();
                }

                // Now we're good, retur:
                e.LoadConstant(true);
                e.Return();
            }

            e.MarkLabel(nextStatement);
            e.LoadConstant(false);
            e.Return();

            return e.CreateDelegate();
        }
    }

    public interface IHashAlgorithm
    {
        /// <summary>
        /// Returns a hash of the value.
        /// </summary>
        /// <param name="target">Target array to hash to</param>
        /// <param name="value">Value to hash</param>
        /// <param name="hashLength">Number of bytes to write into the target.</param>
        void Hash(byte[] target, byte[] value, int hashLength);
    }

    public class XxHashAlgorithm : IHashAlgorithm
    {
        internal static readonly IHashAlgorithm Instance = new XxHashAlgorithm();
        private static IHashFunction _hashFunction = xxHashFactory.Instance.Create();

        private readonly ulong seed;

        /// <summary>
        /// Creates a new xxhash algorithm instance.
        /// </summary>
        /// <param name="seed">Seed value for the hash</param>
        public XxHashAlgorithm(ulong seed = 0)
        {
            this.seed = seed;
        }

        /// <summary>
        /// Returns a hash of the value.
        /// </summary>
        /// <param name="target">Target array to hash to</param>
        /// <param name="value">Value to hash</param>
        /// <param name="hashLength">Desired length of the fingerprint.</param>
        public void Hash(byte[] target, byte[] value, int hashLength)
        {
            var hash = xxHash64.ComputeHash(value, value.Length, this.seed); 
            for (var i = 0; i < hashLength; i++)
            {
                target[i] = (byte)(hash & 0xFF);
                hash >>= 8;
            }
        }
    }

    public class SimpleStreamSerializer
    {
        /// <summary>
        /// Deserializes the CuckooFilter to from a stream.
        /// </summary>
        /// <param name="source">Source stream to read</param>
        /// <param name="hashAlgorithm">Hash algorithm</param>
        public virtual CuckooFilter Deserialize(Stream source, IHashAlgorithm hashAlgorithm = null)
        {
            var paramsReadBuffer = new byte[16];
            source.Read(paramsReadBuffer, 0, 16);

            var entriesPerBucket = SimpleStreamSerializer.ReadBigEndianUint(paramsReadBuffer, 0);
            var fingerprintLength = SimpleStreamSerializer.ReadBigEndianUint(paramsReadBuffer, 4);
            var buckets = SimpleStreamSerializer.ReadBigEndianUint(paramsReadBuffer, 8);
            var maxKicks = SimpleStreamSerializer.ReadBigEndianUint(paramsReadBuffer, 12);

            var contents = new byte[buckets * entriesPerBucket * fingerprintLength];
            source.Read(contents, 0, contents.Length);

            return new CuckooFilter(
                contents: contents,
                entriesPerBucket: entriesPerBucket,
                fingerprintLength: fingerprintLength,
                maxKicks: maxKicks,
                hashAlgorithm: hashAlgorithm);
        }

        /// <summary>
        /// Serializes the CuckooFilter to a stream.
        /// </summary>
        /// <param name="target">Target stream to write to</param>
        /// <param name="filter">Filter to serialize</param>
        public virtual void Serialize(Stream target, CuckooFilter filter)
        {
            var writeBuffer = new byte[16];
            SimpleStreamSerializer.WriteBigEndianUint(writeBuffer, filter.EntriesPerBucket, 0);
            SimpleStreamSerializer.WriteBigEndianUint(writeBuffer, filter.FingerprintLength, 4);
            SimpleStreamSerializer.WriteBigEndianUint(writeBuffer, filter.Buckets, 8);
            SimpleStreamSerializer.WriteBigEndianUint(writeBuffer, filter.MaxKicks, 12);
            target.Write(writeBuffer, 0, 16);
            target.Write(filter.Contents, 0, filter.Contents.Length);
        }

        private static uint ReadBigEndianUint(byte[] target, int offset)
        {
            uint x = 0;
            x |= (uint)(target[offset] << 24);
            x |= (uint)(target[offset + 1] << 16);
            x |= (uint)(target[offset + 2] << 8);
            x |= target[offset + 3];

            return x;
        }

        private static void WriteBigEndianUint(byte[] target, uint number, int offset)
        {
            target[offset] = (byte)(number >> 24);
            target[offset + 1] = (byte)(number >> 16);
            target[offset + 2] = (byte)(number >> 8);
            target[offset + 3] = (byte)number;
        }
    }
}
