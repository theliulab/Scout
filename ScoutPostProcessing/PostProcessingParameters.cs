using Accord.Math.Geometry;
using Digestor;
using ScoutCore;
using ScoutPostProcessing.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Digestor.Enzyme;

namespace ScoutPostProcessing
{
    public enum FDRModes
    {
        CombinedIntraInter,
        SeparateIntraInter
    }

    public class PostProcessingParameters
    {

        private List<string> _csm_Features;
        public List<string> CSM_Features
        {
            get => internal_method0();
            set => _csm_Features = value;
        }
        public double CSM_FDR { get; set; } = 0.01;
        private List<string> _resPair_Features;
        public List<string> ResPair_Features
        {
            get => internal_method1();
            set => _resPair_Features = value;
        }
        public double ResPair_FDR { get; set; } = 0.01;
        private List<string> _ppi_Features;
        public List<string> PPI_Features
        {
            get => internall_method2();
            set => _ppi_Features = value;
        }
        public double PPI_FDR { get; set; } = 0.01;
        public bool UniquePPIsOnly { get; set; } = false;

        public bool UsePythonModels { get; set; } = true;
        public bool UseFinalScoringCSMs { get; set; } = false;
        public bool GroupedByGene { get; set; } = false;
        public bool IsLooplinkFilter { get; set; } = false;
        public bool ApplyBoostFDR { get; set; } = false;

        #region Apply filters
        public bool ApplyPostProcessingFilters { get; set; } = true;
        public int DiffPPM_Threshold { get; set; } = 3;
        public bool Apply_DiffPPM_Threshold { get; set; } = false;
        public double PoissonScore_Threshold { get; set; } = 1.5;
        public double MinDDPScore_Threshold { get; set; } = 0.06;
        public bool MinUniqueRPsInInterProteins { get; set; } = true;
        public bool MinUniqueRPsInIntraProteins { get; set; } = false;
        public bool Independent_FDR_Control { get; set; } = false;

        #endregion

        public FDRModes FDRMode { get; set; } = FDRModes.SeparateIntraInter;

        private List<string> internal_method0()
        {
            if (IsLooplinkFilter) return defaultCSMLooplinkParams;
            return defaultCSMInterParams;
        }
        private List<string> internal_method1()
        {
            if (Independent_FDR_Control) return defaultResPairIndepedentParams;
            return defaultResPairParams;
        }
        private List<string> internall_method2()
        {
            if (Independent_FDR_Control) return defaultPPIIndependentParams_Inter;
            return defaultPPIParams_Inter;
        }

        public List<string> defaultCSMLooplinkParams = new List<string>()
        {
             "XLScore",
             "PoissonScore",
             "MinDDPScore",
             "DiffPPM",
             "MinUniqueRPsInInterProteins",
             "MinUniqueRPsInIntraProteins"
        };

        public List<string> defaultCSMInterParams = new List<string>()
        {
             "PoissonScore",
             "MinDDPScore",
             "DiffPPM",
             "MinUniqueRPsInInterProteins",
             "MinUniqueRPsInIntraProteins"
        };

        private static List<string> defaultResPairParams = new List<string>()
        {
            "TopCSMScore",
            "BestPoisson",
            "BestMinScore",
            "MinUniqueRPsInInterProteins",
            "MinUniqueRPsInIntraProteins"
        };

        private static List<string> defaultResPairIndepedentParams = new List<string>()
        {
            "BestPoisson",
            "BestSpectralAngle",
            "BestMinScore",
            "BestDDPScore",
            "BestDiffPPM",
            "MinUniqueRPsInInterProteins",
            "MinUniqueRPsInIntraProteins"
        };

        public static List<string> defaultPPIParams_Inter = new List<string>()
        {
            "BestClassificationScore",
            "UniquePepsA_B",
            "UniquePepsB_A"
        };

        public static List<string> defaultPPIParams_Intra = new List<string>()
        {
            "BestClassificationScore",
            "UniquePepsA_B",
            "UniquePepsB_A"
        };

        public static List<string> defaultPPIIndependentParams_Inter = new List<string>()
        {
            "BestPoissonScore",
            "UniquePepsA_B",
            "UniquePepsB_A"
        };

        internal List<string> GetFinalScoringFeatures()
        {
            var r = new List<string>()
            {
                "AlphaScore",
                "AlphaTagScore",
                "AlphaDeltaCN",
                "BetaScore",
                "BetaTagScore",
                "BetaDeltaCN",
            };
            return r;
        }


        #region SavingAndLoading
        public static PostProcessingParameters Load(string path)
        {
            string content = File.ReadAllText(path);

            var parameters = ScoutCore.FileManagement.Serializer.FromJson<PostProcessingParameters>(
                content, true);

            PostProcessingParameters _postProcessingParams = new PostProcessingParameters();
            _postProcessingParams.UniquePPIsOnly = parameters.UniquePPIsOnly;
            _postProcessingParams.FDRMode = parameters.FDRMode;
            _postProcessingParams.GroupedByGene = parameters.GroupedByGene;
            _postProcessingParams.ApplyBoostFDR = parameters.ApplyBoostFDR;
            _postProcessingParams.CSM_FDR = parameters.CSM_FDR;
            _postProcessingParams.ResPair_FDR = parameters.ResPair_FDR;
            _postProcessingParams.PPI_FDR = parameters.PPI_FDR;

            #region filters
            _postProcessingParams.ApplyPostProcessingFilters = parameters.ApplyPostProcessingFilters;
            _postProcessingParams.DiffPPM_Threshold = parameters.DiffPPM_Threshold;
            _postProcessingParams.Apply_DiffPPM_Threshold = parameters.Apply_DiffPPM_Threshold;
            _postProcessingParams.PoissonScore_Threshold = parameters.PoissonScore_Threshold;
            _postProcessingParams.MinDDPScore_Threshold = parameters.MinDDPScore_Threshold;
            _postProcessingParams.MinUniqueRPsInInterProteins = parameters.MinUniqueRPsInInterProteins;
            _postProcessingParams.MinUniqueRPsInIntraProteins = parameters.MinUniqueRPsInIntraProteins;
            _postProcessingParams.Independent_FDR_Control = parameters.Independent_FDR_Control;
            #endregion

            return _postProcessingParams;
        }


        public static PostProcessingParameters LoadFromString(string content)
        {

            var parameters = ScoutCore.FileManagement.Serializer.FromJson<PostProcessingParameters>(
                content, true);

            return parameters;
        }

        public string ToSaveJson(bool indentJson = true)
        {
            string content = ScoutCore.FileManagement.Serializer.ToJSON(
                this, false, indentJson);
            return content;
        }

        public PostProcessingParameters Copy()
        {
            return LoadFromString(ToSaveJson());
        }

        private DataTable CreateParamsDatatable()
        {
            DataTable dtParams = new DataTable();

            dtParams.Columns.Add("Property");
            dtParams.Columns.Add("Value");

            var row = dtParams.NewRow();
            row["Property"] = "Use only unique XLs into PPIs";
            row["Value"] = UniquePPIsOnly;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Separate protein intra- and inter-crosslinks";
            row["Value"] = FDRMode == FDRModes.SeparateIntraInter;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "FDR";
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "  CSM level";
            row["Value"] = CSM_FDR;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "  Residue pair level";
            row["Value"] = ResPair_FDR;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "  PPI level";
            row["Value"] = PPI_FDR;
            dtParams.Rows.Add(row);

            return dtParams;
        }

        public void PrintToConsole()
        {
            Console.WriteLine($"===================\nPost processing parameters:\n===================");
            foreach (DataRow row in CreateParamsDatatable().Rows)
            {
                string property = row["Property"].ToString();
                string value = row["Value"].ToString();
                Console.WriteLine("{0}: {1}", property, value);
            }
        }

        #endregion
    }
}
