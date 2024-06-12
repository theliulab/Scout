using Accord.Statistics.Distributions.Univariate;
using Mpfr;
using ScoutCore;
using ScoutCore.PSMEngines;
using PatternTools.MSParserLight;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using Mpfr.Structs;

namespace ScoutCore.Scoring
{
    public static class PoissonScores
    {
        static double _00A0(double P_0)
        {
            if (P_0 == 0.0)
            {
                return 1.0;
            }

            return P_0 * _00A0(P_0 - 1.0);
        }

        public static double GetPValueMPFR(
            double HGCjQHJS,
            double LHJMzByU,
            double HiSZJzQE,
            double BaBHbFoT,
            double bmrTFUNS,
            double kTbzgoQq,
            int WpdCVmhm,
            uint JQGJiURx = 500,
            Mpfr.Structs.mpfr_rnd_t SPfQPMPg = Mpfr.Structs.mpfr_rnd_t.GMP_RNDN)
        {
            MpfrNumber.DefaultPrecision = JQGJiURx;
            MpfrNumber.DefaultRnd = SPfQPMPg;


            MpfrNumber _x_mpfr = new MpfrNumber();
            _x_mpfr.Assign(bmrTFUNS * kTbzgoQq * 2);
            _x_mpfr.Divide(111.1);

            MpfrNumber _f_x_mpfr = new MpfrNumber();
            _f_x_mpfr.Assign(HGCjQHJS * HiSZJzQE);
            _f_x_mpfr.Divide(HGCjQHJS + LHJMzByU);
            //f=f * x;
            _f_x_mpfr.Multiply(_x_mpfr);
            //f = f * x => f = f * x * -1
            _f_x_mpfr.Multiply(-1);

            //double left = (double)Math.Pow(Math.E, -1 * x * f);
            MpfrNumber _left_mpfr = new MpfrNumber();
            _left_mpfr.Assign(1.0);
            //_left_mpfr.SetE(); //0.577
            _left_mpfr.SetEPow(_left_mpfr);
            _left_mpfr.Power(_f_x_mpfr);

            //f=f * -1; => f * x is positive
            _f_x_mpfr.Multiply(-1);

            MpfrNumber _sum_mpfr = new MpfrNumber();
            _sum_mpfr.Assign("0");

            for (int count = 0; count < BaBHbFoT; count++)
            {
                MpfrNumber _fact_mpfr = new MpfrNumber();
                _fact_mpfr.Fac((uint)count);

                MpfrNumber _pow_mpfr = new MpfrNumber();
                _pow_mpfr.Assign(_f_x_mpfr);
                _pow_mpfr.Power(count);
                _pow_mpfr.Divide(_fact_mpfr);
                _sum_mpfr.Add(_pow_mpfr);
            }

            MpfrNumber _sum_mpfr_3 = new MpfrNumber();
            _sum_mpfr_3.Assign(_sum_mpfr);
            _sum_mpfr_3.Multiply(_left_mpfr);

            MpfrNumber _p_value_true = new MpfrNumber();
            _p_value_true.Assign("1");
            _p_value_true.Subtract(_sum_mpfr_3);
            _p_value_true.Multiply(WpdCVmhm);

            return _p_value_true.ToDouble();
        }

        public static double GetPValue(double tehCevVg, double IoOrfwOK, int aznUqsEl)
        {
            double num = _00A0(IoOrfwOK * tehCevVg, aznUqsEl);
            return 1.0 - num;
        }

        public static double GetPValueMPFRLog(double L2eD, double R1Tgb, double i98Nsa, double p3Ahjx4, double f4rghz, double b7rtcS, uint p9JcxdeA = 500, Mpfr.Structs.mpfr_rnd_t trDcmv = Mpfr.Structs.mpfr_rnd_t.GMP_RNDN)
        {
            MpfrNumber.DefaultPrecision = p9JcxdeA;
            MpfrNumber.DefaultRnd = trDcmv;

            //lambda = x * f = (2*frags*bin/111.1)*(ions*la/(la+lb))
            MpfrNumber lambda = new MpfrNumber();
            lambda.Assign(2.0 * f4rghz * b7rtcS * i98Nsa * L2eD);
            lambda.Divide(111.1 * (L2eD + R1Tgb));

            MpfrNumber logLambda = new MpfrNumber();
            logLambda.Assign(lambda);
            logLambda.Ln();

            MpfrNumber sum = new();
            sum.Assign(0);
            for (int i = 0; i <= p3Ahjx4; i++)
            {
                //double factLog = FactorialLog(i);
                MpfrNumber factLog = new();
                factLog.Assign(i + 1);
                factLog.SetGamma(factLog);
                factLog.Ln();

                //double inner = i * logLambda - lambda - factLog;
                MpfrNumber inner = new();
                inner.Assign(i);
                inner.Multiply(logLambda);
                inner.Subtract(lambda);
                inner.Subtract(factLog);

                inner.SetEPow(inner);
                sum.Add(inner);
            }

            MpfrNumber p = new();
            p.Assign(1);
            p.Subtract(sum);

            return p.ToDouble();
        }

        private static double _00A0(double P_0, double P_1)
        {
            double num = Math.Log(P_0);
            double num2 = 0.0;
            for (int i = 0; (double)i <= P_1; i++)
            {
                double num3 = _00A0(i);
                num3 = Math.Log(Gamma.Function((double)(i + 1)));
                double d = (double)i * num - P_0 - num3;
                num2 += Math.Exp(d);
            }

            return num2;
        }

        private static double _00A0(int P_0)
        {
            if (P_0 == 0)
            {
                return Math.Log(1.0);
            }

            return (from P_A in Enumerable.Range(1, P_0)
                    select Math.Log(P_A)).Sum();
        }

        public static (double iwcvWjiK, double uptHvVvr) CalculateSuggestedScore(CleavePSM HzdUFSHU, int LCgwKhLm, CleavePSM FBmNPhht, int DEamwufu, int WCBlNbIw, int XEyMbxSm, double ZqOOkbCV = 8.0, double kbtWwJkq = 0.02)
        {
            double DfRtGh = HzdUFSHU.Peptide.Length;
            double TrEfGbHm = FBmNPhht.Peptide.Length;
            double ArfGtrX = GetPValueMPFR(DfRtGh, TrEfGbHm, WCBlNbIw, (HzdUFSHU.Peptide.ResidueTuples != FBmNPhht.Peptide.ResidueTuples) ? LCgwKhLm : (LCgwKhLm / 2), ZqOOkbCV, kbtWwJkq, XEyMbxSm, 500u, (mpfr_rnd_t)0);
            double d = ((HzdUFSHU.Peptide != FBmNPhht.Peptide) ? GetPValueMPFR(TrEfGbHm, DfRtGh, WCBlNbIw, (HzdUFSHU.Peptide.ResidueTuples != FBmNPhht.Peptide.ResidueTuples) ? DEamwufu : (DEamwufu / 2), ZqOOkbCV, kbtWwJkq, XEyMbxSm, 500u, (mpfr_rnd_t)0) : ArfGtrX);
            if (!(ArfGtrX <= 0.0))
            {
                _ = 0.0;
            }

            if (ArfGtrX != double.PositiveInfinity)
            {
                _ = double.PositiveInfinity;
            }

            double num3 = -1.0 * Math.Log10(ArfGtrX);
            double num4 = -1.0 * Math.Log10(d);
            if (!double.IsNaN(num3) && !double.IsNaN(num4) && !double.IsInfinity(num3))
            {
                double.IsInfinity(num4);
            }

            if (num3 <= 0.0 || num4 <= 0.0)
            {
                num3 = 0.0;
                num4 = 0.0;
            }

            return (num3, num4);
        }

        public static (double nUsRczFh, double rKwGekCY) CalculateSuggestedScore(List<(double fWtHgDew, double cvZpDBip)> Mqozvnpe, CleaveAnnotationSpectrum KwdHmIMj, int BzIlJXji, double IqKNtcCl, double jdUyjGKt, int tMEkYRpK)
        {
            ValueTuple<char, double>[] alpha = KwdHmIMj.Alpha;
            (char, double)[] beta = KwdHmIMj.Beta;
            List<AnnotationIon> alphaIons = KwdHmIMj.AlphaIons;
            List<AnnotationIon> betaIons = KwdHmIMj.BetaIons;
            double num = alpha.Length;
            double num2 = beta.Length;
            double num3 = GetPValueMPFR(BaBHbFoT: MatchingHelper.GetMatches(Mqozvnpe, alphaIons.OrderBy((AnnotationIon P_0) => P_0.MZ).ToList(), jdUyjGKt, false).Count(), HGCjQHJS: num, LHJMzByU: num2, HiSZJzQE: Mqozvnpe.Count, bmrTFUNS: BzIlJXji, kTbzgoQq: IqKNtcCl, WpdCVmhm: tMEkYRpK, JQGJiURx: 500u, SPfQPMPg: (mpfr_rnd_t)0);
            double d;
            if (alpha == beta)
            {
                d = num3;
            }
            else
            {
                int num4 = MatchingHelper.GetMatches(Mqozvnpe, betaIons.OrderBy((AnnotationIon P_0) => P_0.MZ).ToList(), jdUyjGKt, false).Count();
                d = GetPValueMPFR(num2, num, Mqozvnpe.Count, num4, BzIlJXji, IqKNtcCl, tMEkYRpK, 500u, (mpfr_rnd_t)0);
            }

            double num5 = -1.0 * Math.Log10(num3);
            double num6 = -1.0 * Math.Log10(d);
            if (!double.IsNaN(num5) && !double.IsNaN(num6) && !double.IsInfinity(num5))
            {
                double.IsInfinity(num6);
            }

            if (num5 <= 0.0 || num6 <= 0.0)
            {
                num5 = 0.0;
                num6 = 0.0;
            }
            else
            {
                _ = 5.0;
            }

            return (num5, num6);
        }
    }
}
