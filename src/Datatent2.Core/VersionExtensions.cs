using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core
{
    public static class VersionExtensions
    {
        public static ushort GetMajorMinor(this Version version)
        {
            ushort versions = (ushort)(version.Major + (version.Minor << 8));

            return versions;
        }

        public static Version GetVersion(this ushort versions)
        {
            var major = (byte)versions;
            var minor = versions >> 8;

            return new Version(major, minor);
        }

        public static bool CompareToMajorMinor(this Version version, ushort versions)
        {
            var versions2 = version.GetMajorMinor();

            return versions2 == versions;
        }
    }
}
