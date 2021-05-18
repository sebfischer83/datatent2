// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Memory
{
    internal static class BufferPoolFactory
    {
        private static DatatentSettings? _settings;
        private static ILogger? _logger;

        public static void Init(DatatentSettings datatentSettings, ILogger logger)
        {
            _settings = datatentSettings;
            _logger = logger;
            UnmanagedBufferPool.InitFunction = () => (datatentSettings, logger);
        }

        public static BufferPoolBase Get()
        {
            if (_settings == null)
            {
                throw new InvalidEngineStateException($"{nameof(BufferPoolFactory)} needs Init!");
            }

            if (_settings?.BufferPoolImplementation == BufferPoolImplementation.Unmanaged)
                return UnmanagedBufferPool.Shared;
            if (_settings?.BufferPoolImplementation == BufferPoolImplementation.Managed)
                return BufferPool.Shared;

            throw new InvalidEngineStateException($@"Invalid {nameof(BufferPoolImplementation)} configured, 
            value {System.Enum.GetName(typeof(BufferPoolImplementation), _settings!.BufferPoolImplementation)} is not supported!");
        }
    }
}
