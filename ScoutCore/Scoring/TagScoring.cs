using Digestor;
using ScoutCore.SpectraOperations;
using SpectrumWizard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutCore.Scoring
{
    public static class TagScoring
    {
        public static double CalculateTagScore(Peptide IgDcniCg, List<(double oRRlXpQU, double PTueahjc)> fRaMkGSB, (List<(int SDfqRroc, List<AnnotationIon> UJpBsIGP)> dKwNaluN, List<(int buNUABfj, List<AnnotationIon> pBZznIeD)> SiREMijF) PKRrhHzA, double VvcSLuSQ, out double uKpNYeWh, out int KppckVbS, out int FtLEXnJm)
        {
            int WFDTghhV;
            int IgtfrXbr;
            List<(int, int, double)> tags = GetTags(fRaMkGSB, PKRrhHzA.dKwNaluN, VvcSLuSQ, 3, out WFDTghhV, out IgtfrXbr);
            int WFDTghhV2;
            int IgtfrXbr2;
            List<(int, int, double)> tags2 = GetTags(fRaMkGSB, PKRrhHzA.SiREMijF, VvcSLuSQ, 3, out WFDTghhV2, out IgtfrXbr2);
            uKpNYeWh = tags.Concat(tags2).Sum<(int, int, double)>(((int number, int count, double summedIntensity) a) => a.summedIntensity) / fRaMkGSB.Sum<(double, double)>(((double MZ, double Intensity) a) => a.Intensity);
            KppckVbS = WFDTghhV + WFDTghhV2;
            FtLEXnJm = Math.Max(IgtfrXbr, IgtfrXbr2);
            return tags.Concat(tags2).Sum<(int, int, double)>(((int number, int count, double summedIntensity) a) => a.count);
        }

        public static List<(int RMgBcbWW, int LWfXqnGa, double IcjwIhBe)> GetTags(List<(double OiKrrnuG, double vOURHyqX)> mctWnkeU, List<(int ybvfGJWG, List<AnnotationIon> yfTUZNDn)> lvLzTtEW, double NVbXAEWq, int SpfFVmcQ, out int WFDTghhV, out int IgtfrXbr)
        {
            List<(double MZ, double Intensity)> list = mctWnkeU;
            List<(int, int, double)> list2 = new List<(int, int, double)>();
            if (list.Count == 0)
            {
                WFDTghhV = 0;
                IgtfrXbr = 0;
                return list2;
            }

            list.Max(((double MZ, double Intensity) a) => a.Intensity);
            int num = 0;
            int num2 = 0;
            WFDTghhV = 0;
            double num3 = 0.0;
            foreach (var item in lvLzTtEW)
            {
                IEnumerable<int?> source = item.yfTUZNDn.Select((AnnotationIon P_0) => PeakSearching.BinarySearchForMZ(list, P_0.MZ, 20.0));
                if (source.Any((int? P_0) => P_0.HasValue))
                {
                    WFDTghhV++;
                    num3 += source.Where((int? P_0) => P_0.HasValue).Sum((int? P_0) => list[P_0.Value].Intensity);
                    num++;
                    if (num2 == 0)
                    {
                        (num2, _) = item;
                    }
                }
                else if (num > SpfFVmcQ)
                {
                    list2.Add((num2, num, num3));
                    num = 0;
                    num2 = 0;
                    num3 = 0.0;
                }
            }

            if (num > SpfFVmcQ)
            {
                list2.Add((num2, num, num3));
            }

            if (list2.Count != 0)
            {
                IgtfrXbr = list2.Max<(int, int, double)>(((int number, int count, double tagSummedIntensity) a) => a.count);
            }
            else
            {
                IgtfrXbr = 0;
            }

            return list2;
        }
    }
}
