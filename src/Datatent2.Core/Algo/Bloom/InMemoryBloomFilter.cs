using System;
using System.Data.HashFunction;
using System.Data.HashFunction.CityHash;
using System.Data.HashFunction.MurmurHash;
using System.Runtime.InteropServices;
using System.Text;

namespace Datatent2.Core.Algo.Bloom
{
    internal sealed class InMemoryBloomFilter<T>
    {
        private const float PROBABILITY_OF_FALSE_POSITIVES = 0.05f;

        // n
        private readonly int _numberOfElements;

        // p
        private readonly float _probabilityOfFalsePositives;

        // m
        private readonly int _size;

        // k
        private readonly int _numberOfHashFunctions;

        private readonly byte[] _bytes;

        private readonly IHashFunction _stringHashFunction;

        private readonly IHashFunction _internalHashFunction;

        private readonly Func<T, int> _hashFunction;

        public InMemoryBloomFilter(int numberOfElements, float probabilityOfFalsePositives) : this(numberOfElements, probabilityOfFalsePositives, 
            CalculateInitialValues(numberOfElements, probabilityOfFalsePositives).size, CalculateInitialValues(numberOfElements, probabilityOfFalsePositives).functions)
        {
           
        }

        public InMemoryBloomFilter(int numberOfElements, float probabilityOfFalsePositives, int arraySize, int numberOfHashFunctions)
        {
            _probabilityOfFalsePositives = probabilityOfFalsePositives;
            _numberOfElements = numberOfElements;
            _size = arraySize;
            _numberOfHashFunctions = numberOfHashFunctions;
            _bytes = new byte[_size];
            _stringHashFunction = MurmurHash3Factory.Instance.Create(new MurmurHash3Config() {HashSizeInBits = 32});

            if (typeof(T) == typeof(string))
            {
                _internalHashFunction = CityHashFactory.Instance.Create();
                
                _hashFunction = arg =>
                {
                    var s = arg as string ?? "";
                    var bytes = Encoding.UTF8.GetBytes(s);

                    return MemoryMarshal.Cast<byte, int>(_internalHashFunction.ComputeHash(bytes).Hash)[0];
                };
            }
            else if (typeof(T) == typeof(int))
            {
                _internalHashFunction = CityHashFactory.Instance.Create();

                _hashFunction = arg =>
                {
                    if (arg == null)
                        throw new ArgumentNullException(nameof(arg));

                    int a = (int)(object)arg;
                    a = (a ^ 61) ^ (a >> 16);
                    a = a + (a << 3);
                    a = a ^ (a >> 4);
                    a = a * 0x27d4eb2d;
                    a = a ^ (a >> 15);

                    return a;
                };
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        
        public InMemoryBloomFilter(int numberOfElements) : this(numberOfElements, PROBABILITY_OF_FALSE_POSITIVES)
        {

        }
        
        private static (int size, int functions) CalculateInitialValues(long numberOfElements, float prob)
        {
            /*
             n = ceil(m / (-k / log(1 - exp(log(p) / k))))
             p = pow(1 - exp(-k / (m / n)), k)
             m = ceil((n * log(p)) / log(1 / pow(2, log(2))));
             k = round((m / n) * log(2));
             */
            var size = (int) MathF.Ceiling((numberOfElements * MathF.Log2(prob)) /
                                           MathF.Log2(1 / MathF.Pow(2, (float)Math.Log2(2f))));

            var functions = (int) MathF.Round((size / (float)numberOfElements) * MathF.Log2(2));

            return (size, functions);
        }

        public void Add(T key)
        {
            if (key == null)
                return;

            int firstHash;
            if (typeof(T) == typeof(string))
                firstHash = MemoryMarshal.Cast<byte, int>(_stringHashFunction
                    .ComputeHash(Encoding.UTF8.GetBytes(key as string)).Hash)[0];
            else
                firstHash = key.GetHashCode();
            int secondaryHash = _hashFunction(key);

            for (int i = 0; i < this._numberOfHashFunctions; i++)
            {
                int hash = ComputeHash(firstHash, secondaryHash, i);
                int byteIndex = hash / 8;
                int bitInByteIndex = hash % 8;
                byte mask = (byte)(1 << bitInByteIndex);
                _bytes[byteIndex] |= mask;
            }
        }

        public bool Contains(T key)
        {
            if (key == null)
                return false;

            int firstHash;
            if (typeof(T) == typeof(string))
                firstHash = MemoryMarshal.Cast<byte, int>(_stringHashFunction
                    .ComputeHash(Encoding.UTF8.GetBytes(key as string)).Hash)[0];
            else
                firstHash = key.GetHashCode();
            int secondaryHash = _hashFunction(key);

            for (int i = 0; i < this._numberOfHashFunctions; i++)
            {
                int hash = ComputeHash(firstHash, secondaryHash, i);
                int byteIndex = hash / 8;
                int bitInByteIndex = hash % 8;
                byte mask = (byte)(1 << bitInByteIndex);
                if ((_bytes[byteIndex] & mask) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Performs Dillinger and Manolios double hashing.
        /// https://gist.github.com/richardkundl/8300092
        /// </summary>
        private int ComputeHash(int primaryHash, int secondaryHash, int i)
        {
            int resultingHash = (primaryHash + (i * secondaryHash)) % _size;
            return Math.Abs((int)resultingHash);
        }
    }
}
