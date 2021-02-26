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