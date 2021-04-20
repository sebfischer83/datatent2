// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

namespace Datatent2.Core.Scripting.Csharp
{
    public class Globals
    {
        public dynamic DataObject { get; set; }

        public Globals(DataObjectCSharpScriptWrapper dataObject)
        {
            this.DataObject = dataObject;
        }
    }
}