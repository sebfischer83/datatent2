using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Datatent2.Core.Tests
{
    public  class VersionExtensionsTest
    {
        [Fact]
        public void TransformVersion()
        {
            Version version = new Version(5, 22, 1, 3);

            var test = version.GetMajorMinor();

            var testVersion = test.GetVersion();

            version.Major.ShouldBe(testVersion.Major);
            version.Minor.ShouldBe(testVersion.Minor);
        }
    }
}
