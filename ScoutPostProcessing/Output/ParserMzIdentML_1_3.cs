using Digestor;
using EhuBio.Proteomics.Hupo.mzIdentML;
using EhuBio.Proteomics.Hupo.mzIdentML1_3;
using EhuBio.Proteomics.Inference;
using PatternTools.FastaTools;
using PatternTools.MSParserLight;
using ScoutCore;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScoutPostProcessing.Output
{
    public class ParserMzIdentML_1_3 : Mapper
    {
        private const string MZIDML_FILE_EXTENSION = "-specId.ms2";
        private List<ScoredCSM> OriginalScoredCSMs;
        private FastaFileParser _FASTA;
        /// <summary>
        /// Private variables
        /// </summary>
        private mzidFile1_3 MzidML;

        //Variables for saving mzIdML
        private struct PeptideMzIdML
        {
            public PeptideType PeptideType;
            public List<string> Proteins;
        }
        private List<DBSequenceType> ProteinList;
        private List<PeptideMzIdML> PeptideEvidenceList;

        //Variables for loading mzIdML
        private List<PeptideType> PeptideList;
        private List<SpectrumIdentificationResultType> SpectrumList;
        private Regex numberCaptured = new Regex("[0-9|\\.]+", RegexOptions.Compiled);
        private int TotalProteins = -1;

        private static Mapper.Software sw;
        public static int progress_reading_mzIdentML = 0;
        public string ErrorMsg = "";

        /// <summary>
        /// Constructor
        /// </summary>
        public ParserMzIdentML_1_3()
            : base(sw)
        {
            m_Type = Mapper.SourceType.mzIdentML130;
            MzidML = new mzidFile1_3();

            MzidML.Data = new MzIdentMLType();

            #region cv List
            List<cvType> cvList = new List<cvType>();
            cvType cv = new cvType();
            cv.id = "PSI-MS";
            cv.uri = "https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo";
            cv.fullName = "PSI-MS";
            cvList.Add(cv);

            cvType cv2 = new cvType();
            cv2.id = "XLMOD";
            cv2.uri = "https://raw.githubusercontent.com/HUPO-PSI/xlmod-CV/main/XLMOD.obo";
            cv2.fullName = "XLMOD";
            cvList.Add(cv2);

            cvType cv3 = new cvType();
            cv3.id = "UNIMOD";
            cv3.uri = "http://www.unimod.org/obo/unimod.obo";
            cv3.fullName = "UNIMOD";
            cvList.Add(cv3);

            cvType cv4 = new cvType();
            cv4.id = "UO";
            cv4.uri = "https://raw.githubusercontent.com/bio-ontology-research-group/unit-ontology/master/unit.obo";
            cv4.fullName = "UNIT-ONTOLOGY";
            cvList.Add(cv4);
            MzidML.Data.cvList = cvList.ToArray();
            #endregion

            CVParamType xlms = new CVParamType();
            xlms.accession = "MS:1003385";
            xlms.name = "mzIdentML crosslinking extension document version";
            xlms.cvRef = "PSI-MS";
            xlms.value = "1.0.0";
            MzidML.Data.cvParam = xlms;

            MzidML.Data.version = "1.3.0";

            MzidML.ListEvidences = new List<PeptideEvidenceType>();

            ProteinList = new List<DBSequenceType>();
            MzidML.ListProteins = ProteinList;

            PeptideEvidenceList = new List<PeptideMzIdML>();
            MzidML.ListPeptides = new List<PeptideType>();

            PeptideList = new List<PeptideType>();
            SpectrumList = new List<SpectrumIdentificationResultType>();
        }

        /// <summary>
        /// Method resposible for saving mzIdentML file
        /// </summary>
        /// <param name="fpath"></param>
        /// <param name="_results"></param>
        public bool SaveMzIdML(string fpath, XLFilteredResults _results)
        {
            if (MzidML == null || m_InputFiles.Count > 1 || _results == null)
            {
                ErrorMsg += "Results object is null.\n";
                return false;
            }

            //Load scout search params
            ScoutParameters ScoutParams = _results.SearchParams;

            MzidML.Data.creationDate = DateTime.Now;
            MzidML.Data.creationDateSpecified = true;

            try
            {
                _FASTA = Utils.GetSearchFasta(_results.FastaPath, ScoutParams);
            }
            catch (Exception)
            {
                ErrorMsg += "Fasta not found.\n";
                return false;
            }

            #region Cloning MySearchQueriesList
            OriginalScoredCSMs = new List<ScoredCSM>((from ppi in _results.PackagePPIs.FilteredPPIs.AsParallel()
                                                      select ppi.GetAllCSMs()).SelectMany(a => a).ToList());

            //Add looplinks
            OriginalScoredCSMs.AddRange(_results.PackageCSMsLoopLinks.FilteredCSMs.Where(a => a.IsDecoy == false));
            #endregion

            //Check spectra
            if (OriginalScoredCSMs.Count == 0)
            {
                ErrorMsg += "There are no spectra.\n";
                return false;
            }

            if (OriginalScoredCSMs[0].Spectrum == null)
            {
                ErrorMsg += "Spectra object is null.\n";
                return false;
            }

            //Add protein information
            #region Add Proteins

            List<PPI> ppis = _results.PackagePPIs.FilteredPPIs;
            List<(string identifier, string seq)> proteins = new();
            foreach (var ppi in ppis)
            {
                string ptnA = Utils.getAccessionNumber(ppi.ProteinOneString.Replace(',', ';'));
                string ptnB = Utils.getAccessionNumber(ppi.ProteinTwoString.Replace(',', ';'));
                var fastaA = _FASTA.MyItems.Where(a => a.SequenceIdentifier.Contains(ptnA)).FirstOrDefault();
                var fastaB = _FASTA.MyItems.Where(a => a.SequenceIdentifier.Contains(ptnB)).FirstOrDefault();
                if (fastaA != null && fastaB != null)
                {
                    proteins.Add((ptnA, fastaA.Sequence));
                    proteins.Add((ptnB, fastaB.Sequence));
                }
            }

            foreach (var ptn in _results.PackageCSMsLoopLinks.FilteredCSMs.Where(a => a.IsDecoy == false).SelectMany(b => b.AlphaMappings))
            {
                var fasta = _FASTA.MyItems.Where(a => a.SequenceIdentifier.Contains(ptn.Locus)).FirstOrDefault();
                if (fasta != null)
                    proteins.Add((ptn.Locus, fasta.Sequence));
            }

            proteins = proteins.Distinct().ToList();
            TotalProteins = proteins.Count;

            foreach (var protein in proteins)
                AddProtein(protein.identifier, protein.identifier, protein.identifier, protein.seq, protein.identifier);
            #endregion

            //Add peptide information and fill PeptideEvidenceList (ListEvidence) too
            #region Add Peptides

            int peptID = 0;
            int count = 0;

            try
            {
                foreach (var csm in OriginalScoredCSMs)
                {
                    if (csm.IsLoopLink == false)
                    {
                        string peptide1 = csm.AlphaPSM.Peptide.AsString;
                        string peptide2 = csm.BetaPSM.Peptide.AsString;
                        List<string> proteinList1 = csm.AlphaMappings.Select(a => a.Locus).Distinct().ToList();
                        List<string> proteinList2 = csm.BetaMappings.Select(a => a.Locus).Distinct().ToList();

                        AddPeptide(String.Join("", System.Text.ASCIIEncoding.Default.GetBytes(peptide1)) + "_" + peptID.ToString(), peptide1, csm.AlphaPSM.Peptide.AsCleanString, _results.SearchParams, proteinList1, 0, true, true, csm.AlphaPSM.ReagentPosition1, peptID);
                        AddPeptide(String.Join("", System.Text.ASCIIEncoding.Default.GetBytes(peptide2)) + "_" + peptID.ToString(), peptide2, csm.BetaPSM.Peptide.AsCleanString, _results.SearchParams, proteinList2, 1, true, false, csm.BetaPSM.ReagentPosition1, peptID);
                    }
                    else
                    {
                        string peptide1 = csm.AlphaPSM.Peptide.AsString;
                        List<string> proteinList1 = csm.AlphaMappings.Select(a => a.Locus).Distinct().ToList();

                        AddPeptideLooplink(String.Join("", System.Text.ASCIIEncoding.Default.GetBytes(peptide1)) + "_" + peptID.ToString(), peptide1, csm.AlphaPSM.Peptide.AsCleanString, _results.SearchParams, proteinList1, csm.AlphaPSM.ReagentPosition1, csm.AlphaPSM.ReagentPosition2, peptID);
                    }
                    peptID++;
                }
            }
            catch (Exception)
            {
                Console.WriteLine();
            }


            #region PeptideEvidence
            List<PeptideEvidenceType> listEvidences = new List<PeptideEvidenceType>();

            count = 0;

            try
            {
                foreach (PeptideMzIdML pept in PeptideEvidenceList)
                {
                    if (pept.Proteins == null) continue;
                    PeptideEvidenceType new_pept = new PeptideEvidenceType();
                    new_pept.isDecoy = false;
                    new_pept.peptide_ref = pept.PeptideType.id;
                    new_pept.dBSequence_ref = pept.Proteins[0];
                    string original_protein = pept.Proteins[0];
                    FastaItem _fastaPtn = _FASTA.MyItems.FirstOrDefault(item => item.SequenceIdentifier.Equals(original_protein));
                    if (_fastaPtn == null) throw new Exception("Protein sequence not found.");
                    string ptnStr = _fastaPtn.Sequence;
                    int startPosInPtn = ScoutCore.Utils.IndexOfFast(_fastaPtn.SequenceInBytes, System.Text.ASCIIEncoding.Default.GetBytes(pept.PeptideType.PeptideSequence));
                    new_pept.start = startPosInPtn + 1;
                    new_pept.startSpecified = true;
                    new_pept.endSpecified = true;
                    new_pept.end = startPosInPtn + pept.PeptideType.PeptideSequence.Length + 1;
                    if (startPosInPtn > 0)
                        new_pept.pre = "" + ptnStr[startPosInPtn - 1];
                    else
                        new_pept.pre = "-";

                    if (new_pept.end < ptnStr.Length + 1)
                        new_pept.post = "" + ptnStr[new_pept.end - 1];
                    else
                        new_pept.post = "-";
                    new_pept.id = new_pept.peptide_ref + "_" + new_pept.dBSequence_ref;
                    listEvidences.Add(new_pept);
                }
                count++;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            MzidML.ListEvidences = listEvidences;

            PeptideEvidenceList.Clear();
            #endregion

            #endregion

            //Add information about laboratories
            #region Organization
            OrganizationType org = new OrganizationType();
            org.id = "LPEC";
            org.name = "Laboratory for structural and computational proteomics";
            CVParamType url = new CVParamType();
            url.accession = "MS:1000588";
            url.name = "contact URL";
            url.cvRef = "PSI-MS";
            url.value = "https://www.icc.fiocruz.br/lpec/";
            org.Item = url;
            MzidML.ListOrganizations.Add(org);

            OrganizationType org2 = new OrganizationType();
            org2.id = "TheLiuLab";
            org2.name = "The Liu Lab - Germany";
            CVParamType url2 = new CVParamType();
            url2.accession = "MS:1000588";
            url2.name = "contact URL";
            url2.cvRef = "PSI-MS";
            url2.value = "https://theliulab.com/";
            org2.Item = url2;
            MzidML.ListOrganizations.Add(org2);
            #endregion

            //Add information about software's authors
            #region Software's authors
            PersonType person = new PersonType();
            person.id = "Scout_Author_MAC";
            person.firstName = "Milan";
            person.lastName = "A Clasen";
            CVParamType email = new CVParamType();
            email.accession = "MS:1000589";
            email.name = "contact email";
            email.cvRef = "PSI-MS";
            email.value = "milanclasen@gmail.com";
            person.Item = email;
            AffiliationType aff = new AffiliationType();
            aff.organization_ref = org.id;
            person.Affiliation = new AffiliationType[] { aff };
            MzidML.ListPeople.Add(person);

            PersonType person2 = new PersonType();
            person2.id = "Scout_Author_MR";
            person2.firstName = "Max";
            person2.lastName = "Ruwolt";
            CVParamType email2 = new CVParamType();
            email2.accession = "MS:1000589";
            email2.name = "contact email";
            email2.cvRef = "PSI-MS";
            email2.value = "ruwolt@fmp-berlin.de";
            person2.Item = email2;
            AffiliationType aff2 = new AffiliationType();
            aff2.organization_ref = org2.id;
            person2.Affiliation = new AffiliationType[] { aff2 };
            MzidML.ListPeople.Add(person2);

            PersonType person3 = new PersonType();
            person3.id = "Scout_Author_DBL";
            person3.firstName = "Diogo";
            person3.lastName = "B Lima";
            CVParamType email3 = new CVParamType();
            email3.accession = "MS:1000589";
            email3.name = "contact email";
            email3.cvRef = "PSI-MS";
            email3.value = "diogobor@gmail.com";
            person3.Item = email3;
            AffiliationType aff3 = new AffiliationType();
            aff3.organization_ref = org2.id;
            person3.Affiliation = new AffiliationType[] { aff3 };
            MzidML.ListPeople.Add(person3);

            PersonType person4 = new PersonType();
            person4.id = "Scout_Author_PCC";
            person4.firstName = "Paulo";
            person4.lastName = "C Carvalho";
            CVParamType email4 = new CVParamType();
            email4.accession = "MS:1000589";
            email4.name = "contact email";
            email4.cvRef = "PSI-MS";
            email4.value = "paulo@pcarvalho.com";
            person4.Item = email4;
            AffiliationType aff4 = new AffiliationType();
            aff4.organization_ref = org.id;
            person4.Affiliation = new AffiliationType[] { aff4 };
            MzidML.ListPeople.Add(person4);

            PersonType person5 = new PersonType();
            person5.id = "Scout_Author_FL";
            person5.firstName = "Fan";
            person5.lastName = "Liu";
            CVParamType email5 = new CVParamType();
            email5.accession = "MS:1000589";
            email5.name = "contact email";
            email5.cvRef = "PSI-MS";
            email5.value = "fliu@fmp-berlin.de";
            person5.Item = email5;
            AffiliationType aff5 = new AffiliationType();
            aff5.organization_ref = org2.id;
            person5.Affiliation = new AffiliationType[] { aff5 };
            MzidML.ListPeople.Add(person5);
            #endregion

            //Add information about the software provider
            #region Provider
            ProviderType provider = new ProviderType();
            provider.id = "PROVIDER";

            ContactRoleType contactRole = new ContactRoleType();
            contactRole.contact_ref = person3.id;

            RoleType roleProvider = new RoleType();
            CVParamType contacttypeProvider = new CVParamType();
            contacttypeProvider.accession = "MS:1001271";
            contacttypeProvider.cvRef = "PSI-MS";
            contacttypeProvider.name = "researcher";
            roleProvider.cvParam = contacttypeProvider;

            contactRole.Role = roleProvider;

            provider.ContactRole = contactRole;
            MzidML.Data.Provider = provider;
            #endregion

            //Add information about the software
            #region Analysis software
            AnalysisSoftwareType sw = new AnalysisSoftwareType();
            sw.id = "SCOUT_ID_Software";
            sw.name = "Scout";
            sw.uri = "https://github.com/diogobor/Scout";
            sw.version = _results.FileVersion;
            CVParamType swname = new CVParamType();
            swname.name = "Scout";
            swname.cvRef = "PSI-MS";
            swname.accession = "MS:1003407";
            sw.SoftwareName = new ParamType();
            sw.SoftwareName.Item = swname;
            RoleType role = new RoleType();
            CVParamType contacttype = new CVParamType();
            contacttype.accession = "MS:1001271";
            contacttype.cvRef = "PSI-MS";
            contacttype.name = "researcher";
            role.cvParam = contacttype;
            sw.ContactRole = new ContactRoleType();
            sw.ContactRole.contact_ref = person.id;
            sw.ContactRole.Role = role;
            sw.Customizations = m_Software.Customizations;
            MzidML.ListSW.Add(sw);
            #endregion

            //Add information about the type of search
            #region Protein detection list

            AnalysisProtocolCollectionType analysisProtocol = new AnalysisProtocolCollectionType();

            SpectrumIdentificationProtocolType specIdProtocolType = new SpectrumIdentificationProtocolType();
            specIdProtocolType.analysisSoftware_ref = sw.id;
            specIdProtocolType.id = "SIP";

            ParamType searchType = new ParamType();
            CVParamType cvSearchType = new CVParamType();
            cvSearchType.name = "ms-ms search";
            cvSearchType.accession = "MS:1001083";
            cvSearchType.cvRef = "PSI-MS";
            searchType.Item = cvSearchType;
            specIdProtocolType.SearchType = searchType;

            //Add information about the paramenters used in search like series (b, y)
            #region additional search params List
            ParamListType paramsListType = new ParamListType();
            List<AbstractParamType> absParamTypeListSearchParams = new List<AbstractParamType>();

            CVParamType absParamTypeSearchParams = new CVParamType();
            absParamTypeSearchParams.name = "parent mass type mono";
            absParamTypeSearchParams.unitCvRef = "PSI-MS";
            absParamTypeSearchParams.unitAccession = "MS:1001211";
            absParamTypeSearchParams.cvRef = "PSI-MS";
            absParamTypeSearchParams.accession = "MS:1001211";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams);

            CVParamType absParamTypeSearchParams2 = new CVParamType();
            absParamTypeSearchParams2.name = "fragment mass type mono";
            absParamTypeSearchParams2.unitCvRef = "PSI-MS";
            absParamTypeSearchParams2.unitAccession = "MS:1001256";
            absParamTypeSearchParams2.cvRef = "PSI-MS";
            absParamTypeSearchParams2.accession = "MS:1001256";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams2);

            CVParamType absParamTypeSearchParams3 = new CVParamType();
            absParamTypeSearchParams3.name = "param: b ion";
            absParamTypeSearchParams3.unitCvRef = "PSI-MS";
            absParamTypeSearchParams3.unitAccession = "MS:1001118";
            absParamTypeSearchParams3.cvRef = "PSI-MS";
            absParamTypeSearchParams3.accession = "MS:1001118";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams3);

            CVParamType absParamTypeSearchParams4 = new CVParamType();
            absParamTypeSearchParams4.name = "param: b ion-NH3";
            absParamTypeSearchParams4.unitCvRef = "PSI-MS";
            absParamTypeSearchParams4.unitAccession = "MS:1001149";
            absParamTypeSearchParams4.cvRef = "PSI-MS";
            absParamTypeSearchParams4.accession = "MS:1001149";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams4);

            CVParamType absParamTypeSearchParams5 = new CVParamType();
            absParamTypeSearchParams5.name = "param: b ion-H2O";
            absParamTypeSearchParams5.unitCvRef = "PSI-MS";
            absParamTypeSearchParams5.unitAccession = "MS:1001150";
            absParamTypeSearchParams5.cvRef = "PSI-MS";
            absParamTypeSearchParams5.accession = "MS:1001150";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams5);

            CVParamType absParamTypeSearchParams6 = new CVParamType();
            absParamTypeSearchParams6.name = "param: y ion";
            absParamTypeSearchParams6.unitCvRef = "PSI-MS";
            absParamTypeSearchParams6.unitAccession = "MS:1001262";
            absParamTypeSearchParams6.cvRef = "PSI-MS";
            absParamTypeSearchParams6.accession = "MS:1001262";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams6);

            CVParamType absParamTypeSearchParams7 = new CVParamType();
            absParamTypeSearchParams7.name = "param: y ion-NH3";
            absParamTypeSearchParams7.unitCvRef = "PSI-MS";
            absParamTypeSearchParams7.unitAccession = "MS:1001151";
            absParamTypeSearchParams7.cvRef = "PSI-MS";
            absParamTypeSearchParams7.accession = "MS:1001151";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams7);

            CVParamType absParamTypeSearchParams8 = new CVParamType();
            absParamTypeSearchParams8.name = "param: y ion-H2O";
            absParamTypeSearchParams8.unitCvRef = "PSI-MS";
            absParamTypeSearchParams8.unitAccession = "MS:1001152";
            absParamTypeSearchParams8.cvRef = "PSI-MS";
            absParamTypeSearchParams8.accession = "MS:1001152";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams8);

            CVParamType absParamTypeSearchParams9 = new CVParamType();
            absParamTypeSearchParams9.name = "crosslinking search";
            absParamTypeSearchParams9.unitCvRef = "PSI-MS";
            absParamTypeSearchParams9.unitAccession = "MS:1002494";
            absParamTypeSearchParams9.cvRef = "PSI-MS";
            absParamTypeSearchParams9.accession = "MS:1002494";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams9);

            CVParamType absParamTypeSearchParams10 = new CVParamType();
            absParamTypeSearchParams10.name = "peptide-level scoring";
            absParamTypeSearchParams10.unitCvRef = "PSI-MS";
            absParamTypeSearchParams10.unitAccession = "MS:1002490";
            absParamTypeSearchParams10.cvRef = "PSI-MS";
            absParamTypeSearchParams10.accession = "MS:1002490";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams10);

            CVParamType absParamTypeSearchParams11 = new CVParamType();
            absParamTypeSearchParams11.name = "group CSMs by sequence";
            absParamTypeSearchParams11.unitCvRef = "PSI-MS";
            absParamTypeSearchParams11.unitAccession = "MS:1002496";
            absParamTypeSearchParams11.cvRef = "PSI-MS";
            absParamTypeSearchParams11.accession = "MS:1002496";
            absParamTypeListSearchParams.Add(absParamTypeSearchParams11);

            if (_results.PostParams.FDRMode == FDRModes.SeparateIntraInter)
            {
                CVParamType absParamTypeSearchParams12 = new CVParamType();
                absParamTypeSearchParams12.name = "FDR applied separately to self crosslinks and protein heteromeric crosslinks";
                absParamTypeSearchParams12.unitCvRef = "PSI-MS";
                absParamTypeSearchParams12.unitAccession = "MS:1003343";
                absParamTypeSearchParams12.cvRef = "PSI-MS";
                absParamTypeSearchParams12.accession = "MS:1003343";
                absParamTypeListSearchParams.Add(absParamTypeSearchParams12);
            }

            paramsListType.Items = absParamTypeListSearchParams.ToArray();

            specIdProtocolType.AdditionalSearchParams = paramsListType;
            #endregion

            //Add information about the modification used in search
            #region modification params
            List<SearchModificationType> modificationList = new List<SearchModificationType>();

            string xlName = ScoutParams.CXLReagent.Name.ToLower();
            List<SpectrumWizard.AminoacidMod> all_modifications = new List<SpectrumWizard.AminoacidMod>(ScoutParams.StaticModifications);
            if (all_modifications == null) all_modifications = new();
            all_modifications.AddRange(ScoutParams.VariableModifications);

            char[] reactionSites_char = new char[0];
            string reactionSites = "";

            foreach (SpectrumWizard.AminoacidMod scout_mod in all_modifications)
            {
                SearchModificationType modif = new SearchModificationType();
                modif.massDelta = (float)scout_mod.MassShift;
                modif.fixedMod = !scout_mod.IsVariable;

                reactionSites_char = scout_mod.TargetResidues.ToCharArray();
                reactionSites = string.Join(" ", reactionSites_char);
                modif.residues = reactionSites;

                CVParamType cvMod = new CVParamType();

                if (scout_mod.Name.ToLower().Contains("carbamidomethyl"))
                {
                    cvMod.accession = "UNIMOD:4";
                    cvMod.cvRef = "UNIMOD";
                }
                else if (scout_mod.Name.ToLower().Contains("oxidation") || scout_mod.Name.ToLower().Contains("oxydation"))
                {
                    cvMod.accession = "UNIMOD:35";
                    cvMod.cvRef = "UNIMOD";
                }
                else if (scout_mod.Name.ToLower().Contains("acetyl"))
                {
                    cvMod.accession = "UNIMOD:1";
                    cvMod.cvRef = "UNIMOD";
                }
                else if (scout_mod.Name.ToLower().Contains("deamidation"))
                {
                    cvMod.accession = "UNIMOD:7";
                    cvMod.cvRef = "UNIMOD";
                }
                else
                {
                    cvMod.accession = "MS:1001460"; //MS:1001460 -> Unknown modifications;
                    cvMod.cvRef = "PSI-MS";
                }

                cvMod.name = scout_mod.Name;
                modif.cvParam = new CVParamType[1] { cvMod };
                //}

                modificationList.Add(modif);
            }

            #region get reaction sites for c-term and n-term modifications
            reactionSites_char = ScoutParams.CXLReagent.Targets.ToCharArray();
            reactionSites = string.Join(" ", reactionSites_char);

            #region crosslink donor
            SearchModificationType modifXLDonor = new SearchModificationType();//Donor
            modifXLDonor.massDelta = (float)ScoutParams.CXLReagent.WholeMass;
            modifXLDonor.fixedMod = false;
            modifXLDonor.residues = reactionSites;

            CVParamType cvXLDonorID = new CVParamType();
            cvXLDonorID.cvRef = "PSI-MS";
            cvXLDonorID.accession = "MS:1003392";
            cvXLDonorID.name = "search modification id";
            cvXLDonorID.value = "crosslink_donor";

            CVParamType cvParams_Mod_xlLinkDonor = new CVParamType();
            if (xlName.Contains("dsbu"))
            {
                cvParams_Mod_xlLinkDonor.name = "BuUrBu";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:02120";
            }
            else if (xlName.Contains("dsso"))
            {
                cvParams_Mod_xlLinkDonor.name = "DSSO";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:02126";
            }
            else if (xlName.Contains("dsbso"))
            {
                cvParams_Mod_xlLinkDonor.name = "azide-A-DSBSO";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:02222";
            }
            else if (xlName.Contains("zero-length"))
            {
                cvParams_Mod_xlLinkDonor.name = "Zero-Length";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:00008";
            }
            else if (xlName.Contains("bs3"))
            {
                cvParams_Mod_xlLinkDonor.name = "BS3";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:02000";
            }
            else if (xlName.Contains("dss"))
            {
                cvParams_Mod_xlLinkDonor.name = "DSS";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:02001";
            }
            else if (xlName.Contains("dsg"))
            {
                cvParams_Mod_xlLinkDonor.name = "DSG";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:02006";
            }
            else if (xlName.Contains("disulfide"))
            {
                cvParams_Mod_xlLinkDonor.name = "Disulfide";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:02009";
            }
            else if (xlName.Contains("dsau"))
            {
                cvParams_Mod_xlLinkDonor.name = "DSAU";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:09317";
            }
            else
            {
                cvParams_Mod_xlLinkDonor.name = "Unknown crosslinker";
                cvParams_Mod_xlLinkDonor.accession = "XLMOD:00004";
            }
            cvParams_Mod_xlLinkDonor.cvRef = "XLMOD";

            CVParamType cvXLDonor = new CVParamType();
            cvXLDonor.cvRef = "PSI-MS";
            cvXLDonor.accession = "MS:1002509";
            cvXLDonor.name = "crosslink donor";
            cvXLDonor.value = "0";

            if (!(xlName.Contains("bs3") ||
                xlName.Contains("dss") ||
                xlName.Contains("dsg") ||
                xlName.Contains("disulfide") ||
                xlName.Contains("zero-length")))//cleavable crosslinkers
            {
                CVParamType cvXLDonorCharacteristic1 = new CVParamType();
                cvXLDonorCharacteristic1.cvRef = "PSI-MS";
                cvXLDonorCharacteristic1.accession = "MS:1003390";
                cvXLDonorCharacteristic1.name = "crosslinker cleavage characteristics";
                if (xlName.Contains("dsso"))
                    cvXLDonorCharacteristic1.value = "A:54.0105647:ST";
                else if (xlName.Contains("dsbu"))
                    cvXLDonorCharacteristic1.value = "A:85.0527638:B";
                else if (xlName.Contains("dsbso"))
                    cvXLDonorCharacteristic1.value = "A:54.0105647:ST";
                else if (xlName.Contains("dsau"))
                    cvXLDonorCharacteristic1.value = "A:57.0214637:ST";

                CVParamType cvXLDonorCharacteristic2 = new CVParamType();
                cvXLDonorCharacteristic2.cvRef = "PSI-MS";
                cvXLDonorCharacteristic2.accession = "MS:1003390";
                cvXLDonorCharacteristic2.name = "crosslinker cleavage characteristics";
                if (xlName.Contains("dsso"))
                    cvXLDonorCharacteristic2.value = "T:85.9826354:A";
                else if (xlName.Contains("dsbu"))
                    cvXLDonorCharacteristic2.value = "B:111.032028:A";
                else if (xlName.Contains("dsbso"))
                    cvXLDonorCharacteristic2.value = "T:236.0177:A";
                else if (xlName.Contains("dsau"))
                    cvXLDonorCharacteristic2.value = "T:83.0007282:A";

                modifXLDonor.cvParam = new CVParamType[5] { cvXLDonorID, cvParams_Mod_xlLinkDonor, cvXLDonor, cvXLDonorCharacteristic1, cvXLDonorCharacteristic2 };
            }
            else//non-cleavable crosslinkers
                modifXLDonor.cvParam = new CVParamType[3] { cvXLDonorID, cvParams_Mod_xlLinkDonor, cvXLDonor };
            #endregion

            #region crosslink Acceptor
            SearchModificationType modifXLAcceptor = new SearchModificationType();//Acceptor
            modifXLAcceptor.massDelta = 0;
            modifXLAcceptor.fixedMod = false;
            modifXLAcceptor.residues = reactionSites;

            CVParamType cvXLAcceptorID = new CVParamType();
            cvXLAcceptorID.cvRef = "PSI-MS";
            cvXLAcceptorID.accession = "MS:1003392";
            cvXLAcceptorID.name = "search modification id";
            cvXLAcceptorID.value = "crosslink_acceptor";

            CVParamType cvParams_Mod_xlLinkAcceptor = new CVParamType();
            if (xlName.Contains("dsbu"))
            {
                cvParams_Mod_xlLinkAcceptor.name = "BuUrBu";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:02120";
            }
            else if (xlName.Contains("dsso"))
            {
                cvParams_Mod_xlLinkAcceptor.name = "DSSO";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:02126";
            }
            else if (xlName.Contains("dsbso"))
            {
                cvParams_Mod_xlLinkAcceptor.name = "azide-A-DSBSO";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:02222";
            }
            else if (xlName.Contains("zero-length"))
            {
                cvParams_Mod_xlLinkAcceptor.name = "Zero-Length";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:00008";
            }
            else if (xlName.Contains("bs3"))
            {
                cvParams_Mod_xlLinkAcceptor.name = "BS3";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:02000";
            }
            else if (xlName.Contains("dss"))
            {
                cvParams_Mod_xlLinkAcceptor.name = "DSS";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:02001";
            }
            else if (xlName.Contains("dsg"))
            {
                cvParams_Mod_xlLinkAcceptor.name = "DSG";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:02006";
            }
            else if (xlName.Contains("disulfide"))
            {
                cvParams_Mod_xlLinkAcceptor.name = "Disulfide";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:02009";
            }
            else if (xlName.Contains("dsau"))
            {
                cvParams_Mod_xlLinkAcceptor.name = "DSAU";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:09317";
            }
            else
            {
                cvParams_Mod_xlLinkAcceptor.name = "Unknown crosslinker";
                cvParams_Mod_xlLinkAcceptor.accession = "XLMOD:00004";
            }
            cvParams_Mod_xlLinkAcceptor.cvRef = "XLMOD";

            CVParamType cvXLAcceptor = new CVParamType();
            cvXLAcceptor.cvRef = "PSI-MS";
            cvXLAcceptor.accession = "MS:1002509";
            cvXLAcceptor.name = "crosslink acceptor";
            cvXLAcceptor.value = "0";

            if (!(xlName.Contains("bs3") ||
                xlName.Contains("dss") ||
                xlName.Contains("dsg") ||
                xlName.Contains("disulfide") ||
                xlName.Contains("zero-length")))//cleavable crosslinkers
            {
                CVParamType cvXLAcceptorCharacteristic1 = new CVParamType();
                cvXLAcceptorCharacteristic1.cvRef = "PSI-MS";
                cvXLAcceptorCharacteristic1.accession = "MS:1003390";
                cvXLAcceptorCharacteristic1.name = "crosslinker cleavage characteristics";
                if (xlName.Contains("dsso"))
                    cvXLAcceptorCharacteristic1.value = "A:54.0105647:ST";
                else if (xlName.Contains("dsbu"))
                    cvXLAcceptorCharacteristic1.value = "A:85.0527638:B";
                else if (xlName.Contains("dsbso"))
                    cvXLAcceptorCharacteristic1.value = "A:54.0105647:ST";
                else if (xlName.Contains("dsau"))
                    cvXLAcceptorCharacteristic1.value = "A:57.0214637:ST";

                CVParamType cvXLAcceptorCharacteristic2 = new CVParamType();
                cvXLAcceptorCharacteristic2.cvRef = "PSI-MS";
                cvXLAcceptorCharacteristic2.accession = "MS:1003390";
                cvXLAcceptorCharacteristic2.name = "crosslinker cleavage characteristics";
                if (xlName.Contains("dsso"))
                    cvXLAcceptorCharacteristic2.value = "T:85.9826354:A";
                else if (xlName.Contains("dsbu"))
                    cvXLAcceptorCharacteristic2.value = "B:111.032028:A";
                else if (xlName.Contains("dsbso"))
                    cvXLAcceptorCharacteristic2.value = "T:236.0177:A";
                else if (xlName.Contains("dsau"))
                    cvXLAcceptorCharacteristic2.value = "T:83.0007282:A";

                modifXLAcceptor.cvParam = new CVParamType[5] { cvXLAcceptorID, cvParams_Mod_xlLinkAcceptor, cvXLAcceptor, cvXLAcceptorCharacteristic1, cvXLAcceptorCharacteristic2 };
            }
            else
                modifXLAcceptor.cvParam = new CVParamType[3] { cvXLAcceptorID, cvParams_Mod_xlLinkAcceptor, cvXLAcceptor };
            #endregion

            modificationList.Add(modifXLDonor);
            modificationList.Add(modifXLAcceptor);

            #endregion

            specIdProtocolType.ModificationParams = modificationList.ToArray();
            #endregion

            //Add information about the enzyme used in search
            #region enzyme
            EnzymesType enzymes = new EnzymesType();
            EnzymeType enzyme = new EnzymeType();
            enzyme.id = "ENZ_0";
            enzyme.nTermGain = "H";
            enzyme.cTermGain = "OH";
            enzyme.semiSpecific = ScoutParams.EnzymeSpecificity == Enzyme.EnzymeSpecificity.SemiSpecific;
            enzyme.semiSpecificSpecified = true;
            enzyme.missedCleavages = ScoutParams.MiscleavageNum;
            enzyme.missedCleavagesSpecified = true;

            ParamListType enzymeListType = new ParamListType();

            CVParamType cvParamEnzymeType = new CVParamType();
            cvParamEnzymeType.name = ScoutParams.Enzyme.Name;
            cvParamEnzymeType.unitCvRef = "PSI-MS";
            cvParamEnzymeType.cvRef = "PSI-MS";
            if (ScoutParams.Enzyme.Name.ToLower().Contains("trypsin"))
            {
                cvParamEnzymeType.unitAccession = "MS:1001251";
                cvParamEnzymeType.accession = "MS:1001251";
            }
            else if (ScoutParams.Enzyme.Name.ToLower().Contains("lysn"))
            {
                cvParamEnzymeType.unitAccession = "MS:1003093";
                cvParamEnzymeType.accession = "MS:1003093";
            }
            else if (ScoutParams.Enzyme.Name.ToLower().Contains("lysc"))
            {
                cvParamEnzymeType.unitAccession = "MS:1001309";
                cvParamEnzymeType.accession = "MS:1001309";
            }
            else if (ScoutParams.Enzyme.Name.ToLower().Contains("gluc"))
            {
                cvParamEnzymeType.unitAccession = "MS:1001917";
                cvParamEnzymeType.accession = "MS:1001917";
            }
            else if (ScoutParams.Enzyme.Name.ToLower().Contains("argc"))
            {
                cvParamEnzymeType.unitAccession = "MS:1001303";
                cvParamEnzymeType.accession = "MS:1001303";
            }
            else if (ScoutParams.Enzyme.Name.ToLower().Contains("chymotrypsin"))
            {
                cvParamEnzymeType.unitAccession = "MS:1001306";
                cvParamEnzymeType.accession = "MS:1001306";
            }
            else if (ScoutParams.Enzyme.Name.ToLower().Contains("cnbr"))
            {
                cvParamEnzymeType.unitAccession = "MS:1001307";
                cvParamEnzymeType.accession = "MS:1001307";
            }
            else if (ScoutParams.Enzyme.Name.ToLower().Contains("aspn"))
            {
                cvParamEnzymeType.unitAccession = "OGG:3000054829";
                cvParamEnzymeType.accession = "OGG:3000054829";
            }
            else if (ScoutParams.Enzyme.Name.ToLower().Contains("thermolysin"))
            {
                cvParamEnzymeType.unitAccession = "OMIT:0014671";
                cvParamEnzymeType.accession = "OMIT:0014671";
            }

            enzymeListType.Items = new AbstractParamType[1] { cvParamEnzymeType };

            enzyme.SiteRegexp = ScoutParams.Enzyme.GetRegularExpression();

            enzyme.EnzymeName = enzymeListType;
            enzymes.Enzyme = new EnzymeType[1] { enzyme };
            specIdProtocolType.Enzymes = enzymes;
            #endregion

            //Add information about the value of MS2PPM
            #region fragment tolerance
            CVParamType cvParamFragmentPPM_Max = new CVParamType();
            cvParamFragmentPPM_Max.name = "search tolerance plus value";
            cvParamFragmentPPM_Max.unitName = "parts per million";
            cvParamFragmentPPM_Max.unitAccession = "UO:0000169";
            cvParamFragmentPPM_Max.value = ScoutParams.PPMMS2Tolerance.ToString();
            cvParamFragmentPPM_Max.unitCvRef = "UO";
            cvParamFragmentPPM_Max.cvRef = "PSI-MS";
            cvParamFragmentPPM_Max.accession = "MS:1001412";

            CVParamType cvParamFragmentPPM_Min = new CVParamType();
            cvParamFragmentPPM_Min.name = "search tolerance minus value";
            cvParamFragmentPPM_Min.unitName = "parts per million";
            cvParamFragmentPPM_Min.unitAccession = "UO:0000169";
            cvParamFragmentPPM_Min.value = ScoutParams.PPMMS2Tolerance.ToString();
            cvParamFragmentPPM_Min.unitCvRef = "UO";
            cvParamFragmentPPM_Min.cvRef = "PSI-MS";
            cvParamFragmentPPM_Min.accession = "MS:1001413";

            specIdProtocolType.FragmentTolerance = new CVParamType[2] { cvParamFragmentPPM_Max, cvParamFragmentPPM_Min };
            #endregion

            //Add information about the value of MS1PPM
            #region parent tolerance
            CVParamType cvParamParentPPM_Max = new CVParamType();
            cvParamParentPPM_Max.name = "search tolerance plus value";
            cvParamParentPPM_Max.unitName = "parts per million";
            cvParamParentPPM_Max.unitAccession = "UO:0000169";
            cvParamParentPPM_Max.value = ScoutParams.PPMMS1Tolerance.ToString();
            cvParamParentPPM_Max.unitCvRef = "UO";
            cvParamParentPPM_Max.cvRef = "PSI-MS";
            cvParamParentPPM_Max.accession = "MS:1001412";

            CVParamType cvParamParentPPM_Min = new CVParamType();
            cvParamParentPPM_Min.name = "search tolerance minus value";
            cvParamParentPPM_Min.unitName = "parts per million";
            cvParamParentPPM_Min.unitAccession = "UO:0000169";
            cvParamParentPPM_Min.value = ScoutParams.PPMMS1Tolerance.ToString();
            cvParamParentPPM_Min.unitCvRef = "UO";
            cvParamParentPPM_Min.cvRef = "PSI-MS";
            cvParamParentPPM_Min.accession = "MS:1001413";

            specIdProtocolType.ParentTolerance = new CVParamType[2] { cvParamParentPPM_Max, cvParamFragmentPPM_Min };
            #endregion

            //Add information about the cutoff result
            #region threshold
            ParamListType thresholdListType = new ParamListType();
            CVParamType cvThresholdTypeCSM = new CVParamType();
            cvThresholdTypeCSM.name = "crosslinked CSM-level global FDR";
            cvThresholdTypeCSM.accession = "MS:1003337";
            cvThresholdTypeCSM.value = Math.Round(_results.PostParams.CSM_FDR, 2).ToString();
            cvThresholdTypeCSM.cvRef = "PSI-MS";
            thresholdListType.Items = new CVParamType[1] { cvThresholdTypeCSM };
            specIdProtocolType.Threshold = thresholdListType;
            #endregion

            analysisProtocol.SpectrumIdentificationProtocol = new SpectrumIdentificationProtocolType[1] { specIdProtocolType };

            #region Protein Protocol
            ProteinDetectionProtocolType proteinDetectionProtocolType = new();
            proteinDetectionProtocolType.id = "PDP_1";
            proteinDetectionProtocolType.analysisSoftware_ref = "SCOUT_ID_Software";

            #region threshold
            ParamListType thresholdListTypeProteinProtocol = new ParamListType();
            CVParamType cvThresholdTypeResPair = new CVParamType();
            cvThresholdTypeResPair.name = "residue-pair-level global FDR";
            cvThresholdTypeResPair.accession = "MS:1002677";
            cvThresholdTypeResPair.value = Math.Round(_results.PostParams.ResPair_FDR, 2).ToString();
            cvThresholdTypeResPair.cvRef = "PSI-MS";

            CVParamType cvThresholdTypePPI = new CVParamType();
            cvThresholdTypePPI.name = "protein-pair-level global FDR";
            cvThresholdTypePPI.accession = "MS:1002676";
            cvThresholdTypePPI.value = Math.Round(_results.PostParams.PPI_FDR, 2).ToString();
            cvThresholdTypePPI.cvRef = "PSI-MS";

            thresholdListTypeProteinProtocol.Items = new CVParamType[2] { cvThresholdTypeResPair, cvThresholdTypePPI };
            proteinDetectionProtocolType.Threshold = thresholdListTypeProteinProtocol;
            #endregion

            analysisProtocol.ProteinDetectionProtocol = proteinDetectionProtocolType;
            #endregion

            MzidML.Data.AnalysisProtocolCollection = analysisProtocol;
            #endregion

            //Add information about SearchQueries and database used in search
            #region Data collection

            DataCollectionType dataCollection = new DataCollectionType();

            #region inputs
            InputsType inputs = new InputsType();

            #region source file
            SourceFileType srcFileType = new SourceFileType();
            srcFileType.location = ScoutParams.RawPath;
            srcFileType.id = "SourceFile_1";

            FileFormatType fileFormatType = new FileFormatType();
            CVParamType cvFileFormatType = new CVParamType();
            cvFileFormatType.name = "SEQUEST out file format";
            cvFileFormatType.accession = "MS:1001200";
            cvFileFormatType.cvRef = "PSI-MS";
            fileFormatType.cvParam = cvFileFormatType;
            srcFileType.FileFormat = fileFormatType;
            inputs.SourceFile = new SourceFileType[1] { srcFileType };
            #endregion

            #region search database
            SearchDatabaseType searchDBType = new SearchDatabaseType();
            FileFormatType searchDBfileFormatType = new FileFormatType();
            CVParamType cvSearchDBFileFormatType = new CVParamType();
            cvSearchDBFileFormatType.name = "FASTA format";
            cvSearchDBFileFormatType.accession = "MS:1001348";
            cvSearchDBFileFormatType.cvRef = "PSI-MS";
            searchDBfileFormatType.cvParam = cvSearchDBFileFormatType;
            searchDBType.FileFormat = searchDBfileFormatType;

            ParamType dbName = new ParamType();
            UserParamType usrParam = new UserParamType();
            usrParam.name = "";
            dbName.Item = usrParam;
            searchDBType.DatabaseName = dbName;

            searchDBType.location = ScoutParams.FastaFile;
            searchDBType.id = "SearchDB_1";
            searchDBType.numDatabaseSequences = TotalProteins;
            searchDBType.numDatabaseSequencesSpecified = true;

            inputs.SearchDatabase = new SearchDatabaseType[1] { searchDBType };
            #endregion

            #region spectra data

            List<SpectraDataType> spectraMzIdentMLList = new List<SpectraDataType>();
            List<string> fileNameList = (from csm in OriginalScoredCSMs.AsParallel()
                                         select System.IO.Path.GetFileName(csm.FileName)).Distinct().ToList();
            SpectraDataType specDataType = new SpectraDataType();

            FileInfo newOutputFile = new FileInfo(fpath);
            specDataType.location = newOutputFile.Name.Replace(".mzid", MZIDML_FILE_EXTENSION);
            specDataType.id = "SID_" + 0;
            MzidML.Data.id = newOutputFile.Name.Replace(".", "_");

            FileFormatType specDatafileFormatType = new FileFormatType();
            CVParamType cvSpecDataFileFormatType = new CVParamType();

            SpectrumIDFormatType specIDFormat = new SpectrumIDFormatType();
            CVParamType cvSpecIDType = new CVParamType();

            #region setting cvParams
            cvSpecDataFileFormatType.name = "MSn spectrum";
            cvSpecDataFileFormatType.accession = "MS:1001466";
            cvSpecDataFileFormatType.cvRef = "PSI-MS";

            cvSpecIDType.name = "multiple peak list nativeID format";
            cvSpecIDType.accession = "MS:1000774";
            cvSpecIDType.cvRef = "PSI-MS";
            #endregion

            specDatafileFormatType.cvParam = cvSpecDataFileFormatType;
            specIDFormat.cvParam = cvSpecIDType;

            specDataType.FileFormat = specDatafileFormatType;
            specDataType.SpectrumIDFormat = specIDFormat;
            spectraMzIdentMLList.Add(specDataType);

            inputs.SpectraData = spectraMzIdentMLList.ToArray();
            #endregion

            dataCollection.Inputs = inputs;
            #endregion

            #region analysis data

            AnalysisDataType analysisData = new AnalysisDataType();

            #region spectrum identification list
            SpectrumIdentificationListType specIDResult = new SpectrumIdentificationListType();
            specIDResult.id = "SIL_1";

            //Default values used in search like mz, product ion intensity and ppm error
            #region fragmentation table
            MeasureType measure_mz = new MeasureType();
            measure_mz.id = "Measure_MZ";
            CVParamType cvMZ = new CVParamType();
            cvMZ.cvRef = "PSI-MS";
            cvMZ.accession = "MS:1001225";
            cvMZ.unitCvRef = "PSI-MS";
            cvMZ.unitName = "m/z";
            cvMZ.unitAccession = "MS:1000040";
            cvMZ.name = "product ion m/z";
            measure_mz.cvParam = new CVParamType[1] { cvMZ };

            MeasureType measure_int = new MeasureType();
            measure_int.id = "Measure_Int";
            CVParamType cvInt = new CVParamType();
            cvInt.cvRef = "PSI-MS";
            cvInt.accession = "MS:1001226";
            cvInt.unitCvRef = "PSI-MS";
            cvInt.unitName = "number of counts";
            cvInt.unitAccession = "MS:1000131";
            cvInt.name = "product ion intensity";
            measure_int.cvParam = new CVParamType[1] { cvInt };

            MeasureType measure_error = new MeasureType();
            measure_error.id = "Measure_Error";
            CVParamType cvError = new CVParamType();
            cvError.cvRef = "PSI-MS";
            cvError.accession = "MS:1001227";
            cvError.unitCvRef = "PSI-MS";
            cvError.unitName = "m/z";
            cvError.unitAccession = "MS:1000040";
            cvError.name = "product ion m/z error";
            measure_error.cvParam = new CVParamType[1] { cvError };

            specIDResult.FragmentationTable = new MeasureType[3] { measure_mz, measure_int, measure_error };
            #endregion

            //Add information about each searchQuery identified
            #region spec id results
            List<SpectrumIdentificationResultType> spectrumResults = new List<SpectrumIdentificationResultType>();

            if (OriginalScoredCSMs[0].Spectrum == null)
            {
                ErrorMsg += "Spectra object is null.\n";
                return false;
            }
            List<MSUltraLight> tmsList = (from csm in OriginalScoredCSMs.AsParallel()
                                          where !String.IsNullOrEmpty(csm.Spectrum)
                                          select ScoutCore.FileManagement.Serializer.FromJson<MSUltraLight>(csm.Spectrum, false)).ToList();
            #region Fill spectraMzIdML
            peptID = 0;

            foreach (var _csm in OriginalScoredCSMs)
            {
                List<SpectrumIdentificationItemType> spectraList = new List<SpectrumIdentificationItemType>();

                int peptCount = 0;
                SpectrumIdentificationResultType specMzIdMLResultType = new SpectrumIdentificationResultType();
                specMzIdMLResultType.spectraData_ref = "SID_" + 0;

                #region define spectrumID
                var spectrum = ScoutCore.FileManagement.Serializer.FromJson<MSUltraLight>(_csm.Spectrum, false);
                int spectrumID = tmsList.FindIndex(a => a.ScanNumber == spectrum.ScanNumber &&
                a.CromatographyRetentionTime == spectrum.CromatographyRetentionTime &&
                a.Precursors.Count == spectrum.Precursors.Count &&
                a.Precursors.Count > 0 && spectrum.Precursors.Count > 0 &&
                a.Precursors[0].MZ == spectrum.Precursors[0].MZ &&
                a.Precursors[0].Z == spectrum.Precursors[0].Z &&
                a.Ions.Count == spectrum.Ions.Count &&
                a.Ions.Count > 0 && spectrum.Ions.Count > 0 &&
                a.Ions[0].MZ == spectrum.Ions[0].MZ &&
                a.Ions[0].Intensity == spectrum.Ions[0].Intensity);
                if (spectrumID != -1)
                {
                    specMzIdMLResultType.spectrumID = "index=" + spectrumID;
                    tmsList[spectrumID].ScanNumber = spectrumID;
                }
                else
                    specMzIdMLResultType.spectrumID = "index=-1";

                #endregion

                specMzIdMLResultType.id = "SIR_" + peptID;
                int specIdItemCount = 0;

                string peptide1 = _csm.AlphaPSM.Peptide.AsString;
                string peptide2 = "";
                if (_csm.BetaPSM != null)
                    peptide2 = _csm.BetaPSM.Peptide.AsString;

                if (_csm.BetaPSM != null)
                {
                    #region interlink
                    SpectrumIdentificationItemType specMzIdMLItemTypeAlpha = new SpectrumIdentificationItemType();//peptide Alpha
                    specMzIdMLItemTypeAlpha.passThreshold = true;
                    specMzIdMLItemTypeAlpha.rank = (peptCount + 1);
                    specMzIdMLItemTypeAlpha.peptide_ref = String.Join("", System.Text.ASCIIEncoding.Default.GetBytes(peptide1)) + "_" + peptID.ToString() + "_0";
                    specMzIdMLItemTypeAlpha.experimentalMassToCharge = _csm.PrecursorMZ;
                    specMzIdMLItemTypeAlpha.chargeState = Convert.ToInt32(_csm.PrecursorCharge);
                    specMzIdMLItemTypeAlpha.id = "SII_" + peptID + "_" + (peptCount + 1);
                    specMzIdMLItemTypeAlpha.calculatedMassToChargeSpecified = true;
                    specMzIdMLItemTypeAlpha.calculatedMassToCharge = _csm.TheoreticalMZ;
                    PeptideEvidenceRefType peptEvidenceRefTypeAlpha = new PeptideEvidenceRefType();
                    string protein1 = _csm.AlphaMappings.Select(a => a.Locus).Distinct().ToList()[0];
                    peptEvidenceRefTypeAlpha.peptideEvidence_ref = specMzIdMLItemTypeAlpha.peptide_ref + "_" + protein1;
                    specMzIdMLItemTypeAlpha.PeptideEvidenceRef = new PeptideEvidenceRefType[1] { peptEvidenceRefTypeAlpha };

                    #region cvParams Alpha
                    CVParamType cvXLIdentifiedAlpha = new CVParamType();
                    cvXLIdentifiedAlpha.accession = "MS:1002511";
                    cvXLIdentifiedAlpha.cvRef = "PSI-MS";
                    cvXLIdentifiedAlpha.value = "SII_" + peptID + "_" + (peptCount + 1) + "_" + specIdItemCount;
                    cvXLIdentifiedAlpha.name = "crosslink spectrum identification item";

                    CVParamType cvXLIdentifiedScoreAlpha = new CVParamType();
                    cvXLIdentifiedScoreAlpha.accession = "MS:1003408";
                    cvXLIdentifiedScoreAlpha.cvRef = "PSI-MS";

                    double? xl_score = 0;
                    _csm.Scores.TryGetValue("XLScore", out xl_score);
                    double? minDDP_score = 0;
                    _csm.Scores.TryGetValue("MinDDPScore", out minDDP_score);
                    double score = minDDP_score.Value * 0.25 + _csm.ClassificationScore * 0.55 + xl_score.Value * 0.2;//MinDDPScore + classification score + XLScore
                    cvXLIdentifiedScoreAlpha.value = Convert.ToString(score);

                    cvXLIdentifiedScoreAlpha.name = "Scout score";
                    specMzIdMLItemTypeAlpha.Items = new AbstractParamType[2] { cvXLIdentifiedAlpha, cvXLIdentifiedScoreAlpha };
                    #endregion

                    SpectrumIdentificationItemType specMzIdMLItemTypeBeta = new SpectrumIdentificationItemType();//peptide Beta
                    specMzIdMLItemTypeBeta.passThreshold = true;
                    specMzIdMLItemTypeBeta.rank = (peptCount + 1);
                    specMzIdMLItemTypeBeta.peptide_ref = String.Join("", System.Text.ASCIIEncoding.Default.GetBytes(peptide2)) + "_" + peptID.ToString() + "_1";
                    specMzIdMLItemTypeBeta.experimentalMassToCharge = _csm.PrecursorMZ;
                    specMzIdMLItemTypeBeta.chargeState = Convert.ToInt32(_csm.PrecursorCharge);
                    specMzIdMLItemTypeBeta.id = "SII_" + peptID + "_" + (peptCount + 2);
                    specMzIdMLItemTypeBeta.calculatedMassToChargeSpecified = true;
                    specMzIdMLItemTypeBeta.calculatedMassToCharge = _csm.TheoreticalMZ;
                    PeptideEvidenceRefType peptEvidenceRefTypeBeta = new PeptideEvidenceRefType();
                    string protein2 = _csm.BetaMappings.Select(a => a.Locus).Distinct().ToList()[0];
                    peptEvidenceRefTypeBeta.peptideEvidence_ref = specMzIdMLItemTypeBeta.peptide_ref + "_" + protein2;
                    specMzIdMLItemTypeBeta.PeptideEvidenceRef = new PeptideEvidenceRefType[1] { peptEvidenceRefTypeBeta };

                    #region cvParams Beta
                    CVParamType cvXLIdentifiedBeta = new CVParamType();
                    cvXLIdentifiedBeta.accession = "MS:1002511";
                    cvXLIdentifiedBeta.cvRef = "PSI-MS";
                    cvXLIdentifiedBeta.value = "SII_" + peptID + "_" + (peptCount + 1) + "_" + specIdItemCount;
                    cvXLIdentifiedBeta.name = "crosslink spectrum identification item";

                    CVParamType cvXLIdentifiedScoreBeta = new CVParamType();
                    cvXLIdentifiedScoreBeta.accession = "MS:1003408";
                    cvXLIdentifiedScoreBeta.cvRef = "PSI-MS";

                    xl_score = 0;
                    _csm.Scores.TryGetValue("XLScore", out xl_score);
                    minDDP_score = 0;
                    _csm.Scores.TryGetValue("MinDDPScore", out minDDP_score);
                    score = minDDP_score.Value * 0.25 + _csm.ClassificationScore * 0.55 + xl_score.Value * 0.2;//MinDDPScore + classification score + XLScore
                    cvXLIdentifiedScoreBeta.value = Convert.ToString(score);

                    cvXLIdentifiedScoreBeta.name = "Scout score";

                    specMzIdMLItemTypeBeta.Items = new AbstractParamType[2] { cvXLIdentifiedBeta, cvXLIdentifiedScoreBeta };
                    #endregion

                    spectraList.Add(specMzIdMLItemTypeAlpha);
                    spectraList.Add(specMzIdMLItemTypeBeta);
                    #endregion
                }
                else
                {
                    #region looplink
                    SpectrumIdentificationItemType specMzIdMLItemTypeLooplink = new SpectrumIdentificationItemType();
                    specMzIdMLItemTypeLooplink.passThreshold = true;
                    specMzIdMLItemTypeLooplink.rank = (peptCount + 1);
                    specMzIdMLItemTypeLooplink.peptide_ref = String.Join("", System.Text.ASCIIEncoding.Default.GetBytes(peptide1)) + "_" + peptID.ToString();
                    specMzIdMLItemTypeLooplink.experimentalMassToCharge = _csm.PrecursorMZ;
                    specMzIdMLItemTypeLooplink.chargeState = Convert.ToInt32(_csm.PrecursorCharge);
                    specMzIdMLItemTypeLooplink.id = "SII_" + peptID + "_" + (peptCount + 1);
                    specMzIdMLItemTypeLooplink.calculatedMassToChargeSpecified = true;
                    specMzIdMLItemTypeLooplink.calculatedMassToCharge = _csm.TheoreticalMZ;
                    PeptideEvidenceRefType peptEvidenceRefTypeLooplink = new PeptideEvidenceRefType();
                    string protein1 = _csm.AlphaMappings.Select(a => a.Locus).Distinct().ToList()[0];
                    peptEvidenceRefTypeLooplink.peptideEvidence_ref = specMzIdMLItemTypeLooplink.peptide_ref + "_" + protein1;
                    specMzIdMLItemTypeLooplink.PeptideEvidenceRef = new PeptideEvidenceRefType[1] { peptEvidenceRefTypeLooplink };

                    #region cvParams LoopLink
                    CVParamType cvXLIdentifiedLooplink = new CVParamType();
                    cvXLIdentifiedLooplink.accession = "MS:1003329";
                    cvXLIdentifiedLooplink.cvRef = "PSI-MS";
                    cvXLIdentifiedLooplink.name = "looplink spectrum identification item";

                    CVParamType cvXLIdentifiedScoreLooplink = new CVParamType();
                    cvXLIdentifiedScoreLooplink.accession = "MS:1003408";
                    cvXLIdentifiedScoreLooplink.cvRef = "PSI-MS";

                    double? xl_score = 0;
                    _csm.Scores.TryGetValue("XLScore", out xl_score);
                    double? poisson_score = 0;
                    _csm.Scores.TryGetValue("PoissonScore", out poisson_score);
                    double score = poisson_score.Value * 0.025 + _csm.ClassificationScore * 0.1 + xl_score.Value * 0.5;//PoissonScore + classification score + XLScore
                    cvXLIdentifiedScoreLooplink.value = Convert.ToString(score);

                    cvXLIdentifiedScoreLooplink.name = "Scout score";
                    specMzIdMLItemTypeLooplink.Items = new AbstractParamType[2] { cvXLIdentifiedLooplink, cvXLIdentifiedScoreLooplink };
                    #endregion

                    spectraList.Add(specMzIdMLItemTypeLooplink);
                    #endregion
                }

                peptCount++;
                peptID++;
                specIdItemCount++;

                specMzIdMLResultType.SpectrumIdentificationItem = spectraList.ToArray();
                spectrumResults.Add(specMzIdMLResultType);
            }

            #endregion

            XLFilteredResultsSaver.SaveSpectraToMS2(tmsList, fpath.Replace(".mzid", MZIDML_FILE_EXTENSION));

            specIDResult.SpectrumIdentificationResult = spectrumResults.ToArray();
            #endregion

            analysisData.SpectrumIdentificationList = new SpectrumIdentificationListType[1] { specIDResult };


            #endregion

            #region protein detection list
            ProteinDetectionListType ptnList = new ProteinDetectionListType();
            ptnList.id = "PDL_1";

            CVParamType cvCountIdentifiedProteins = new CVParamType();
            cvCountIdentifiedProteins.accession = "MS:1002404";
            cvCountIdentifiedProteins.cvRef = "PSI-MS";
            cvCountIdentifiedProteins.value = TotalProteins.ToString();
            cvCountIdentifiedProteins.name = "count of identified proteins";

            ptnList.Items = new AbstractParamType[1] { cvCountIdentifiedProteins };
            analysisData.ProteinDetectionList = ptnList;
            #endregion

            dataCollection.AnalysisData = analysisData;

            #endregion

            MzidML.Data.DataCollection = dataCollection;

            #endregion

            //Add information about the spectra identification
            #region Analysis Collection
            AnalysisCollectionType analysisCollection = new AnalysisCollectionType();

            //Add information about the amount of spectra identification
            #region Spectrum Identification

            SpectrumIdentificationType specIdType = new SpectrumIdentificationType();
            specIdType.spectrumIdentificationList_ref = "SIL_1";
            specIdType.spectrumIdentificationProtocol_ref = "SIP";
            specIdType.id = "SI";
            specIdType.activityDate = DateTime.Now;
            specIdType.activityDateSpecified = true;

            //Add information about all spectraMzIdentMLList (SourceFiles spectra)
            List<InputSpectraType> inputSpecList = new List<InputSpectraType>();
            foreach (SpectraDataType specDT in spectraMzIdentMLList)
            {
                InputSpectraType inputSpec = new InputSpectraType();
                inputSpec.spectraData_ref = specDT.id;
                inputSpecList.Add(inputSpec);
            }

            specIdType.InputSpectra = inputSpecList.ToArray();

            SearchDatabaseRefType sdb = new SearchDatabaseRefType();
            sdb.searchDatabase_ref = "SearchDB_1";
            specIdType.SearchDatabaseRef = new SearchDatabaseRefType[1] { sdb };

            analysisCollection.SpectrumIdentification = new SpectrumIdentificationType[1] { specIdType };

            #endregion

            #region Protein Detection
            //ProteinDetectionType proteinDetection = new ProteinDetectionType();
            //proteinDetection.id = "PD_1";
            //proteinDetection.activityDate = DateTime.Now;
            //proteinDetection.activityDateSpecified = true;
            //proteinDetection.proteinDetectionList_ref = "PDL_1";
            //proteinDetection.proteinDetectionProtocol_ref = "PDP_1";

            //InputSpectrumIdentificationsType inputSpecType = new InputSpectrumIdentificationsType();
            //inputSpecType.spectrumIdentificationList_ref = "SIL_1";
            //proteinDetection.InputSpectrumIdentifications = new InputSpectrumIdentificationsType[1] { inputSpecType };
            //analysisCollection.ProteinDetection = proteinDetection;
            #endregion

            MzidML.Data.AnalysisCollection = analysisCollection;
            #endregion

            //Add information about the references of the software
            #region References
            BibliographicReferenceType pa = new BibliographicReferenceType();
            pa.authors = "Milan Avila Clasen, Max Ruwolt, Louise U. Kurt, Fabio C Gozzo, Shuai Wang, Tao Chen, Paulo C Carvalho, Diogo Borges Lima, Fan Liu";
            pa.id = pa.doi = "10.1101/2023.11.30.569448";
            pa.issue = "0";
            pa.name = pa.title = "Proteome - scale recombinant standards and a robust high-speed search engine to advance cross-linking MS - based interactomics";
            pa.publication = "BioRxiv";
            pa.publisher = "CSH";
            pa.volume = "-1";
            pa.year = 2023;
            List<BibliographicReferenceType> refs = new List<BibliographicReferenceType>();
            refs.Add(pa);
            MzidML.Data.BibliographicReference = refs.ToArray();
            #endregion

            MzidML.Save(fpath);
            ProteinList.Clear();
            Notify("Saved to " + fpath);
            try
            {
                mzidFile1_3.Validate(fpath);
            }
            catch (ApplicationException e)
            {
                Console.WriteLine($"WARN: mzIdentML has been saved, but it has not been validated.\n{e.Message}\n{e.StackTrace}");
                ErrorMsg += "mzIdentML has been saved, but it has not been validated.\n";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the name of the parser.
        /// </summary>
        /// <value>
        /// The name of the parser.
        /// </value>
        public override string ParserName
        {
            get
            {
                return "PSI-PI mzIdentML (v1.3.0)";
            }
        }

        /// <summary>
        /// Method responsible for loading mzIdentML file
        /// </summary>
        /// <param name="mzid"></param>
        protected override void Load(string mzid)
        {
            MzidML = new mzidFile1_3();
            MzidML.Load(mzid);
            ProteinList = MzidML.ListProteins;
            PeptideList = MzidML.ListPeptides;
            if (MzidML.Data.DataCollection.AnalysisData.SpectrumIdentificationList.Length > 0)
                SpectrumList = new List<SpectrumIdentificationResultType>(MzidML.Data.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult);

            PeptideEvidenceList = GetPeptidesFromMzIdentFile(PeptideList, ProteinList, MzidML.ListEvidences);
        }

        /// <summary>
        /// Method responsible for getting all peptides in mzIdentML and associate them with their protein list
        /// </summary>
        /// <param name="PeptideList"></param>
        /// <param name="ProteinList"></param>
        /// <param name="PeptEvidenceList"></param>
        /// <returns></returns>
        private List<PeptideMzIdML> GetPeptidesFromMzIdentFile(List<PeptideType> PeptideList, List<DBSequenceType> ProteinList, List<PeptideEvidenceType> PeptEvidenceList)
        {
            List<PeptideMzIdML> returnList = new List<PeptideMzIdML>();

            int peptide_processed = 0;
            double peptideListLength = PeptideList.Count;

            foreach (PeptideType pept in PeptideList)
            {
                PeptideMzIdML peptMzIdML = new PeptideMzIdML();
                peptMzIdML.PeptideType = pept;

                #region get protein
                List<PeptideEvidenceType> peptEvidenceListTMP = (from peptEvidence in PeptEvidenceList
                                                                 where peptEvidence.peptide_ref.Equals(pept.id)
                                                                 select peptEvidence).ToList();
                if (peptEvidenceListTMP.Count == 0)
                {
                    throw new Exception(" It was not found the peptide sequence with reference = " + pept.id);
                }
                peptMzIdML.Proteins = new List<string>() { peptEvidenceListTMP[0].dBSequence_ref };
                #endregion

                returnList.Add(peptMzIdML);

                peptide_processed++;
                int new_progress = ((int)((double)peptide_processed / (peptideListLength) * 100)) / 2;
                if (new_progress > progress_reading_mzIdentML)
                {
                    progress_reading_mzIdentML = new_progress;
                }
            }

            return returnList;
        }

        /// <summary>
        /// Method responsible for adding protein information
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Name"></param>
        /// <param name="Accession"></param>
        /// <param name="Sequence"></param>
        public void AddProtein(string ID, string Name, string Accession, string Sequence, string Description)
        {
            DBSequenceType protein = new DBSequenceType();
            protein.id = ID;
            protein.name = Name;
            protein.accession = Accession;
            protein.searchDatabase_ref = "SearchDB_1";

            #region cvParams
            CVParamType cvParams_ptn = new CVParamType();
            cvParams_ptn.accession = "MS:1001088";
            cvParams_ptn.cvRef = "PSI-MS";
            cvParams_ptn.name = "protein description";
            cvParams_ptn.value = Description;
            protein.Seq = Sequence;
            protein.length = protein.Seq.Length;

            protein.Items = new CVParamType[1] { cvParams_ptn };
            #endregion

            ProteinList.Add(protein);
        }

        /// <summary>
        /// Method responsible for adding peptide information and fill PeptideEvidenceList too
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Sequence"></param>
        /// <param name="scout_params"></param>
        /// <param name="proteinList"></param>
        /// <param name="peptAlphaOrBeta"></param>
        /// <param name="isXL"></param>
        /// <param name="isAlphaPeptide"></param>
        /// <param name="posXL"></param>
        /// <param name="value"></param>
        public void AddPeptide(
            string ID,
            string Sequence,
            string CleanSequence,
            ScoutParameters scout_params,
            List<string> proteinList,
            int peptAlphaOrBeta,
            bool isXL = false,
            bool isAlphaPeptide = false,
            int posXL = 0,
            int value = 0)
        {
            PeptideMzIdML peptMzIdML = new PeptideMzIdML();

            PeptideType pept = new PeptideType();
            pept.id = ID + "_" + peptAlphaOrBeta;

            List<ModificationType> listModifications = new List<ModificationType>();

            #region Find ptms in peptide sequence
            List<int> posPtms = new List<int>();

            //Match all characters excepts the last one.
            MatchCollection mc = Regex.Matches(Sequence, "[(|)]");
            foreach (Match match in mc)
            {
                posPtms.Add(match.Index);
            }

            int deductionPTM = 0;
            if (posPtms.Count > 0)
            {
                for (int i = 0; i < posPtms.Count; i += 2)
                {
                    ModificationType modification = new ModificationType();
                    string ptmValue = Sequence.Substring(posPtms[i] + 1, posPtms[i + 1] - posPtms[i] - 1);

                    #region CV Params Modification
                    CVParamType cvParams_Mod = new CVParamType();

                    int index_mod = -1;
                    index_mod = (posPtms[i] - 1) > 0 ? posPtms[i] - 1 : posPtms[i + 1] + 1;

                    if (Sequence[index_mod] == 'C')
                    {
                        cvParams_Mod.accession = "UNIMOD:4";
                        cvParams_Mod.cvRef = "UNIMOD";
                        cvParams_Mod.name = "carbamidomethyl";
                    }
                    else if (Sequence[index_mod] == 'M')
                    {
                        cvParams_Mod.accession = "UNIMOD:35";
                        cvParams_Mod.cvRef = "UNIMOD";
                        cvParams_Mod.name = "oxidation";
                    }
                    else if (Sequence[index_mod] == '[' && ptmValue.Equals("+42.010600"))
                    {
                        cvParams_Mod.accession = "UNIMOD:1";
                        cvParams_Mod.cvRef = "UNIMOD";
                        cvParams_Mod.name = "acetyl";
                    }
                    else if (Sequence[index_mod] == 'N' || Sequence[(posPtms[i] - 1)] == 'Q')
                    {
                        cvParams_Mod.accession = "UNIMOD:7";
                        cvParams_Mod.cvRef = "UNIMOD";
                        cvParams_Mod.name = "deamidation";
                    }
                    else
                    {
                        cvParams_Mod.accession = "MS:1001460"; //MS:1001460 -> Unknown modifications;
                        cvParams_Mod.cvRef = "PSI-MS";
                        cvParams_Mod.name = "unknown modification";
                    }

                    modification.cvParam = new CVParamType[1] { cvParams_Mod };

                    #endregion

                    modification.locationSpecified = true;

                    if (deductionPTM == 0)
                    {
                        modification.location = posPtms[i];
                    }
                    else
                    {
                        modification.location = posPtms[i] - deductionPTM;
                    }
                    deductionPTM += ptmValue.Length + 2;//Included '(' and ')'

                    modification.monoisotopicMassDelta = Convert.ToDouble(ptmValue);
                    modification.monoisotopicMassDeltaSpecified = true;
                    char _res_mod = Sequence[index_mod];
                    if (_res_mod != '[')
                        modification.residues = new string[1] { _res_mod.ToString() };
                    listModifications.Add(modification);
                }
            }

            #endregion

            if (isXL)
            {
                #region modification that indicates the receiver and donor xl peptide
                ModificationType modification = new ModificationType();

                #region CV Params Modification
                CVParamType cvParams_Mod_xlLink_SearchModID = new CVParamType();//search modification ID ref
                cvParams_Mod_xlLink_SearchModID.name = "search modification id ref";
                cvParams_Mod_xlLink_SearchModID.accession = "MS:1003393";
                cvParams_Mod_xlLink_SearchModID.cvRef = "PSI-MS";

                if (isAlphaPeptide)
                {
                    cvParams_Mod_xlLink_SearchModID.value = "crosslink_donor";

                    CVParamType cvParams_Mod_xlLink = new CVParamType();//xlLink
                    string xlName = scout_params.CXLReagent.Name.ToLower();
                    if (xlName.Contains("dsbu"))
                    {
                        cvParams_Mod_xlLink.name = "BuUrBu";
                        cvParams_Mod_xlLink.accession = "XLMOD:02120";
                    }
                    else if (xlName.Contains("dsso"))
                    {
                        cvParams_Mod_xlLink.name = "DSSO";
                        cvParams_Mod_xlLink.accession = "XLMOD:02126";
                    }
                    else if (xlName.Contains("dsbso"))
                    {
                        cvParams_Mod_xlLink.name = "azide-A-DSBSO";
                        cvParams_Mod_xlLink.accession = "XLMOD:02222";
                    }
                    else if (xlName.Contains("zero-length"))
                    {
                        cvParams_Mod_xlLink.name = "Zero-Length";
                        cvParams_Mod_xlLink.accession = "XLMOD:00008";
                    }
                    else if (xlName.Contains("bs3"))
                    {
                        cvParams_Mod_xlLink.name = "BS3";
                        cvParams_Mod_xlLink.accession = "XLMOD:02000";
                    }
                    else if (xlName.Contains("dss"))
                    {
                        cvParams_Mod_xlLink.name = "DSS";
                        cvParams_Mod_xlLink.accession = "XLMOD:02001";
                    }
                    else if (xlName.Contains("dsg"))
                    {
                        cvParams_Mod_xlLink.name = "DSG";
                        cvParams_Mod_xlLink.accession = "XLMOD:02006";
                    }
                    else if (xlName.Contains("disulfide"))
                    {
                        cvParams_Mod_xlLink.name = "Disulfide";
                        cvParams_Mod_xlLink.accession = "XLMOD:02009";
                    }
                    else if (xlName.Contains("dsau"))
                    {
                        cvParams_Mod_xlLink.name = "DSAU";
                        cvParams_Mod_xlLink.accession = "XLMOD:09317";
                    }
                    else
                    {
                        cvParams_Mod_xlLink.name = "Unknown crosslinker";
                        cvParams_Mod_xlLink.accession = "XLMOD:00004";
                    }

                    cvParams_Mod_xlLink.cvRef = "XLMOD";

                    CVParamType cvParams_Mod_donor = new CVParamType();//donor
                    cvParams_Mod_donor.name = "crosslink donor";
                    cvParams_Mod_donor.accession = "MS:1002509";
                    cvParams_Mod_donor.cvRef = "PSI-MS";
                    cvParams_Mod_donor.value = value.ToString();

                    modification.cvParam = new CVParamType[3] { cvParams_Mod_xlLink_SearchModID, cvParams_Mod_xlLink, cvParams_Mod_donor };
                    modification.monoisotopicMassDelta = scout_params.CXLReagent.WholeMass;

                }
                else
                {
                    cvParams_Mod_xlLink_SearchModID.value = "crosslink_acceptor";

                    CVParamType cvParams_Mod_receiver = new CVParamType();//receiver
                    cvParams_Mod_receiver.name = "crosslink acceptor";
                    cvParams_Mod_receiver.accession = "MS:1002510";
                    cvParams_Mod_receiver.cvRef = "PSI-MS";
                    cvParams_Mod_receiver.value = value.ToString();

                    modification.cvParam = new CVParamType[2] { cvParams_Mod_xlLink_SearchModID, cvParams_Mod_receiver };
                    modification.monoisotopicMassDelta = 0.0;
                }
                #endregion

                modification.location = posXL + 1;//Crosslinking position (It begins the number 1)
                modification.locationSpecified = true;
                modification.residues = new string[1] { CleanSequence[posXL].ToString() };
                modification.monoisotopicMassDeltaSpecified = true;
                listModifications.Add(modification);
                #endregion

            }

            pept.Modification = listModifications.ToArray();
            pept.PeptideSequence = CleanSequence;

            peptMzIdML.PeptideType = pept;
            peptMzIdML.Proteins = proteinList;

            MzidML.ListPeptides.Add(pept);
            PeptideEvidenceList.Add(peptMzIdML);
        }

        /// <summary>
        /// Method responsible for adding looplink information and fill PeptideEvidenceList too
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Sequence"></param>
        /// <param name="CleanSequence"></param>
        /// <param name="scout_params"></param>
        /// <param name="proteinList"></param>
        /// <param name="posXL1"></param>
        /// <param name="value"></param>
        public void AddPeptideLooplink(
            string ID,
            string Sequence,
            string CleanSequence,
            ScoutParameters scout_params,
            List<string> proteinList,
            int posXL1 = 0,
            int posXL2 = 0,
            int value = 0)
        {
            PeptideMzIdML peptMzIdML = new PeptideMzIdML();

            PeptideType pept = new PeptideType();
            pept.id = ID;

            List<ModificationType> listModifications = new List<ModificationType>();

            #region Find ptms in peptide sequence
            List<int> posPtms = new List<int>();

            //Match all characters excepts the last one.
            MatchCollection mc = Regex.Matches(Sequence, "[(|)]");
            foreach (Match match in mc)
            {
                posPtms.Add(match.Index);
            }

            int deductionPTM = 0;
            if (posPtms.Count > 0)
            {
                for (int i = 0; i < posPtms.Count; i += 2)
                {
                    ModificationType modification = new ModificationType();
                    string ptmValue = Sequence.Substring(posPtms[i] + 1, posPtms[i + 1] - posPtms[i] - 1);

                    #region CV Params Modification
                    CVParamType cvParams_Mod = new CVParamType();

                    int index_mod = -1;
                    index_mod = (posPtms[i] - 1) > 0 ? posPtms[i] - 1 : posPtms[i + 1] + 1;

                    if (Sequence[index_mod] == 'C')
                    {
                        cvParams_Mod.accession = "UNIMOD:4";
                        cvParams_Mod.cvRef = "UNIMOD";
                        cvParams_Mod.name = "carbamidomethyl";
                    }
                    else if (Sequence[index_mod] == 'M')
                    {
                        cvParams_Mod.accession = "UNIMOD:35";
                        cvParams_Mod.cvRef = "UNIMOD";
                        cvParams_Mod.name = "oxidation";
                    }
                    else if (Sequence[index_mod] == '[' && ptmValue.Equals("+42.010600"))
                    {
                        cvParams_Mod.accession = "UNIMOD:1";
                        cvParams_Mod.cvRef = "UNIMOD";
                        cvParams_Mod.name = "acetyl";
                    }
                    else if (Sequence[index_mod] == 'N' || Sequence[(posPtms[i] - 1)] == 'Q')
                    {
                        cvParams_Mod.accession = "UNIMOD:7";
                        cvParams_Mod.cvRef = "UNIMOD";
                        cvParams_Mod.name = "deamidation";
                    }
                    else
                    {
                        cvParams_Mod.accession = "MS:1001460"; //MS:1001460 -> Unknown modifications;
                        cvParams_Mod.cvRef = "PSI-MS";
                        cvParams_Mod.name = "unknown modification";
                    }

                    modification.cvParam = new CVParamType[1] { cvParams_Mod };

                    #endregion

                    modification.locationSpecified = true;

                    if (deductionPTM == 0)
                    {
                        modification.location = posPtms[i];
                    }
                    else
                    {
                        modification.location = posPtms[i] - deductionPTM;
                    }
                    deductionPTM += ptmValue.Length + 2;//Included '(' and ')'

                    modification.monoisotopicMassDelta = Convert.ToDouble(ptmValue);
                    modification.monoisotopicMassDeltaSpecified = true;

                    char _res_mod = Sequence[index_mod];
                    if (_res_mod != '[')
                        modification.residues = new string[1] { _res_mod.ToString() };

                    listModifications.Add(modification);
                }
            }

            #endregion

            #region modificationXL that indicates the receiver and donor xl peptide
            ModificationType modificationXLDonor = new ModificationType();

            #region CV Params modificationXL
            CVParamType cvParams_Mod_xlLink = new CVParamType();//xlLink
            string xlName = scout_params.CXLReagent.Name.ToLower();
            if (xlName.Contains("dsbu"))
            {
                cvParams_Mod_xlLink.name = "BuUrBu";
                cvParams_Mod_xlLink.accession = "XLMOD:02120";
            }
            else if (xlName.Contains("dsso"))
            {
                cvParams_Mod_xlLink.name = "DSSO";
                cvParams_Mod_xlLink.accession = "XLMOD:02126";
            }
            else if (xlName.Contains("dsbso"))
            {
                cvParams_Mod_xlLink.name = "azide-A-DSBSO";
                cvParams_Mod_xlLink.accession = "XLMOD:02222";
            }
            else if (xlName.Contains("zero-length"))
            {
                cvParams_Mod_xlLink.name = "Zero-Length";
                cvParams_Mod_xlLink.accession = "XLMOD:00008";
            }
            else if (xlName.Contains("bs3"))
            {
                cvParams_Mod_xlLink.name = "BS3";
                cvParams_Mod_xlLink.accession = "XLMOD:02000";
            }
            else if (xlName.Contains("dss"))
            {
                cvParams_Mod_xlLink.name = "DSS";
                cvParams_Mod_xlLink.accession = "XLMOD:02001";
            }
            else if (xlName.Contains("dsg"))
            {
                cvParams_Mod_xlLink.name = "DSG";
                cvParams_Mod_xlLink.accession = "XLMOD:02006";
            }
            else if (xlName.Contains("disulfide"))
            {
                cvParams_Mod_xlLink.name = "Disulfide";
                cvParams_Mod_xlLink.accession = "XLMOD:02009";
            }
            else if (xlName.Contains("dsau"))
            {
                cvParams_Mod_xlLink.name = "DSAU";
                cvParams_Mod_xlLink.accession = "XLMOD:09317";
            }
            else
            {
                cvParams_Mod_xlLink.name = "Unknown crosslinker";
                cvParams_Mod_xlLink.accession = "XLMOD:00004";
            }

            cvParams_Mod_xlLink.cvRef = "XLMOD";

            CVParamType cvParams_Mod_donor = new CVParamType();//donor
            cvParams_Mod_donor.name = "crosslink donor";
            cvParams_Mod_donor.accession = "MS:1002509";
            cvParams_Mod_donor.cvRef = "PSI-MS";
            cvParams_Mod_donor.value = value.ToString();

            modificationXLDonor.cvParam = new CVParamType[2] { cvParams_Mod_xlLink, cvParams_Mod_donor };
            modificationXLDonor.monoisotopicMassDelta = scout_params.CXLReagent.WholeMass;


            #endregion

            modificationXLDonor.location = posXL1 + 1;//Crosslinking position (It begins the number 1)
            modificationXLDonor.locationSpecified = true;
            modificationXLDonor.residues = new string[1] { CleanSequence[posXL1].ToString() };
            modificationXLDonor.monoisotopicMassDeltaSpecified = true;
            listModifications.Add(modificationXLDonor);

            ModificationType modificationXLAcceptor = new ModificationType();
            modificationXLAcceptor.location = posXL2 + 1;//Crosslinking position (It begins the number 1)
            modificationXLAcceptor.locationSpecified = true;
            modificationXLAcceptor.residues = new string[1] { CleanSequence[posXL2].ToString() };

            CVParamType cvParams_Mod_acceptor = new CVParamType();//acceptor
            cvParams_Mod_acceptor.name = "crosslink acceptor";
            cvParams_Mod_acceptor.accession = "MS:1002510";
            cvParams_Mod_acceptor.cvRef = "PSI-MS";
            cvParams_Mod_acceptor.value = value.ToString();

            modificationXLAcceptor.cvParam = new CVParamType[1] { cvParams_Mod_acceptor };
            modificationXLAcceptor.monoisotopicMassDelta = 0.0;
            modificationXLAcceptor.monoisotopicMassDeltaSpecified = true;
            listModifications.Add(modificationXLAcceptor);
            #endregion

            pept.Modification = listModifications.ToArray();
            pept.PeptideSequence = CleanSequence;

            peptMzIdML.PeptideType = pept;
            peptMzIdML.Proteins = proteinList;

            MzidML.ListPeptides.Add(pept);
            PeptideEvidenceList.Add(peptMzIdML);
        }
    }
}