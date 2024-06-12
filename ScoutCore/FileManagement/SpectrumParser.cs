using CSMSL.IO;
using CSMSL.IO.MzML;
using CSMSL.Spectral;
using MoreLinq.Extensions;
using Newtonsoft.Json.Linq;
using PatternTools.MSParserLight;
using PatternTools.YADA;
using ScoutCore.SpectraOperations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using timsTofBrukerReader;
using YCore;
using static ScoutCore.FileManagement.SpectrumParser;

namespace ScoutCore.FileManagement
{
    public class SpectrumParser
    {
        const int FRAME_STEP = 110;

        [StructLayout(LayoutKind.Sequential)]
        public struct ScanNumbers
        {
            public int size;
            public IntPtr data;
        }

        [DllImport("timsTofBrukerWrapper.dll")]
        public static extern MassSpectra GetMS(string fileName, int start_frame, [In] ref ScanNumbers attribute);
        [DllImport("timsTofBrukerWrapper.dll")]
        public static extern MassSpectra GetMSMS(string fileName, int start_frame, [In] ref ScanNumbers attribute);

        [DllImport("timsTofBrukerWrapper.dll")]
        public static extern bool TryGetTotalFrames(string fileName, int msLevel, out int itemsCount);
        public static List<string> GetFilesFromDirectory(string directory, string extension = ".raw", bool recursive = true)
        {
            if (recursive)
                return Directory.GetFiles(directory, $"*{extension}", SearchOption.AllDirectories).ToList();
            else
                return Directory.GetFiles(directory, $"*{extension}", SearchOption.TopDirectoryOnly).ToList();
        }

        public static List<MSUltraLight> Parse(
            CancellationToken token,
            string filePath,
            bool toPrint_console = true,
            int msLevel = 2,
            int? maxNumIons = null,
            List<int> desiredScans = null)
        {
            if (Path.GetExtension(filePath.ToLower()) == ".raw")
            {
                return ParseRaw(filePath,
                                msLevel,
                                maxNumIons,
                                desiredScans);
            }
            else if (Path.GetExtension(filePath.ToLower()) == ".mgf")
            {
                return ParseMgf(token,
                                filePath,
                                maxNumIons,
                                desiredScans);
            }
            else if (Path.GetExtension(filePath.ToLower()) == ".ms2")
            {
                return ParseMS2(token, filePath);
            }
            else if (Path.GetExtension(filePath.ToLower()) == ".mzml")
            {
                return ParseMzML(token, 
                                filePath,
                                desiredScans);
            }
            else if (Path.GetExtension(filePath.ToLower()) == ".d")
            {
                return ParseBruker(token, filePath, msLevel, desiredScans);
            }

            throw new Exception("Unknown file format: " + Path.GetExtension(filePath));
        }

        public static List<MSUltraLight> ParseBruker(
            CancellationToken token,
            string filePath,
            int msLevel = 2,
            List<int> desiredScans = null)
        {
            ScanNumbers scanNumbers = new ScanNumbers();
            if (desiredScans != null && desiredScans.Count > 0)
                scanNumbers = m_A005(desiredScans);

            List<MassSpectrum> all_spectra = new();

            int old_progress = 0;
            int totalFrames = 0;

            if (scanNumbers.size > 0) totalFrames = scanNumbers.size;
            else
                TryGetTotalFrames(filePath, msLevel, out totalFrames);

            MassSpectra massSpectra;
            if (totalFrames > 0)
            {
                for (int count_frame = 0; count_frame < totalFrames; count_frame += FRAME_STEP)
                {
                    if (msLevel == 1)
                        massSpectra = GetMS(filePath, count_frame, ref scanNumbers);
                    else
                        massSpectra = GetMSMS(filePath, count_frame, ref scanNumbers);

                    all_spectra.AddRange(massSpectra.spectraList);

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    int new_progress = (int)((double)count_frame / (totalFrames) * 100);
                    if (new_progress > old_progress)
                    {
                        old_progress = new_progress;
                        Console.Write("Reading Bruker File: " + old_progress + "%");
                    }
                }
            }
            else
            {
                Console.WriteLine("ERROR: Could not open file!\nTotal Frames: NaN");
            }

            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            List<MSUltraLight> specList = (from ms in all_spectra.AsParallel()
                                           where ms.msLevel > 0
                                           select new MSUltraLight(ms.retentionTime, ms.scanNumber, m_A000(ms.ions), new List<(double, short)>() { m_A001(ms.precursor) }, 0, 3, (short)ms.msLevel)).ToList();

            //foreach (MassSpectrum ms in all_spectra.Where(a => a.msLevel > 0))
            //{
            //    MSUltraLight msUL = new MSUltraLight(ms.retentionTime, ms.scanNumber, ConvertIons(ms.ions), new List<(double, short)>() { ConvertPrecursor(ms.precursor) }, 0, 3, (short)ms.msLevel);
            //    msUL.ScanHeader = ms.instrumentName;
            //    msUL.PrecursorScanNumber = ms.precursorScanNumber;
            //    specList.Add(msUL);
            //}
            specList.RemoveAll(a => a.Ions.Count == 400 && a.Ions.All(b => b == (0, 0)));


            return specList;
        }

        public static List<MSUltraLight> ParseRaw(
            string filePath,
            int msLevel = 2,
            int? maxIons = null,
            List<int> searchedScanNumbers = null)
        {
            List<MSUltraLight> spectra = ParserUltraLightRawFlash.Parse(
                filePath,
                (short)msLevel,
                -1,
                false,
                searchedScanNumbers,
                false);

            if (maxIons != null)
            {
                foreach (var ms in spectra)
                {
                    if (ms.Ions.Count <= maxIons) continue;

                    var intensityThresh = ms.Ions.OrderByDescending(a => a.Intensity).ToList()[(int)maxIons - 1].Intensity;
                    ms.Ions = ms.Ions.Where(a => a.Intensity >= intensityThresh).ToList();
                }
            }

            #region Check if precursor charge state is zero

            if (spectra.Any(a => a.Precursors[0].Z == 0))
            {
                Console.WriteLine("WARNING: No precursor charge state has been identified. Predicting all of them...");

                var yParams = new Dictionary<int, YadaParams>();
                yParams.Add(1, YadaParams.GetDefaultsMS1());
                yParams.Add(2, YadaParams.GetDefaultsMS2());
                yParams[1].IsotopeModelSteps = 6;
                yParams[1].ErrorTolerancePPM = 12;

                Core yadaCore = new Core(yParams);

                List<int> ms1_scan_number = (from scan in spectra.AsParallel()
                                             select scan.PrecursorScanNumber).Distinct().ToList();
                ms1_scan_number.Sort();

                List<MSUltraLight> ms1s = ParserUltraLightRawFlash.Parse(
                                       filePath, (short)1, -1, false, ms1_scan_number, false, 3000).ToList();

                int processed_line = 0;
                int old_progress = 0;
                double lengthFile = spectra.Count;

                List<MSUltraLight> new_ms2 = new();
                foreach (var ms2 in spectra)
                {
                    MSUltraLight ms1 = ms1s.Where(a => a.ScanNumber == ms2.PrecursorScanNumber).FirstOrDefault();
                    yadaCore.MyParams[1].EnvelopeStringency = 0.94;
                    yParams[1].ErrorTolerancePPM = 10;
                    var result = m_A002(yadaCore, ms1, ms2);
                    if (result == null || result.Count == 0) continue;

                    ms2.Precursors = new List<(double MZ, short Z)>() { ((double)result[0].Item3[0].MZ, (short)result[0].Item1) };
                    if (result.Count > 1)
                    {
                        for (int i = 1; i < result.Count; i++)
                        {
                            MSUltraLight _ms2 = new MSUltraLight(ms2.CromatographyRetentionTime,
                                ms2.ScanNumber,
                                ms2.Ions,
                                new List<(double MZ, short Z)>() { ((double)result[i].Item3[0].MZ, (short)result[i].Item1) },
                                0,
                                ms2.InstrumentType,
                                ms2.MSLevel);
                            _ms2.PrecursorScanNumber = ms2.PrecursorScanNumber;
                            _ms2.ActivationType = ms2.ActivationType;
                            new_ms2.Add(_ms2);
                        }
                    }

                    processed_line++;
                    int new_progress = (int)((double)processed_line / (lengthFile) * 100);
                    if (new_progress > old_progress)
                    {
                        old_progress = new_progress;
                        Console.Write("INFO: Predicting charge state: " + old_progress + "%");
                    }

                }

                spectra.AddRange(new_ms2);
                spectra.Sort((a, b) => a.ScanNumber.CompareTo(b.ScanNumber));
            }
            #endregion

            return spectra;
        }

        public static List<MSUltraLight> ParseMS2(CancellationToken token, string filePath)
        {
            List<MSUltraLight> allMS2 = new List<MSUltraLight>();

            int processed_line = 0;
            int old_progress = 0;
            var lines = File.ReadAllLines(filePath);
            double lengthFile = lines.Length;

            MSUltraLight current = new MSUltraLight()
            {
                MSLevel = 2
            };

            float auxMass = 0;

            current.Ions = new List<(double, double)>();
            foreach (string line in lines)
            {
                if (line.StartsWith("S"))
                {
                    if (current.Ions.Count != 0)
                    {
                        allMS2.Add(current);
                        current = new MSUltraLight()
                        {
                            MSLevel = 2
                        };
                        current.Ions = new List<(double, double)>();
                    }

                    string[] split = line.Split('\t');
                    current.ScanNumber = int.Parse(split[1]);
                    auxMass = float.Parse(split[3]);
                }
                else if (line.StartsWith("Z"))
                {
                    string[] split = line.Split('\t');
                    short charge = short.Parse(split[1].Split('.')[0]);

                    current.Precursors = new List<(double, short)>()
                    {
                        (auxMass, charge)
                    };
                }
                else if (line.StartsWith("I	RetTime"))
                {
                    string[] split = line.Split('\t');
                    float ret = float.Parse(split[2]);

                    current.CromatographyRetentionTime = ret;
                }

                else if (char.IsDigit(line[0]))
                {
                    //start getting ions
                    string[] split = line.Split(' ');
                    if (split.Length == 1)
                        split = line.Split('\t');
                    current.Ions.Add((float.Parse(split[0]), float.Parse(split[1])));
                }

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                processed_line++;
                int new_progress = (int)((double)processed_line / (lengthFile) * 100);
                if (new_progress > old_progress)
                {
                    old_progress = new_progress;
                    Console.Write("Reading MS2 File: " + old_progress + "%");
                }
            }

            allMS2.Add(current);

            return allMS2;
        }
        public static List<MSUltraLight> ParseMzML(
            CancellationToken token,
            string filePath,
            List<int> searchedScanNumbers = null,
            int msLevel = 2)
        {
            List<MSUltraLight> spectra = new();

            try
            {
                //Call CSMSL module for reading mzML file
                Mzml mm = new Mzml(filePath);
                mm.Open();
                short activationType = 1;
                short instrumentTye = 1;
                int spectra_processed = 0;
                int old_progress = 0;
                double lengthFile = mm.GetMsScans().Where(scan => scan.MsnOrder > 1).Count();

                IEnumerable<MSDataScan<MZSpectrum>> scans = mm.GetMsScans().Where(scan => scan.MsnOrder == msLevel);
                if (searchedScanNumbers != null && searchedScanNumbers.Count > 0)
                    scans = scans.Where(a => searchedScanNumbers.Contains(a.SpectrumNumber)).ToList();

                //Convert MSDataScan to MSUltraLight
                foreach (MSDataScan<MZSpectrum> scan in scans)
                {
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    int new_progress = (int)((double)spectra_processed / (lengthFile) * 100);
                    if (new_progress > old_progress)
                    {
                        old_progress = new_progress;
                        Console.Write("INFO: Reading mzML File: " + old_progress + "%");
                    }

                    try
                    {

                        MZSpectrum ms = scan.MassSpectrum;
                        Spectrum<MZPeak, MZSpectrum> spec = ms;
                        double[] masses = spec.GetMasses();
                        double[] intensities = spec.GetIntensities();
                        List<(double, double)> ions = new List<(double, double)>();
                        for (int i = 0; i < masses.Count(); i++)
                            ions.Add((masses[i], intensities[i]));

                        List<(double, short)> precursors = new List<(double, short)>();
                        if (scan.MsnOrder > 1)
                        {
                            if (mm.GetDissociationType(scan.SpectrumNumber, scan.MsnOrder) == CSMSL.Proteomics.DissociationType.CID)
                                activationType = 1;
                            else if (mm.GetDissociationType(scan.SpectrumNumber, scan.MsnOrder) == CSMSL.Proteomics.DissociationType.ETD)
                                activationType = 3;
                            else if (mm.GetDissociationType(scan.SpectrumNumber, scan.MsnOrder) == CSMSL.Proteomics.DissociationType.ECD)
                                activationType = 4;
                            else if (mm.GetDissociationType(scan.SpectrumNumber, scan.MsnOrder) == CSMSL.Proteomics.DissociationType.HCD)
                                activationType = 2;
                            else if (mm.GetDissociationType(scan.SpectrumNumber, scan.MsnOrder) == CSMSL.Proteomics.DissociationType.MPD)
                                activationType = 5;
                            else if (mm.GetDissociationType(scan.SpectrumNumber, scan.MsnOrder) == CSMSL.Proteomics.DissociationType.NPTR)
                                activationType = 6;
                            else if (mm.GetDissociationType(scan.SpectrumNumber, scan.MsnOrder) == CSMSL.Proteomics.DissociationType.PQD)
                                activationType = 7;

                            float precursor_mz = (float)mm.GetPrecursorMz(scan.SpectrumNumber, scan.MsnOrder);
                            short charge = (short)mm.GetPrecusorCharge(scan.SpectrumNumber, scan.MsnOrder);
                            precursors.Add((precursor_mz, charge));
                        }

                        if (scan.MzAnalyzer == MZAnalyzerType.IonTrap2D || scan.MzAnalyzer == MZAnalyzerType.IonTrap3D)
                            instrumentTye = 2;
                        else if (scan.MzAnalyzer == MZAnalyzerType.TOF)
                            instrumentTye = 3;
                        else if (scan.MzAnalyzer == MZAnalyzerType.Orbitrap || scan.MzAnalyzer == MZAnalyzerType.FTICR)
                            instrumentTye = 1;
                        else if (scan.MzAnalyzer == MZAnalyzerType.Quadrupole)
                            instrumentTye = 4;

                        MSUltraLight mSUltraLight = new MSUltraLight(
                            mm.GetRetentionTime(scan.SpectrumNumber),
                            scan.SpectrumNumber,
                            ions,
                            precursors,
                            -1,
                            instrumentTye,
                            (short)scan.MsnOrder,
                            -1);
                        mSUltraLight.ActivationType = activationType;
                        mSUltraLight.PrecursorScanNumber = scan.ParentScanNumber;
                        spectra.Add(mSUltraLight);
                    }
                    catch (Exception) { }

                    spectra_processed++;
                }
                spectra.RemoveAll((MSUltraLight a) => a.Ions.Count == 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(" ERROR: It's not possible to read mzML file.");
            }
            return spectra;
        }

        public static List<MSUltraLight> ParseMgf(
            CancellationToken token,
            string filePath,
            int? maxNumIons = null,
            List<int> desiredScans = null)
        {
            var file = new FileInfo(filePath);

            List<MSUltraLight> spectra = new List<MSUltraLight>();

            using (StreamReader stream = file.OpenText())
            {
                float[] precursor = new float[2];
                short CHARGE = 0;
                float RTINSECONDS = 0;
                int scan = 0;

                string line;
                bool skippingTillNextSpectrum = false;
                int processed_line = 0;
                int old_progress = 0;
                double lengthFile = desiredScans == null ? File.ReadAllLines(filePath).Length : desiredScans.Count;

                do
                {
                    if ((line = stream.ReadLine()) != null)
                        line = line.Trim();
                    processed_line++;
                } while (line != "BEGIN IONS");

                while ((line = stream.ReadLine()) != null)
                {
                    if (line.Length == 0)
                        continue;

                    line = line.Trim();

                    if (skippingTillNextSpectrum == true && line.StartsWith("BEGIN IONS") == false)
                        continue;

                    skippingTillNextSpectrum = false;

                    if (char.IsDigit(line[0]))
                    {
                        var ions = new List<(double MZ, double Intensity)>();
                        do
                        {
                            var split = line.Split(new string[] { " ", "\t" }, StringSplitOptions.None);
                            ions.Add(
                                (double.Parse(split[0]), double.Parse(split[1])));
                            processed_line++;
                        } while ((line = stream.ReadLine().Trim()) != "END IONS");

                        if (maxNumIons != null && maxNumIons > 0)
                        {
                            int actualMax = ((int)maxNumIons) > ions.Count ? ions.Count : (int)maxNumIons;
                            ions = ions.OrderByDescending(a => a.Intensity).Take(actualMax).ToList();
                            ions.Sort((a, b) => a.MZ.CompareTo(b.MZ));
                        }

                        spectra.Add(new MSUltraLight()
                        {
                            Ions = ions,
                            MSLevel = 2,
                            CromatographyRetentionTime = RTINSECONDS,
                            Precursors = new List<(double MZ, short Z)>()
                                { (precursor[0], CHARGE) },
                            ScanNumber = (scan != 0) ? scan : spectra.Count + 1
                        });

                        scan = 0;
                        CHARGE = 0;
                        RTINSECONDS = 0;
                        precursor = new float[2];
                    }
                    else if (line.StartsWith("PEPMASS"))
                    {
                        string[] aux = line.Replace("PEPMASS=", "").Split(' ');

                        precursor[0] = float.Parse(aux[0]);//precursor mass
                        if (aux.Length > 1) //precursor intensity
                            precursor[1] = float.Parse(aux[1]);
                    }
                    else if (line.StartsWith("CHARGE"))
                    {
                        string sign = line.Last().ToString();
                        string num = Regex.Match(line, @"\d+").Value;

                        CHARGE = short.Parse($"{sign}{num}");
                    }
                    else if (line.StartsWith("RTINSECONDS"))
                    {
                        string aux = line.Replace("RTINSECONDS=", "");
                        RTINSECONDS = float.Parse(aux);
                    }
                    else if (line.StartsWith("SCANS="))
                    {
                        string aux = line.Replace("SCANS=", "");
                        scan = int.Parse(Regex.Match(aux, @"\d+").Value);
                        if (desiredScans != null && !desiredScans.Contains(scan))
                        {
                            skippingTillNextSpectrum = true;
                        }
                    }

                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    processed_line++;
                    int new_progress = (int)((double)processed_line / (lengthFile) * 100);
                    if (new_progress > old_progress)
                    {
                        old_progress = new_progress;
                        Console.Write("Reading MGF File: " + old_progress + "%");
                    }
                }
            }

            return spectra;
        }
        private static List<(double, double)> m_A000(KeyValuePair<double, int>[] m_A001)
        {
            return (from ion in m_A001.AsParallel()
                    select (ion.Key, (double)ion.Value)).ToList();
        }
        private static (double, short) m_A001(KeyValuePair<double, int> m_A001)
        {
            return (m_A001.Key, (short)m_A001.Value);
        }
        private static List<(int, double, List<YadaIon>)> m_A002(Core m_A001, MSUltraLight m_A002, MSUltraLight m_A003)
        {
            List<(int, double, List<YadaIon>)> m_A006 = new();

            var result = m_A004(m_A001, m_A002, m_A003);
            if (result != null && result.Envelopes != null && result.Envelopes.Count > 0)
            {
                m_A006.AddRange(result.Envelopes);
                m_A006 = m_A006.Distinct(new EnvelopeComparer()).ToList();
            }
            m_A001.MyParams[1].ErrorTolerancePPM = 12;
            result = m_A004(m_A001, m_A002, m_A003);
            if (result != null && result.Envelopes != null && result.Envelopes.Count > 0)
            {
                m_A006.AddRange(result.Envelopes);
                m_A006 = m_A006.Distinct(new EnvelopeComparer()).ToList();
            }
            m_A001.MyParams[1].MinimunPeaksPerCharge[4] = 2;
            result = m_A004(m_A001, m_A002, m_A003);
            if (result != null && result.Envelopes != null && result.Envelopes.Count > 0)
            {
                m_A006.AddRange(result.Envelopes);
                m_A006 = m_A006.Distinct(new EnvelopeComparer()).ToList();
            }
            m_A001.MyParams[1].MinimunPeaksPerCharge[3] = 2;
            result = m_A004(m_A001, m_A002, m_A003);
            if (result != null && result.Envelopes != null && result.Envelopes.Count > 0)
            {
                m_A006.AddRange(result.Envelopes);
                m_A006 = m_A006.Distinct(new EnvelopeComparer()).ToList();
            }
            m_A001.MyParams[1].MinimunPeaksPerCharge[3] = 3;
            m_A001.MyParams[1].MinimunPeaksPerCharge[4] = 3;
            m_A001.MyParams[1].EnvelopeStringency = 0.7;
            result = m_A004(m_A001, m_A002, m_A003);
            if (result != null && result.Envelopes != null && result.Envelopes.Count > 0)
            {
                m_A006.AddRange(result.Envelopes);
                m_A006 = m_A006.Distinct(new EnvelopeComparer()).ToList();
            }
            m_A001.MyParams[1].ErrorTolerancePPM = 15;
            result = m_A004(m_A001, m_A002, m_A003);
            if (result != null && result.Envelopes != null && result.Envelopes.Count > 0)
            {
                m_A006.AddRange(result.Envelopes);
                m_A006 = m_A006.Distinct(new EnvelopeComparer()).ToList();
            }
            m_A001.MyParams[1].EnvelopeStringency = 0.6;
            result = m_A004(m_A001, m_A002, m_A003);
            if (result != null && result.Envelopes != null && result.Envelopes.Count > 0)
            {
                m_A006.AddRange(result.Envelopes);
                m_A006 = m_A006.Distinct(new EnvelopeComparer()).ToList();
            }

            return m_A006.OrderByDescending(a => a.Item3.Sum(b => b.Intensity) * a.Item2).ToList();
        }
        private static bool m_A003(double m_A001, int m_A002, double m_A003, int m_A004, int m_A005)
        {
            double isotopeMass = 1.00335483778f;
            double precursorMH1 = SpectraOperations.ChargeOperations.ToMH(m_A001, m_A002);
            double precursorMH2 = SpectraOperations.ChargeOperations.ToMH(m_A003, m_A004);

            for (int precIsotope = 0; precIsotope <= m_A005; precIsotope++)
            {
                double adjustedPrecursorMH = precursorMH1 - precIsotope * isotopeMass;
                if (Math.Abs(SpectraOperations.ScoringFunctions.GetPPM(adjustedPrecursorMH, precursorMH2)) < 30)
                    return true;
            }
            return false;
        }
        private static YadaSpectrum m_A004(Core m_A001, MSUltraLight m_A002, MSUltraLight m_A0030)
        {
            var result = m_A001.Deisotope(m_A002.Ions.Where(a => a.MZ - 1.5 < m_A0030.Precursors[0].MZ && a.MZ + 1.5 > m_A0030.Precursors[0].MZ).ToList(), 1);
            result.Envelopes = result.Envelopes.Where(a => a.ions.Any(b => m_A003(b.MZ, a.z, m_A0030.Precursors[0].MZ, a.z, 3))).ToList();
            if (result == null || result.Envelopes.Count == 0)
            {
                result = m_A001.Deisotope(m_A002.Ions.Where(a => a.MZ - 2 < m_A0030.Precursors[0].MZ && a.MZ + 2 > m_A0030.Precursors[0].MZ).ToList(), 1);
                result.Envelopes = result.Envelopes.Where(a => a.ions.Any(b => m_A003(b.MZ, a.z, m_A0030.Precursors[0].MZ, a.z, 3))).ToList();
                if (result == null || result.Envelopes.Count == 0)
                {
                    result = m_A001.Deisotope(m_A002.Ions.Where(a => a.MZ - 3 < m_A0030.Precursors[0].MZ && a.MZ + 3 > m_A0030.Precursors[0].MZ).ToList(), 1);
                    result.Envelopes = result.Envelopes.Where(a => a.ions.Any(b => m_A003(b.MZ, a.z, m_A0030.Precursors[0].MZ, a.z, 3))).ToList();
                    if (result == null || result.Envelopes.Count == 0)
                    {
                        return null;
                    }
                }
            }
            return result;
        }
        private static ScanNumbers m_A005(List<int> m_A001)
        {
            int[] scanNumArray = m_A001.ToArray();
            IntPtr scanNumDataPtr = Marshal.AllocHGlobal(scanNumArray.Length * sizeof(int));
            Marshal.Copy(scanNumArray, 0, scanNumDataPtr, scanNumArray.Length);
            ScanNumbers scanNumbers = new ScanNumbers();
            scanNumbers.size = scanNumArray.Length;
            scanNumbers.data = scanNumDataPtr;
            return scanNumbers;
        }
    }

    public class EnvelopeComparer : IEqualityComparer<(int z, double score, List<YadaIon> ions)>
    {
        public bool Equals((int z, double score, List<YadaIon> ions) x, (int z, double score, List<YadaIon> ions) y)
        {
            //Check if the compared objects reference has same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check if any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            //Check if the items' properties are equal.
            return x.z == y.z &&
                x.ions.Count > 0 && y.ions.Count > 0 &&
                x.ions[0].MZ == y.ions[0].MZ;
        }

        public int GetHashCode((int z, double score, List<YadaIon> ions) obj)
        {
            return obj.ions.Count > 0 ? obj.z.GetHashCode() * 17 + obj.ions[0].MZ.GetHashCode() * 17 : obj.z.GetHashCode() * 17;
        }
    }

}
