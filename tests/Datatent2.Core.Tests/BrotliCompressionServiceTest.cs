using System;
using System.Text;
using Bogus.DataSets;
using Datatent2.Plugins.Compression;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests
{
    public class BrotliCompressionServiceTest
    {
        [Fact]
        public void Compression()
        {
            BrotliCompressionService brotliCompressionService = new BrotliCompressionService();
            var bogus = new Bogus.Randomizer();
            var bytesOrg = Encoding.UTF8.GetBytes(new Lorem().Sentence(500));
            var span = new Span<byte>(new byte[bytesOrg.Length + 50]);

            var result = brotliCompressionService.Compress(bytesOrg, span);
            result.Length.ShouldBeLessThan(bytesOrg.Length);
        }
    }
}
