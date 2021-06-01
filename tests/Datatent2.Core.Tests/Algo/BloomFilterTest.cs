using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Algo;
using Datatent2.Core.Algo.Bloom;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Algo
{
    public class BloomFilterTest
    {
        [Fact]
        public void Test()
        {
            InMemoryBloomFilter<string> inMemoryBloomFilter = new InMemoryBloomFilter<string>(10);
            inMemoryBloomFilter.Add("foo");
            inMemoryBloomFilter.Add("bar");
            inMemoryBloomFilter.Add("apple");
            inMemoryBloomFilter.Add("orange");
            inMemoryBloomFilter.Add("banana");

            inMemoryBloomFilter.Contains("banana").ShouldBe(true);
            inMemoryBloomFilter.Contains("dfsj").ShouldBe(false);
        }
    }
}
