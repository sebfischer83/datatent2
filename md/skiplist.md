## Skiplist

!!!

- probalistische Datenstruktur
- "ordered linked list"
- Layout auf Festplatte einfacher als Trees

!!!

<img src="https://upload.wikimedia.org/wikipedia/commons/2/2c/Skip_list_add_element-en.gif" style="background-color: cornsilk;">

!!!

##### Insert

<table class="fragment" style="font-size: 18px">
    <tr>
        <td>Method</td>
        <td>Count</td>
        <td>Mean</td>
        <td>Error</td>
        <td>StdDev</td>
        <td>Median</td>
        <td>Kurtosis</td>
        <td>Skewness</td>
        <td>Rank</td>
        <td>Baseline</td>
        <td>Gen 0</td>
        <td>Gen 1</td>
        <td>Allocated</td>
    </tr>
    <tr>
        <td>AddBenchmark</td>
        <td>100</td>
        <td>261.2 us</td>
        <td>2.70 us</td>
        <td>4.04 us</td>
        <td>261.2 us</td>
        <td>2.088</td>
        <td>-0.0964</td>
        <td>1</td>
        <td>No</td>
        <td>78.1250</td>
        <td>2.9297</td>
        <td>639 KB</td>
    </tr>
    <tr>
        <td>AddBenchmark</td>
        <td>500</td>
        <td>1,720.8 us</td>
        <td>12.21 us</td>
        <td>18.27 us</td>
        <td>1,721.0 us</td>
        <td>2.087</td>
        <td>-0.0642</td>
        <td>2</td>
        <td>No</td>
        <td>511.7188</td>
        <td>44.9219</td>
        <td>4,189 KB</td>
    </tr>
    <tr>
        <td>AddBenchmark</td>
        <td>1000</td>
        <td>3,786.0 us</td>
        <td>28.78 us</td>
        <td>41.27 us</td>
        <td>3,771.4 us</td>
        <td>1.600</td>
        <td>0.4965</td>
        <td>3</td>
        <td>No</td>
        <td>1148.4375</td>
        <td>160.1563</td>
        <td>9,388 KB</td>
    </tr>
    <tr>
        <td>AddBenchmark</td>
        <td>2500</td>
        <td>10,552.8 us</td>
        <td>65.62 us</td>
        <td>92.00 us</td>
        <td>10,582.3 us</td>
        <td>2.763</td>
        <td>-0.8859</td>
        <td>4</td>
        <td>No</td>
        <td>3296.8750</td>
        <td>31.2500</td>
        <td>26,988 KB</td>
    </tr>
    <tr>
        <td>AddBenchmark</td>
        <td>5000</td>
        <td>23,183.8 us</td>
        <td>277.25 us</td>
        <td>406.39 us</td>
        <td>23,065.1 us</td>
        <td>3.737</td>
        <td>1.0937</td>
        <td>5</td>
        <td>No</td>
        <td>7187.5000</td>
        <td>62.5000</td>
        <td>58,743 KB</td>
    </tr>
    <tr>
        <td>AddBenchmark</td>
        <td>7500</td>
        <td>36,208.0 us</td>
        <td>447.07 us</td>
        <td>669.16 us</td>
        <td>36,138.6 us</td>
        <td>2.172</td>
        <td>-0.1529</td>
        <td>6</td>
        <td>No</td>
        <td>11357.1429</td>
        <td>71.4286</td>
        <td>92,822 KB</td>
    </tr>
    <tr>
        <td>AddBenchmark</td>
        <td>10000</td>
        <td>50,210.7 us</td>
        <td>478.14 us</td>
        <td>685.73 us</td>
        <td>50,092.0 us</td>
        <td>2.940</td>
        <td>-0.0535</td>
        <td>7</td>
        <td>No</td>
        <td>15400.0000</td>
        <td>200.0000</td>
        <td>126,023 KB</td>
    </tr>
</table>
!!!

##### Search

<table class="fragment" style="font-size: 18px">
    <tr>
        <td>Method</td>
        <td>Count</td>
        <td>Mean</td>
        <td>Error</td>
        <td>StdDev</td>
        <td>Median</td>
        <td>Kurtosis</td>
        <td>Skewness</td>
        <td>Rank</td>
        <td>Baseline</td>
        <td>Gen 0</td>
        <td>Allocated</td>
    </tr>
     <tr>
        <td>FindSkipList</td>
        <td>100</td>
        <td>3,150.9 ns</td>
        <td>289.93 ns</td>
        <td>415.81 ns</td>
        <td>3,183.9 ns</td>
        <td>0.9579</td>
        <td>-0.0163</td>
        <td>3</td>
        <td>No</td>
        <td>0.5875</td>
        <td>4,928 B</td>
    </tr>
    <tr>
        <td>FindHeap</td>
        <td>100</td>
        <td>573.8 ns</td>
        <td>18.11 ns</td>
        <td>26.54 ns</td>
        <td>559.1 ns</td>
        <td>1.0523</td>
        <td>0.0650</td>
        <td>2</td>
        <td>No</td>
        <td>0.1278</td>
        <td>1,072 B</td>
    </tr>
    <tr>
        <td>FindSkipList</td>
        <td>500</td>
        <td>3,500.2 ns</td>
        <td>357.53 ns</td>
        <td>524.06 ns</td>
        <td>3,778.4 ns</td>
        <td>1.3536</td>
        <td>0.1460</td>
        <td>4</td>
        <td>No</td>
        <td>0.4845</td>
        <td>4,064 B</td>
    </tr>
    <tr>
        <td>FindHeap</td>
        <td>500</td>
        <td>7,117.1 ns</td>
        <td>382.78 ns</td>
        <td>548.98 ns</td>
        <td>7,165.0 ns</td>
        <td>0.9937</td>
        <td>-0.0208</td>
        <td>9</td>
        <td>No</td>
        <td>1.4572</td>
        <td>12,208 B</td>
    </tr>
    <tr>
        <td>FindSkipList</td>
        <td>1000</td>
        <td>4,269.1 ns</td>
        <td>186.96 ns</td>
        <td>279.83 ns</td>
        <td>4,307.5 ns</td>
        <td>2.3906</td>
        <td>-0.0935</td>
        <td>5</td>
        <td>No</td>
        <td>0.6332</td>
        <td>5,328 B</td>
    </tr>
    <tr>
        <td>FindHeap</td>
        <td>1000</td>
        <td>5,555.2 ns</td>
        <td>3,406.14 ns</td>
        <td>5,098.15 ns</td>
        <td>5,465.0 ns</td>
        <td>0.9360</td>
        <td>0.0011</td>
        <td>8</td>
        <td>No</td>
        <td>0.1087</td>
        <td>912 B</td>
    </tr>
    <tr>
        <td>FindSkipList</td>
        <td>2500</td>
        <td>4,432.5 ns</td>
        <td>75.72 ns</td>
        <td>108.60 ns</td>
        <td>4,437.6 ns</td>
        <td>1.6008</td>
        <td>-0.1133</td>
        <td>6</td>
        <td>No</td>
        <td>0.7706</td>
        <td>6,480 B</td>
    </tr>
    <tr>
        <td>FindHeap</td>
        <td>2500</td>
        <td>22,786.0 ns</td>
        <td>9,802.89 ns</td>
        <td>14,368.95 ns</td>
        <td>10,011.7 ns</td>
        <td>0.9460</td>
        <td>0.0701</td>
        <td>10</td>
        <td>No</td>
        <td>8.1787</td>
        <td>68,912 B</td>
    </tr>
    <tr>
        <td>FindSkipList</td>
        <td>5000</td>
        <td>5,640.0 ns</td>
        <td>94.82 ns</td>
        <td>132.93 ns</td>
        <td>5,608.7 ns</td>
        <td>2.7098</td>
        <td>0.0893</td>
        <td>8</td>
        <td>No</td>
        <td>1.0529</td>
        <td>8,816 B</td>
    </tr>
    <tr>
        <td>FindHeap</td>
        <td>5000</td>
        <td>63,669.9 ns</td>
        <td>15,629.29 ns</td>
        <td>22,909.21 ns</td>
        <td>72,775.0 ns</td>
        <td>1.3791</td>
        <td>0.2516</td>
        <td>11</td>
        <td>No</td>
        <td>16.6016</td>
        <td>139,024 B</td>
    </tr>
    <tr>
        <td>FindSkipList</td>
        <td>7500</td>
        <td>5,146.0 ns</td>
        <td>150.27 ns</td>
        <td>200.61 ns</td>
        <td>5,158.7 ns</td>
        <td>1.3450</td>
        <td>0.0728</td>
        <td>7</td>
        <td>No</td>
        <td>0.8163</td>
        <td>6,864 B</td>
    </tr>
    <tr>
        <td>FindHeap</td>
        <td>7500</td>
        <td>76,158.2 ns</td>
        <td>14,496.41 ns</td>
        <td>21,248.64 ns</td>
        <td>57,590.7 ns</td>
        <td>0.9421</td>
        <td>0.0647</td>
        <td>12</td>
        <td>No</td>
        <td>22.2168</td>
        <td>186,832 B</td>
    </tr>
    <tr>
        <td>FindSkipList</td>
        <td>10000</td>
        <td>5,499.1 ns</td>
        <td>265.32 ns</td>
        <td>388.90 ns</td>
        <td>5,199.9 ns</td>
        <td>0.9703</td>
        <td>0.0639</td>
        <td>8</td>
        <td>No</td>
        <td>0.8621</td>
        <td>7,248 B</td>
    </tr>
    <tr>
        <td>FindHeap</td>
        <td>10000</td>
        <td>124,275.6 ns</td>
        <td>5,785.76 ns</td>
        <td>8,480.69 ns</td>
        <td>130,548.2 ns</td>
        <td>0.9795</td>
        <td>-0.0386</td>
        <td>13</td>
        <td>No</td>
        <td>26.6113</td>
        <td>223,024 B</td>
    </tr>
    <tr>
        <td>FindSkipList</td>
        <td>25000</td>
        <td>6,536.1 ns</td>
        <td>603.23 ns</td>
        <td>884.20 ns</td>
        <td>5,769.2 ns</td>
        <td>0.9444</td>
        <td>0.0620</td>
        <td>9</td>
        <td>No</td>
        <td>0.9842</td>
        <td>8,240 B</td>
    </tr>
    <tr>
        <td>FindHeap</td>
        <td>25000</td>
        <td>271,767.1 ns</td>
        <td>88,444.03 ns</td>
        <td>129,640.11 ns</td>
        <td>388,378.0 ns</td>
        <td>0.9381</td>
        <td>-0.0644</td>
        <td>14</td>
        <td>No</td>
        <td>89.8438</td>
        <td>754,576 B</td>
    </tr>
    <tr>
        <td>FindSkipList</td>
        <td>50000</td>
        <td>6,954.1 ns</td>
        <td>38.14 ns</td>
        <td>55.91 ns</td>
        <td>6,953.7 ns</td>
        <td>2.0499</td>
        <td>0.0746</td>
        <td>9</td>
        <td>No</td>
        <td>1.1368</td>
        <td>9,568 B</td>
    </tr>
    <tr>
        <td>FindHeap</td>
        <td>50000</td>
        <td>474,376.5 ns</td>
        <td>96,070.55 ns</td>
        <td>140,818.97 ns</td>
        <td>600,416.4 ns</td>
        <td>0.9415</td>
        <td>-0.0656</td>
        <td>15</td>
        <td>No</td>
        <td>70.3125</td>
        <td>590,544 B</td>
    </tr>
</table>