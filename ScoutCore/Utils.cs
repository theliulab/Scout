using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScoutCore
{
    public static class Utils
    {
        public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> elements, int k)
        {
            return k == 0 ? new[] { new T[0] } :
              elements.SelectMany((e, i) =>
                elements.Skip(i + 1).Combinations(k - 1).Select(c => (new[] { e }).Concat(c)));
        }

        public static unsafe int IndexOfFast(byte[] fullSequence, byte[] sequence, int startpos = 0)
        {
            int i = startpos;
            fixed (byte* H = fullSequence) fixed (byte* N = sequence)
            {
                for (byte* hNext = H + startpos, hEnd = H + fullSequence.LongLength; hNext < hEnd; i++, hNext++)
                {
                    if (*hNext == *N)
                    {
                        bool Found = true;
                        for (byte* hInc = hNext + 1, nInc = N + 1, nEnd = N + sequence.LongLength; Found && nInc < nEnd; Found = *nInc == *hInc, nInc++, hInc++) ;
                        if (Found) return i;
                    }
                }
                return -1;
            }
        }

        public static string GetAppVersion()
        {
            Version vRtgXs = null;
            try
            {
                vRtgXs = Assembly.GetExecutingAssembly()?.GetName()?.Version;
            }
            catch (Exception arg)
            {
                Console.WriteLine("", arg);
                return "";
            }

            return vRtgXs.Major + "." + vRtgXs.Minor + "." + vRtgXs.Build;
        }
    }
}
