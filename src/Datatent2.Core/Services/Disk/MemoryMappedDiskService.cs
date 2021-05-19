using Datatent2.Contracts;
using Datatent2.Core.Page.GlobalAllocationMap;
using DotNext.IO.MemoryMappedFiles;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
#pragma warning disable 8618

namespace Datatent2.Core.Services.Disk
{
    internal sealed class MemoryMappedDiskService : DiskService
    {
         /// <summary>
        /// 10000 pages per chunk
        /// </summary>
        private const uint _chunkMultiplicator = 10000;

        private MappedRange _mapRange;

        private MemoryMappedFile _mapFile;

        private MemoryMappedDirectAccessor _mapAccessor;

        public MemoryMappedDiskService(DatatentSettings settings, ILogger logger) : base(settings, logger)
        {
            var initalMap = 0u;
            // if file dont exist, create
            if (!File.Exists(settings.DatabasePath))
            {
                logger.LogInformation($"No file exists at {settings.DatabasePath}, create new one");
                using var s = File.Create(settings.DatabasePath!);
            }
            else
            {
                // file is there, so we need to map the complete file at start
                var file = new FileInfo(settings.DatabasePath!);
                initalMap = (uint)file.Length / Constants.PAGE_SIZE;
                logger.LogInformation($"Existings file found of size {file.Length} bytes, set {nameof(initalMap)} to {initalMap}");
            }
            CreateMapping(initalMap);
        }

        private void CreateMapping(uint pageId)
        {
            var startPage = 0u;
            var calc = pageId == 0 ? _chunkMultiplicator : pageId + 1;
            var endPage = (uint) Math.Ceiling((double)(calc) / _chunkMultiplicator) *_chunkMultiplicator - 1;

            _mapRange = new MappedRange() { From = startPage, To = endPage };
            if (_mapFile != null)
            {
                _mapAccessor.Flush();
                _mapAccessor.Dispose();
                _mapFile.Dispose();
                Stream.Close();
            }
            Logger.LogInformation($"Create new map for {_mapFile}");
            _mapFile = MemoryMappedFile.CreateFromFile(Settings.DatabasePath!, FileMode.Open, null, (_mapRange.To + 1) * Constants.PAGE_SIZE);
            _mapAccessor = _mapFile.CreateDirectAccessor();
            Stream = _mapAccessor.AsStream();
        }

        protected override Stream GetStream(uint pageId)
        {
            if (_mapRange.InRange(pageId))
            {
                return Stream;
            }
            CreateMapping(pageId);
            return Stream;
        }

        public override void Dispose()
        {
            base.Dispose();
            _mapAccessor.Flush();
            _mapAccessor.Dispose();
            _mapFile?.Dispose();
        }

        private class MappedRange
        {
            public uint From { get; set; }

            public uint To { get; set; }

            public bool InRange(uint pageId)
            {
                return From <= pageId && pageId <= To;
            }

            public override string ToString()
            {
                return $"{From}-{To}";
            }
        }
    }
}
#pragma warning restore 8618