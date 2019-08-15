﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Worker
{
    static class NumaNode
    {
        [DllImport("Kernel32.dll")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool GetNumaHighestNodeNumber([Out] out uint HighestNodeNumber);

        public static readonly int NumaCount;
        public static readonly int UsableCoreCount;
        static int CurrentNuma;

        static NumaNode()
        {
            GetNumaHighestNodeNumber(out uint temp);
            CurrentNuma = (int)temp;
            NumaCount = CurrentNuma + 1;
            UsableCoreCount = Environment.ProcessorCount;
        }

        public static int NextNuma()
        {
            int res = CurrentNuma;
            CurrentNuma = (CurrentNuma - 1 + NumaCount) % NumaCount;
            return res;
        }

        public static int PrevNuma()
        {
            int res = CurrentNuma;
            CurrentNuma = (CurrentNuma + 1) % NumaCount;
            return res;
        }

        public static string X265PoolsParam(int currentNuma)
        {
            string[] res = Enumerable.Repeat("-", NumaCount).ToArray();
            res[currentNuma] = "+";
            return string.Join(",", res);
        }
    }
}
