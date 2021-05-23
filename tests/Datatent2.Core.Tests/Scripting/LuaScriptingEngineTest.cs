//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Datatent2.Core.Scripting;
//using Datatent2.Plugins.Scripting.Lua;
//using Shouldly;
//using Xunit;

//namespace Datatent2.Core.Tests.Scripting
//{
//    public class LuaScriptingEngineTest
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
//            string luaScript = "res = dataObject.BoolProp";
            
//            LuaScriptingEngine luaScriptingEngine = new LuaScriptingEngine(luaScript);
            
//            luaScriptingEngine.Execute<bool>(testClass).ShouldBeTrue();

//            testClass.BoolProp = false;
//            luaScriptingEngine.Execute<bool>(testClass).ShouldBeFalse();
//        }
//    }
//}
