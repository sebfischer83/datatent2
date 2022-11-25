// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Datatent2.Contracts;

namespace Datatent2.Core
{
    public sealed class DatatentSettings
    {
        public IOSettings IO { get; set; }

        public EngineSettings Engine { get; set; }

        public PluginSettings Plugins { get; set; }

        public string? DatabasePath { get; set; }

        public string PluginPath { get; internal set; }

        public BufferPoolImplementation BufferPoolImplementation { get; set; } = BufferPoolImplementation.Unmanaged;

        public DatatentSettings()
        {
            var pathToThisProgram = Assembly.GetExecutingAssembly() // this assembly location (/bin/Debug/netcoreapp3.1)
                .Location;
            var pathToExecutingDir = Path.GetDirectoryName(pathToThisProgram);
            PluginPath = Path.GetFullPath(Path.Combine(pathToExecutingDir!, "plugins"));
            IO = new IOSettings();
            Engine = new EngineSettings();
            Plugins = new PluginSettings();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Settings:");
            stringBuilder.AppendLine($"DatabasePath: {DatabasePath}");
            stringBuilder.AppendLine($"BufferPool: {System.Enum.GetName(typeof(BufferPoolImplementation), BufferPoolImplementation)}");
            stringBuilder.AppendLine($"IOSystem: {System.Enum.GetName(typeof(IOSystem), IO.IOSystem )}");

            return stringBuilder.ToString();
        }

        public class EngineSettings
        {
            /// <summary>
            /// Default approx. 128mb
            /// </summary>
            public int MaxPageCacheSize { get; set; } = 16384;
        }

        public class IOSettings
        {
            public IOSystem IOSystem { get; set; } = IOSystem.FileStream;
            
            /// <summary>
            /// Default approx. 64mb
            /// </summary>
            public int MaxPageReadAheadCacheSize { get; set; } = 8192;

            public bool UseReadAheadCache { get; set; } = true;
        }

        public enum IOSystem
        {
            InMemory,
            FileStream,
            RandomAccess,
            MemoryMappedFile
        }
    }

    internal class TriggerSettings
    {
        private Dictionary<string, Dictionary<TriggerType, TriggerDefinition>> _triggers = new();
    }

    internal enum TriggerType
    {
        Insert = 1
    }

    internal class TriggerDefinition
    {
        public TriggerType Type { get; set; }

        public Guid TriggerEngine { get; set; }

        public string Command { get; set; }

        public TriggerDefinition(TriggerType triggerType, Guid triggerEngine, string command)
        {
            Type = triggerType;
            TriggerEngine = triggerEngine;
            Command = command;
        }
    }
}
