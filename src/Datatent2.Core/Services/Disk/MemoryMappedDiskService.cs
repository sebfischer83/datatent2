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
        private const uint CHUNK_MULTIPLICATOR = 10000;

        private MappedRange _mapRange;

        private MemoryMappedFile? _mapFile;

        private MemoryMappedDirectAccessor _mapAccessor;

        private FileStream _internalStream { get; }

        public MemoryMappedDiskService(DatatentSettings settings, ILogger logger) : base(settings, logger)
        {
            var initialMap = 0u;
            // if file dont exist, create
            if (!Directory.Exists(Path.GetDirectoryName(settings.DatabasePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(settings.DatabasePath)!);

            if (!File.Exists(settings.DatabasePath))
            {
                logger.LogInformation($"No file exists at {settings.DatabasePath}, create new one");
                using var s = File.Create(settings.DatabasePath!);
            }
            else
            {
                // file is there, so we need to map the complete file at start
                var file = new FileInfo(settings.DatabasePath!);
                initialMap = (uint)file.Length / Constants.PAGE_SIZE;
                logger.LogInformation($"Existing file found of size {file.Length} bytes, set {nameof(initialMap)} to {initialMap}");
            }
            _internalStream = new FileStream(Settings.DatabasePath!, FileMode.Open);
            CreateMapping(initialMap);
        }

        /// <summary>
        /// Create a new mapping, that includes the given page
        /// </summary>
        /// <param name="pageId"></param>
        private void CreateMapping(uint pageId)
        {
            var startPage = 0u;
            var calc = pageId == 0 ? CHUNK_MULTIPLICATOR : pageId + 1;
            var endPage = (uint) Math.Ceiling((double)(calc) / CHUNK_MULTIPLICATOR) *CHUNK_MULTIPLICATOR - 1;

            _mapRange = new MappedRange() { From = startPage, To = endPage };
            if (_mapFile != null)
            {
                _mapAccessor.Flush();
                _mapAccessor.Dispose();
                Stream.Close();
            }
            //Logger.LogInformation($"Create new map for {_mapFile}");
            _mapFile = MemoryMappedFile.CreateFromFile(_internalStream, null, (_mapRange.To + 1) * Constants.PAGE_SIZE, MemoryMappedFileAccess.ReadWrite ,HandleInheritability.Inheritable, true);
            _mapAccessor = _mapFile.CreateDirectAccessor();
            Stream = _mapAccessor.AsStream();
        }

        protected override Stream GetStream(uint pageId)
        {
            // if the pageId is in this range already mapped return the stream ... otherwise create a new mapping
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
            _internalStream.Dispose();
            _mapFile?.Dispose();
        }

        /// <summary>
        /// Information about the range of pages that are mapped
        /// </summary>
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