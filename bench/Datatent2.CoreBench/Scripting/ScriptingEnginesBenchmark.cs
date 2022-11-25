using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Bogus;
//using Datatent2.Plugins.Scripting.Csharp;
using Datatent2.Plugins.Scripting.Javascript;
using Datatent2.Plugins.Scripting.Lua;

namespace Datatent2.CoreBench.Scripting
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
    public class ScriptingEnginesBenchmark
    {
        public class TestClass
        {
            public bool BoolProp { get; set; }

            public string StringProp { get; set; }

            public decimal DecimalProp { get; set; }
        }


        [Params(256)]
        public int Size;

        private TestClass[] _testClasses;

        private LuaScriptingEngine _luaScriptingEngine;
        private JavascriptScriptingEngine _jsScriptingEngine;
        //private CsharpScriptingEngine _csharpScriptingEngine;

        [GlobalSetup()]
        public void GlobalSetup()
        {
            _luaScriptingEngine = new LuaScriptingEngine();
            _jsScriptingEngine = new JavascriptScriptingEngine();
            //_csharpScriptingEngine = new CsharpScriptingEngine();

            var bogus = new Randomizer();
            _testClasses = new TestClass[Size];
            for (int i = 0; i < Size; i++)
            {
                var c = new TestClass();
                c.BoolProp = bogus.Bool();
                c.DecimalProp = bogus.Decimal();
                c.StringProp = bogus.String(10, 200);
                _testClasses[i] = c;
            }
        }

        //[Benchmark(OperationsPerInvoke = 256)]
        //public int NLua()
        //{
        //    int a = 0;
        //    for (int i = 0; i < Size; i++)
        //    {
        //        var b = _luaScriptingEngine.Execute<bool>("res = dataObject.BoolProp", _testClasses[i]);
        //        a += b ? 1 : 0;
        //    }

        //    return a;
        //}

        //[Benchmark(OperationsPerInvoke = 256)]
        //public int NLuaParallel()
        //{
        //    int a = 0;
        //    var res =_luaScriptingEngine.Execute<bool>("res = dataObject.BoolProp", new List<object>(_testClasses));
        //    for (int i = 0; i < res.Count; i++)
        //    {
        //        a += res[i].Item2 ? 1 : 0;
        //    }

        //    return a;
        //}

        //[Benchmark(OperationsPerInvoke = 256)]
        //public int Jint()
        //{
        //    int a = 0;
        //    for (int i = 0; i < Size; i++)
        //    {
        //        var b = _jsScriptingEngine.Execute<bool>("res = dataObject.BoolProp", _testClasses[i]);
        //        a += b ? 1 : 0;
        //    }

        //    return a;
        //}

        //[Benchmark(OperationsPerInvoke = 256)]
        //public int Csharp()
        //{
        //    int a = 0;
        //    for (int i = 0; i < Size; i++)
        //    {
        //        var b = _csharpScriptingEngine.Execute<bool>("return DataObject.BoolProp;", _testClasses[i]);
        //        a += b ? 1 : 0;
        //    }

        //    return a;
        //}
    }
}
