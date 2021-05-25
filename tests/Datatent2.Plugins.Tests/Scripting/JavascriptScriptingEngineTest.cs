using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Scripting;
using Datatent2.Plugins.Scripting.Javascript;
using Shouldly;
using Xunit;

namespace Datatent2.Plugins.Tests.Scripting
{
    public class JavascriptScriptingEngineTest
    {
        public class TestClass
        {
            public bool BoolProp { get; set; }
        }

        [Fact]
        public void CheckBoolProp()
        {
            TestClass testClass = new TestClass();
            testClass.BoolProp = true;
            string jsScript = "res = dataObject.BoolProp";

            JavascriptScriptingEngine scriptingEngine = new JavascriptScriptingEngine(jsScript);

            scriptingEngine.Execute<bool>(testClass).ShouldBeTrue();

            testClass.BoolProp = false;
            scriptingEngine.Execute<bool>(testClass).ShouldBeFalse();
        }
    }
}
