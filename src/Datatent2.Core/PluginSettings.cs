// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Datatent2.Contracts;
using System;

namespace Datatent2.Core
{
    public class PluginSettings
    {
        public Guid CompressionAlgorithm { get; set; }

        public PluginSettings()
        {
            CompressionAlgorithm = Constants.NopCompressionPluginId;
        }
    }
}