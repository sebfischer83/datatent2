//using System;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Datatent2.Core.Scripting;
//using Datatent2.Plugins.Scripting.Csharp;
//using Shouldly;
//using Xunit;

//namespace Datatent2.Core.Tests.Scripting
//{
//    public class CsharpScriptingEngineTest
//    {
//        public class TestClass
//        {
//            public bool BoolProp { get; set; }
//        }

//        [Fact]
//        public void CheckBoolProp()
//        {
//            TestClass testClass = new TestClass();
//            testClass.BoolProp = true;
//            string jsScript = "return DataObject.BoolProp;";
//            CsharpScriptingEngine scriptingEngine = new CsharpScriptingEngine(jsScript);

//            scriptingEngine.Execute<bool>(testClass).ShouldBeTrue();

//            testClass.BoolProp = false;
//            scriptingEngine.Execute<bool>(testClass).ShouldBeFalse();
//        }
//    }
//}
