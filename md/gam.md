## GAM, Loops und Optimierungen

!!!

![https://codemonkeyspace.b-cdn.net/post/2021/it_starts_with_a_page/gam.svg](https://codemonkeyspace.b-cdn.net/post/2021/it_starts_with_a_page/gam.svg)

!!!

##### erste Optimierungen

- Edge cases direkt behandeln (leer und voll) <!-- .element: class="fragment" -->
- als longs Daten betrachten <!-- .element: class="fragment" -->
- Intrinsics nutzen <!-- .element: class="fragment" -->

!!!

<div style="font-size:18px">

```cs
public int DefaultLoop(Span<byte> span)
{
    Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(span);

    if (longSpan[0] == 0)
        return 1;

    if (longSpan[^1] == long.MaxValue)
        return -1;

    int iterCount = longSpan.Length / 4;
    for (int i = 0; i < iterCount; i++)
    {
        ref ulong l = ref longSpan[i];
        if (l == ulong.MaxValue)
            continue;
        int res = 0;
        var count = BitOperations.LeadingZeroCount(l);
        res = (64 - count) + 1;
        if (i > 0 && res != -1)
            res += (64 * i);
        return res;
    }

    return -1;
}
```

</div>

<div class="fragment">
    <div style="overflow-x:auto">
        <table>
        <thead>
            <tr>
            <th>Method</th>
            <th>Iterations</th>
            <th style="text-align:right">Mean</th>
            <th style="text-align:right">Error</th>
            <th style="text-align:right">StdDev</th>
            <th style="text-align:right">Median</th>
            <th style="text-align:right">Ratio</th>
            <th style="text-align:right">RatioSD</th>
            <th>Baseline</th>
            </tr>
        </thead>
        <tbody>
            <tr>
            <td>Loop</td>
            <td>1000</td>
            <td style="text-align:right">138.85 μs</td>
            <td style="text-align:right">1.539 μs</td>
            <td style="text-align:right">2.303 μs</td>
            <td style="text-align:right">138.74 μs</td>
            <td style="text-align:right">1.00</td>
            <td style="text-align:right">0.00</td>
            <td>Yes</td>
            </tr>
        </tbody>
        </table>
    </div>
</div>

!!!

##### Loop unrolling

<div style="font-size:18px">

```cs
public int Unroll4(Span<byte> span)
{
    Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(span);

    if (longSpan[0] == 0)
        return 1;

    if (longSpan[^1] == long.MaxValue)
        return -1;

    int iterCount = longSpan.Length;
    for (int i = 0; i < iterCount; i += 4)
    {
        ref ulong l4 = ref longSpan[i + 3];
        // when l4 is max value all others before too
        if (l4 == ulong.MaxValue)
            continue;

        ref ulong l1 = ref longSpan[i];
        ref ulong l2 = ref longSpan[i + 1];
        ref ulong l3 = ref longSpan[i + 2];

        int res = -1;
        if (l1 != ulong.MaxValue)
        {
            var count = BitOperations.LeadingZeroCount(l1);

            res = (64 - count) + 1;
        }
        else if (l2 != ulong.MaxValue)
        {
            var count = BitOperations.LeadingZeroCount(l2);
            res = (64) - count + 64 + 1;
        }
        else if (l3 != ulong.MaxValue)
        {
            var count = BitOperations.LeadingZeroCount(l3);
            res = (64) - count + 128 + 1;
        }
        else if (l4 != ulong.MaxValue)
        {
            var count = BitOperations.LeadingZeroCount(l4);
            res = (64) - count + 192 + 1;
        }

        if (i > 0 && res != -1)
            res += (64 * i);

        return res;
    }

    return -1;
}
```

</div>

 <div class="fragment">
            <table>
              <thead>
                <tr>
                  <th>Method</th>
                  <th>Iterations</th>
                  <th style="text-align:right">Mean</th>
                  <th style="text-align:right">Error</th>
                  <th style="text-align:right">StdDev</th>
                  <th style="text-align:right">Median</th>
                  <th style="text-align:right">Ratio</th>
                  <th style="text-align:right">RatioSD</th>
                  <th>Baseline</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>Loop</td>
                  <td>1000</td>
                  <td style="text-align:right">138.85 μs</td>
                  <td style="text-align:right">1.539 μs</td>
                  <td style="text-align:right">2.303 μs</td>
                  <td style="text-align:right">138.74 μs</td>
                  <td style="text-align:right">1.00</td>
                  <td style="text-align:right">0.00</td>
                  <td>Yes</td>
                </tr>
                <tr>
                  <td>LoopUnrolled4</td>
                  <td>1000</td>
                  <td style="text-align:right">102.69 μs</td>
                  <td style="text-align:right">1.514 μs</td>
                  <td style="text-align:right">2.171 μs</td>
                  <td style="text-align:right">102.61 μs</td>
                  <td style="text-align:right">0.74</td>
                  <td style="text-align:right">0.02</td>
                  <td>No</td>
                </tr>
                <tr>
                  <td>LoopUnrolled2</td>
                  <td>1000</td>
                  <td style="text-align:right">155.59 μs</td>
                  <td style="text-align:right">2.035 μs</td>
                  <td style="text-align:right">2.918 μs</td>
                  <td style="text-align:right">156.24 μs</td>
                  <td style="text-align:right">1.12</td>
                  <td style="text-align:right">0.03</td>
                  <td>No</td>
                </tr>
                <tr>
                  <td>LoopUnrolled8</td>
                  <td>1000</td>
                  <td style="text-align:right">77.82 μs</td>
                  <td style="text-align:right">1.153 μs</td>
                  <td style="text-align:right">1.690 μs</td>
                  <td style="text-align:right">77.90 μs</td>
                  <td style="text-align:right">0.56</td>
                  <td style="text-align:right">0.02</td>
                  <td>No</td>
                </tr>
              </tbody>
            </table>
          </div>

!!!

 <img src="./img/5lbio7.jpg" height="75%">

!!!

<img src="./img/z8g8xg2dekk51.jpg" height="75%">

!!!

##### Binary Search

$\mathcal{O}(n)$ vs. $\mathcal{O}(\log{}n)$

$\log_2 8124 \approx 13$ <!-- .element: class="fragment" data-fragment-index="0" -->

<div class="r-stack">
<img class="fragment current-visible" data-fragment-index="1"
    src="https://codemonkeyspace.b-cdn.net/post/2021/gam/bigo.svg" width="400" height="400">
<img class="fragment" data-fragment-index="2"
    src="https://codemonkeyspace.b-cdn.net/post/2021/gam/binarysearch.svg" width="400" height="400">
</div>

!!!

<div style="font-size:18px">

```cs
public int FindBinarySearch(Span<byte> spanByte)
{
    Span<ulong> span = MemoryMarshal.Cast<byte, ulong>(spanByte);

    if (span[0] == 0)
        return 1;

    if (span[^1] == long.MaxValue)
        return -1;

    int min = 0;
    int max = span.Length - 1;
    int index = -1;

    while (min <= max)
    {
        int mid = (int)unchecked((uint)(min + max) >> 1);
        ref var b = ref span[mid];

        if (b != ulong.MaxValue)
        {
            if (mid == 0)
            {
                index = 0;
                break;
            }

            ref var b1 = ref span[mid - 1];
            if (b1 == ulong.MaxValue)
            {
                index = mid;
                break;
            }

            max = mid - 1;
            continue;
        }

        min = mid + 1;
    }

    if (index > -1)
    {
        int res = 0;
        ref var l = ref span[index];
        var count = BitOperations.LeadingZeroCount((ulong)l);
        res = (64 - count) + 1;
        if (index > 0 && res != -1)
            res += (64 * index);
        return res;
    }

    return index;
}
```

</div>

 <table class="fragment">
            <thead>
              <tr>
                <th>Method</th>
                <th>Iterations</th>
                <th style="text-align:right">Mean</th>
                <th style="text-align:right">Error</th>
                <th style="text-align:right">StdDev</th>
                <th style="text-align:right">Median</th>
                <th style="text-align:right">Ratio</th>
                <th style="text-align:right">RatioSD</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Loop</td>
                <td>1000</td>
                <td style="text-align:right">138.85 μs</td>
                <td style="text-align:right">1.539 μs</td>
                <td style="text-align:right">2.303 μs</td>
                <td style="text-align:right">138.74 μs</td>
                <td style="text-align:right">1.00</td>
                <td style="text-align:right">0.00</td>
              </tr>
              <tr>
                <td>LoopUnrolled8</td>
                <td>1000</td>
                <td style="text-align:right">77.82 μs</td>
                <td style="text-align:right">1.153 μs</td>
                <td style="text-align:right">1.690 μs</td>
                <td style="text-align:right">77.90 μs</td>
                <td style="text-align:right">0.56</td>
                <td style="text-align:right">0.02</td>
              </tr>
              <tr>
                <td>BinarySearchLike</td>
                <td>1000</td>
                <td style="text-align:right">56.73 μs</td>
                <td style="text-align:right">1.024 μs</td>
                <td style="text-align:right">1.501 μs</td>
                <td style="text-align:right">56.48 μs</td>
                <td style="text-align:right">0.41</td>
                <td style="text-align:right">0.01</td>
              </tr>
            </tbody>
          </table>

!!!

##### Lookup

<div style="font-size:18px">

```cs
int res = 0;
ref var l = ref span[index];
var count = BitOperations.LeadingZeroCount((ulong)l);
res = (64 - count) + 1;
if (index > 0 && res != -1)
    res += (64 * index);
return res;
  </code></pre>
          <pre class="fragment"><code data-trim data-noescape>
int res = 0;
ref var l = ref span[index];
res = _lookup[l];
if (index > 0 && res != -1)
    res += (64 * index);
return res;

```

</div>

<table class="fragment" style="font-size: 18px"><thead><tr><th>Method</th><th>Iterations</th><th style="text-align:right">Mean</th><th style="text-align:right">Error</th><th style="text-align:right">StdDev</th><th style="text-align:right">Median</th><th style="text-align:right">Ratio</th><th style="text-align:right">RatioSD</th><th>Baseline</th></tr></thead><tbody><tr><td>Loop</td><td>1000</td><td style="text-align:right">138.85 μs</td><td style="text-align:right">1.539 μs</td><td style="text-align:right">2.303 μs</td><td style="text-align:right">138.74 μs</td><td style="text-align:right">1.00</td><td style="text-align:right">0.00</td><td>Yes</td></tr><tr><td>BinarySearchLike</td><td>1000</td><td style="text-align:right">56.73 μs</td><td style="text-align:right">1.024 μs</td><td style="text-align:right">1.501 μs</td><td style="text-align:right">56.48 μs</td><td style="text-align:right">0.41</td><td style="text-align:right">0.01</td><td>No</td></tr><tr><td>BinarySearchLikeLookup</td><td>1000</td><td style="text-align:right">75.21 μs</td><td style="text-align:right">0.746 μs</td><td style="text-align:right">1.070 μs</td><td style="text-align:right">75.10 μs</td><td style="text-align:right">0.54</td><td style="text-align:right">0.01</td><td>No</td></tr></tbody></table>

###### ? <!-- .element: class="fragment" -->

!!!

100000 calls

<table style="font-size: 18px"><thead><tr><th>Method</th><th style="text-align:right">Mean</th><th style="text-align:right">Error</th><th style="text-align:right">StdDev</th><th style="text-align:right">Median</th><th>Baseline</th></tr></thead><tbody><tr><td>Computation</td><td style="text-align:right">49.71 μs</td><td style="text-align:right">0.449 μs</td><td style="text-align:right">0.630 μs</td><td style="text-align:right">49.56 μs</td><td>No</td></tr><tr><td>Lookup</td><td style="text-align:right">570.87 μs</td><td style="text-align:right">7.044 μs</td><td style="text-align:right">10.102 μs</td><td style="text-align:right">568.72 μs</td><td>No</td></tr></tbody></table>

!!!

##### Span.IndexOf

<div style="font-size:18px">

```cs
public int IndexOfSearch(Span<byte> spanByte)
{
    Span<ulong> span = MemoryMarshal.Cast<byte, ulong>(spanByte);

    if (span[0] == 0)
        return 1;

    if (span[^1] == long.MaxValue)
        return -1;

    int index = -1;

    var firstZero = span.IndexOf(0u);
    if (firstZero > 0)
    {
        // check prev
        if (span[firstZero - 1] == ulong.MaxValue)
            index = firstZero;
        else
            index = firstZero - 1;
    }
    else
    {
        index = span.Length - 1;
    }

    if (index > -1)
    {
        int res = 0;
        ref var l = ref span[index];
        var count = BitOperations.LeadingZeroCount((ulong)l);
        res = (64 - count) + 1;
        if (index > 0 && res != -1)
            res += (64 * index);
        return res;
    }

    return index;
}

```

</div>

<table class="fragment" style="font-size: 18px"><thead><tr><th>Method</th><th>Iterations</th><th style="text-align:right">Mean</th><th style="text-align:right">Error</th><th style="text-align:right">StdDev</th><th style="text-align:right">Median</th><th style="text-align:right">Ratio</th><th style="text-align:right">RatioSD</th><th>Baseline</th></tr></thead><tbody><tr><td>Loop</td><td>1000</td><td style="text-align:right">138.85 μs</td><td style="text-align:right">1.539 μs</td><td style="text-align:right">2.303 μs</td><td style="text-align:right">138.74 μs</td><td style="text-align:right">1.00</td><td style="text-align:right">0.00</td><td>Yes</td></tr><tr><td>BinarySearchLike</td><td>1000</td><td style="text-align:right">56.73 μs</td><td style="text-align:right">1.024 μs</td><td style="text-align:right">1.501 μs</td><td style="text-align:right">56.48 μs</td><td style="text-align:right">0.41</td><td style="text-align:right">0.01</td><td>No</td></tr><tr><td>IndexOf</td><td>1000</td><td style="text-align:right">138.47 μs</td><td style="text-align:right">1.586 μs</td><td style="text-align:right">2.324 μs</td><td style="text-align:right">138.66 μs</td><td style="text-align:right">1.00</td><td style="text-align:right">0.03</td><td>No</td></tr></tbody></table>

!!!

##### Vector

<div style="font-size:18px">

```cs

public int FindFirstEmptyVectorT(Span<byte> span)
{
    int iterations = Math.DivRem(span.Length, Vector<byte>.Count, out int nonAligned);
    for (int i = 0; i < iterations; i++)
    {
        var vector = new Vector<byte>(span[(Vector<byte>.Count * i)..]);
        if (vector != _testVector)
        {
            int bitIndex = Vector<byte>.Count * i * 8;
            var u64vector = Vector.AsVectorUInt64(vector); // handle LZC with uiint here
            for (int j = 0; j < Vector<ulong>.Count; j++)
            {
                var l = u64vector[j];
                if (l == ulong.MaxValue)
                    continue;

                int res = 0;
                var count = BitOperations.LeadingZeroCount((ulong)l);
                res = (64 - count) + 1;
                if (j > 0 && res != -1)
                    res += (64 * j);
                return res + bitIndex;
            }
        }
    }

    return -1;
}
```

</div>

<table class="fragment" style="font-size: 18px"><thead><tr><th>Method</th><th>Iterations</th><th style="text-align:right">Mean</th><th style="text-align:right">Error</th><th style="text-align:right">StdDev</th><th style="text-align:right">Median</th><th style="text-align:right">Ratio</th><th style="text-align:right">RatioSD</th><th>Baseline</th></tr></thead><tbody><tr><td>Loop</td><td>1000</td><td style="text-align:right">138.85 μs</td><td style="text-align:right">1.539 μs</td><td style="text-align:right">2.303 μs</td><td style="text-align:right">138.74 μs</td><td style="text-align:right">1.00</td><td style="text-align:right">0.00</td><td>Yes</td></tr><tr><td>BinarySearchLike</td><td>1000</td><td style="text-align:right">56.73 μs</td><td style="text-align:right">1.024 μs</td><td style="text-align:right">1.501 μs</td><td style="text-align:right">56.48 μs</td><td style="text-align:right">0.41</td><td style="text-align:right">0.01</td><td>No</td></tr><tr><td>WithVector</td><td>1000</td><td style="text-align:right">155.86 μs</td><td style="text-align:right">2.147 μs</td><td style="text-align:right">3.147 μs</td><td style="text-align:right">155.26 μs</td><td style="text-align:right">1.12</td><td style="text-align:right">0.04</td><td>No</td></tr></tbody></table>

!!!

##### “Octuple”

<div style="font-size:18px">

```cs
public unsafe int FindFirstOctupleSearchOnce(Span<byte> spanByte)
{
    Span<uint> span = MemoryMarshal.Cast<byte, uint>(spanByte);
    fixed (uint* spanPtr = span)
    {
        const int bitsPerInt = 32;
        const int ints = 8128 / 4;
        const int loadCount = 8;
        const int sections = loadCount + 1;
        const int sectionLength = (ints / sections) + 1; // 225.8 -> 226
        var indexes = Vector256.Create(
            sectionLength * 1,
            sectionLength * 2,
            sectionLength * 3,
            sectionLength * 4,
            sectionLength * 5,
            sectionLength * 6,
            sectionLength * 7,
            sectionLength * 8);

        int lowerBound = OctupleSearchLowerBound(spanPtr, indexes, sectionLength);
        int index = lowerBound * bitsPerInt;

        int upperBound = Math.Min(lowerBound + sectionLength + 1, span.Length);
        for (int i = lowerBound; i < upperBound; i++)
        {
            int bitsSet = BitOperations.PopCount(span[i]);
            index += bitsSet;
            if (bitsSet != bitsPerInt)
            {
                break;
            }
        }

        return index + 1;
    }
}

public unsafe int OctupleSearchLowerBound(uint* spanPtr, Vector256<int> indexes, int sectionLength)
{
    //Load 8 indexes at once into a Vector256
    var values = Avx2.GatherVector256(spanPtr, indexes, (byte)sizeof(int));

    //How many loaded values have all bits set?
    //If true then set to 0xffffffff else 0
    var isMaxValue = Avx2.CompareEqual(values, Vector256<uint>.AllBitsSet);

    //Take msb of each 32bit element and return them as an int.
    //Then count number of bits that are set and that is equals
    //to the number of loaded values that were all ones.
    var isMaxValueMask = Avx2.MoveMask(isMaxValue.AsSingle());
    var isMaxCount = BitOperations.PopCount((uint)isMaxValueMask);

    //For each loaded vaue that's all ones, a sectionLength
    //number of integers must also be all ones
    return isMaxCount * sectionLength;
}
```

</div>

<table class="fragment" style="font-size: 18px"><thead><tr><th>Method</th><th>Iterations</th><th style="text-align:right">Mean</th><th style="text-align:right">Error</th><th style="text-align:right">StdDev</th><th style="text-align:right">Median</th><th style="text-align:right">Ratio</th><th style="text-align:right">RatioSD</th><th>Baseline</th></tr></thead><tbody><tr><td>Loop</td><td>1000</td><td style="text-align:right">138.85 μs</td><td style="text-align:right">1.539 μs</td><td style="text-align:right">2.303 μs</td><td style="text-align:right">138.74 μs</td><td style="text-align:right">1.00</td><td style="text-align:right">0.00</td><td>Yes</td></tr><tr><td>BinarySearchLike</td><td>1000</td><td style="text-align:right">56.73 μs</td><td style="text-align:right">1.024 μs</td><td style="text-align:right">1.501 μs</td><td style="text-align:right">56.48 μs</td><td style="text-align:right">0.41</td><td style="text-align:right">0.01</td><td>No</td></tr><tr><td>OctupleSearch</td><td>1000</td><td style="text-align:right">94.76 μs</td><td style="text-align:right">1.404 μs</td><td style="text-align:right">2.102 μs</td><td style="text-align:right">94.30 μs</td><td style="text-align:right">0.68</td><td style="text-align:right">0.02</td><td>No</td></tr></tbody></table>

!!!

<div>
<img src="https://codemonkeyspace.b-cdn.net/post/2021/gam/Find%20first%20zero%20bit.svg" style="background-color: cornsilk;">
</div>