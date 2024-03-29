﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Datatent2.Contracts;
using Datatent2.Core;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;

namespace Datatent2.CoreBench.Page
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    public class BasePageInsert
    {
        private DataPage _dataPage;

        [Params(200)]
        public int Size { get; set; }

        [IterationSetup]
        public void GlobalSetup()
        {
            BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);

            header.ToBuffer(bufferSegment.Span, 0);
            _dataPage = new DataPage(bufferSegment);
        }

        [Benchmark(OperationsPerInvoke = 200)]
        public int SimpleInsert()
        {
            int a = 0;
            for (int i = 0; i < Size; i++)
            {
                _dataPage.Insert(15, out var index);
                a += index;
            }

            return a;
        }
    }
}
