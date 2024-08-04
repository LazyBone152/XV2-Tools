using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;
using Xv2CoreLib;
using Xv2CoreLib.Eternity;
using YAXLib;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Xv2CoreLib.Resource;

namespace XV2_Xml_Serializer
{
    class Program
    {
        private static bool DEBUG_MODE = false; 

        static void Main(string[] args)
        {
#if DEBUG
            //for debugging only
            args = new string[1] { @"E:\VS_Test\EMM/ALL EMM" };
            //args = new string[1] { @"E:\VS_Test\EMA" };

            DEBUG_MODE = true;
#endif
            //CpkExtract();
            //MatDecompile(args);
            //Xv2CoreLib.EMM.Analyzer.MaterialAnalyzer.Instance.AnalyzeMaterials();
            //return;

            string fileLocation = null;

            if (args.Length > 0)
            {
                fileLocation = args[0];
            }
            else
            {
                Environment.Exit(0);
            }
            
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            
            for (int i = 0; i < args.Length; i++)
            {
                fileLocation = args[i];
                if(args.Length > 1)
                {
                    Console.WriteLine(String.Format("Processing File {2} of {1}: \"{0}\"...\n", fileLocation, args.Length, i + 1));
                }

                if (Directory.Exists(fileLocation))
                {
                    //Used for debugging
#if DEBUG
                    BulkParseInitial(fileLocation);
#else
                    new Xv2CoreLib.EMB.XmlRepack(fileLocation);
#endif

                }
                else
                {
#if !DEBUG
                    try
#endif
                    {
                        if (!LoadBinaryInitial_Debug(fileLocation))
                        {
                            switch (Path.GetExtension(fileLocation))
                            {

                                case ".eepk":
                                    new Xv2CoreLib.EEPK.Parser(fileLocation, true);
                                    break;
                                case ".ers":
                                    new Xv2CoreLib.ERS.Parser(fileLocation, true);
                                    break;
                                case ".qxd":
                                    new Xv2CoreLib.QXD.Parser(fileLocation, true);
                                    break;
                                case ".qml":
                                    new Xv2CoreLib.QML.Parser(fileLocation, true);
                                    break;
                                case ".qsl":
                                    new Xv2CoreLib.QSL.Parser(fileLocation, true);
                                    break;
                                case ".qbt":
                                    new Xv2CoreLib.QBT.Parser(fileLocation, true);
                                    break;
                                case ".qed":
                                    new Xv2CoreLib.QED.Parser(fileLocation, true);
                                    break;
                                case ".bev":
                                    new Xv2CoreLib.BEV.Parser(fileLocation, true);
                                    break;
                                case ".qsf":
                                    new Xv2CoreLib.QSF.Parser(fileLocation, true);
                                    break;
                                case ".bdm":
                                    new Xv2CoreLib.BDM.Parser(fileLocation, true);
                                    break;
                                case ".msg":
                                    new Xv2CoreLib.MSG.Parser(fileLocation, true);
                                    break;
                                case ".tsd":
                                    new Xv2CoreLib.TSD.Parser(fileLocation, true);
                                    break;
                                case ".tnl":
                                    new Xv2CoreLib.TNL.Parser(fileLocation, true);
                                    break;
                                case ".emp":
                                    new Xv2CoreLib.EMP.Parser(fileLocation, true);
                                    break;
                                case ".ecf":
                                    new Xv2CoreLib.ECF_XML.Parser(fileLocation, true);
                                    break;
                                case ".bsa":
                                    new Xv2CoreLib.BSA.Parser(fileLocation, true);
                                    break;
                                case ".bcm":
                                    new Xv2CoreLib.BCM.Parser(fileLocation, true);
                                    break;
                                case ".bas":
                                    new Xv2CoreLib.BAS.Parser(fileLocation, true);
                                    break;
                                case ".bpe":
                                    new Xv2CoreLib.BPE.Parser(fileLocation, true);
                                    break;
                                case ".aig":
                                    new Xv2CoreLib.AIG.AIG_File(fileLocation, true);
                                    break;
                                case ".ata":
                                    new Xv2CoreLib.ATA.ATA_File(fileLocation, true);
                                    break;
                                case ".bai":
                                    new Xv2CoreLib.BAI.Parser(fileLocation, true);
                                    break;
                                case ".psa":
                                    new Xv2CoreLib.PSA.PSA_File(fileLocation, true);
                                    break;
                                case ".lcp":
                                    new Xv2CoreLib.LCP.LCP_File(fileLocation, true);
                                    break;
                                case ".cnc":
                                    Xv2CoreLib.CNC.CNC_File.Read(fileLocation, true);
                                    break;
                                case ".cns":
                                    Xv2CoreLib.CNS.CNS_File.Read(fileLocation, true);
                                    break;
                                case ".dem":
                                    new Xv2CoreLib.DEM.Parser(fileLocation, true);
                                    break;
                                case ".dse":
                                    Xv2CoreLib.DSE.DSE_File.ParseFile(fileLocation);
                                    break;
                                case ".dml":
                                    Xv2CoreLib.DML.DML_File.ParseFile(fileLocation);
                                    break;
                                case ".acb":
                                    Xv2CoreLib.UTF.UTF_File.ParseUtfFile(fileLocation);
                                    break;
                                case ".acf":
                                    Xv2CoreLib.UTF.UTF_File.ParseUtfFile(fileLocation);
                                    break;
                                case ".awb":
                                    Xv2CoreLib.AFS2.AFS2_File.ParseAsf2File(fileLocation);
                                    break;
                                case ".sav":
                                case ".dec":
                                    Xv2CoreLib.SAV.SAV_File.Load(fileLocation);
                                    break;
                                case ".tdb":
                                    Xv2CoreLib.TDB.TDB_File.Parse(fileLocation, true);
                                    break;
                                case ".fpf":
                                    Xv2CoreLib.FPF.FPF_File.Parse(fileLocation, true);
                                    break;
                                case ".pfl":
                                    Xv2CoreLib.PFL.PFL_File.Serialize(fileLocation, true);
                                    break;
                                case ".pfp":
                                    Xv2CoreLib.PFP.PFP_File.Serialize(fileLocation, true);
                                    break;
                                case ".psl":
                                    Xv2CoreLib.PSL.PSL_File.Serialize(fileLocation, true);
                                    break;
                                case ".oct":
                                    new Xv2CoreLib.OCT.Parser(fileLocation, true);
                                    break;
                                case ".occ":
                                    new Xv2CoreLib.OCC.Parser(fileLocation, true);
                                    break;
                                case ".oco":
                                    new Xv2CoreLib.OCO.Parser(fileLocation, true);
                                    break;
                                case ".ocp":
                                    new Xv2CoreLib.OCP.Parser(fileLocation, true);
                                    break;
                                case ".ocs":
                                    new Xv2CoreLib.OCS.Parser(fileLocation, true);
                                    break;
                                case ".odf":
                                    Xv2CoreLib.ODF.ODF_File.Serialize(fileLocation, true);
                                    break;
                                case ".pso":
                                    Xv2CoreLib.PSO.PSO_File.Serialize(fileLocation, true);
                                    break;
                                case ".ema":
                                    Xv2CoreLib.EMA.EMA_File.Serialize(fileLocation, true);
                                    break;
                                case ".pup":
                                    Xv2CoreLib.PUP.PUP_File.Serialize(fileLocation, true);
                                    break;
                                case ".aur":
                                    Xv2CoreLib.AUR.AUR_File.Serialize(fileLocation, true);
                                    break;
                                case ".psc":
                                    Xv2CoreLib.PSC.PSC_File.Serialize(fileLocation, true);
                                    break;
                                case ".ait":
                                    new Xv2CoreLib.AIT.AIT_File(fileLocation, true);
                                    break;
                                case ".cus":
                                    new Xv2CoreLib.CUS.Parser(fileLocation, true);
                                    break;
                                case ".cms":
                                    new Xv2CoreLib.CMS.Parser(fileLocation, true);
                                    break;
                                case ".idb":
                                    new Xv2CoreLib.IDB.Parser(fileLocation, true);
                                    break;
                                case ".bcs":
                                    new Xv2CoreLib.BCS.Parser(fileLocation, true);
                                    break;
                                case ".emm":
                                    new Xv2CoreLib.EMM.Parser(fileLocation, true);
                                    break;
                                case ".ean":
                                    new Xv2CoreLib.EAN.Parser(fileLocation, true, false);
                                    break;
                                case ".emb":
                                    new Xv2CoreLib.EMB_CLASS.Parser(fileLocation, true);
                                    break;
                                case ".cso":
                                    new Xv2CoreLib.CSO.Parser(fileLocation, true);
                                    break;
                                case ".bac":
                                    new Xv2CoreLib.BAC.Parser(fileLocation, true);
                                    break;
                                case ".esk":
                                    new Xv2CoreLib.ESK.Parser(fileLocation, true);
                                    break;
                                case ".emd":
                                    new Xv2CoreLib.EMD.Parser(fileLocation, true);
                                    break;
                                case ".amk":
                                    Xv2CoreLib.AMK.AMK_File.Read(fileLocation, true);
                                    break;
                                case ".obl":
                                    Xv2CoreLib.OBL.OBL_File.Parse(fileLocation, true);
                                    break;
                                case ".pal":
                                    Xv2CoreLib.PAL.PAL_File.Parse(fileLocation, true);
                                    break;
                                case ".sev":
                                    Xv2CoreLib.SEV.SEV_File.Parse(fileLocation, true);
                                    break;
                                case ".ttc":
                                    Xv2CoreLib.TTC.TTC_File.Parse(fileLocation, true);
                                    break;
                                case ".ttb":
                                    Xv2CoreLib.TTB.TTB_File.Parse(fileLocation, true);
                                    break;
                                case ".hci":
                                    Xv2CoreLib.HCI.HCI_File.Parse(fileLocation, true);
                                    break;
                                case ".cml":
                                    Xv2CoreLib.CML.CML_File.Parse(fileLocation, true);
                                    break;
                                case ".tnn":
                                    Xv2CoreLib.TNN.TNN_File.Parse(fileLocation, true);
                                    break;
                                case ".cst":
                                    Xv2CoreLib.CST.CST_File.CreateXml(fileLocation);
                                    break;
                                case ".emo":
                                    Xv2CoreLib.EMO.EMO_File.CreateXml(fileLocation);
                                    break;
                                case ".nsk":
                                    Xv2CoreLib.NSK.NSK_File.CreateXml(fileLocation);
                                    break;
                                case ".emg":
                                    Xv2CoreLib.EMG.EMG_File.CreateXml(fileLocation);
                                    break;
                                case ".x2s":
                                    {
                                        switch (Path.GetFileName(fileLocation))
                                        {
                                            case CharaSlotsFile.FILE_NAME_BIN:
                                                CharaSlotsFile.CreateXml(fileLocation);
                                                break;
                                            case StageSlotsFile.FILE_NAME_BIN:
                                            case StageSlotsFile.FILE_NAME_LOCAL_BIN:
                                                StageSlotsFile.CreateXml(fileLocation);
                                                break;
                                        }
                                    }
                                    break;
                                case ".sds":
                                    Xv2CoreLib.SDS.SDS_File.Parse(fileLocation, true);
                                    break;
                                case ".emz":
                                    if(!Path.GetFileName(fileLocation).Contains("_sds.")) goto default;
                                    Xv2CoreLib.SDS.SDS_File.Parse(fileLocation, true);
                                    break;
                                case ".ems":
                                    Xv2CoreLib.EMS.EMS_File.CreateXml(fileLocation);
                                    break;
                                case ".vlc":
                                    Xv2CoreLib.VLC.VLC_File.Parse(fileLocation, true);
                                    break;
                                case ".xml":
                                    LoadXmlInitial(fileLocation);
                                    break;
                                default:
                                    FileTypeNotSupported(fileLocation);
                                    break;
                            }
                        }
                    }
#if !DEBUG
                    catch (YAXException ex)
                    {
                        Console.WriteLine(String.Format("An error occured during the XML serialization process.\nThe given reason is: {0}\n\nFull Exception:\n{1}", ex.Message, ex.ToString()));
                        Console.ReadLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(String.Format("An error occured.\nThe given reason is: {0}\n\nFull Exception:\n{1}", ex.Message, ex.ToString()));
                        Console.ReadLine();
                    }
#endif
                }

            }


            Console.WriteLine("\nDone");
        }
        
        static void LoadXmlInitial(string fileLocation)
        {
            //if file is an XML, this code sends it to the correct deserializer

            if (!LoadXmlInitial_Debug(fileLocation))
            {
                switch (Path.GetExtension(Path.GetFileNameWithoutExtension(fileLocation)))
                {

                    case ".eepk":
                        new Xv2CoreLib.EEPK.Deserializer(fileLocation);
                        break;
                    case ".ers":
                        new Xv2CoreLib.ERS.Deserializer(fileLocation);
                        break;
                    case ".qxd":
                        new Xv2CoreLib.QXD.Deserializer(fileLocation);
                        break;
                    case ".qml":
                        new Xv2CoreLib.QML.Deserializer(fileLocation);
                        break;
                    case ".qsl":
                        new Xv2CoreLib.QSL.Deserializer(fileLocation);
                        break;
                    case ".qbt":
                        new Xv2CoreLib.QBT.Deserializer(fileLocation);
                        break;
                    case ".qed":
                        new Xv2CoreLib.QED.Deserializer(fileLocation);
                        break;
                    case ".bev":
                        new Xv2CoreLib.BEV.Deserializer(fileLocation);
                        break;
                    case ".qsf":
                        new Xv2CoreLib.QSF.Deserializer(fileLocation);
                        break;
                    case ".bdm":
                        new Xv2CoreLib.BDM.Deserializer(fileLocation);
                        break;
                    case ".msg":
                        new Xv2CoreLib.MSG.Deserializer(fileLocation);
                        break;
                    case ".tsd":
                        new Xv2CoreLib.TSD.Deserializer(fileLocation);
                        break;
                    case ".tnl":
                        new Xv2CoreLib.TNL.Deserializer(fileLocation);
                        break;
                    case ".emp":
                        new Xv2CoreLib.EMP.Deserializer(fileLocation);
                        break;
                    case ".ecf":
                        new Xv2CoreLib.ECF_XML.Deserializer(fileLocation);
                        break;
                    case ".bsa":
                        new Xv2CoreLib.BSA.Deserializer(fileLocation);
                        break;
                    case ".bas":
                        new Xv2CoreLib.BAS.Deserializer(fileLocation);
                        break;
                    case ".bpe":
                        new Xv2CoreLib.BPE.Deserializer(fileLocation);
                        break;
                    case ".aig":
                        var aig = (Xv2CoreLib.AIG.AIG_File)File_Ex.LoadXml(fileLocation, typeof(Xv2CoreLib.AIG.AIG_File));
                        aig.WriteFile(String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation)));
                        break;
                    case ".ata":
                        var ata = (Xv2CoreLib.ATA.ATA_File)File_Ex.LoadXml(fileLocation, typeof(Xv2CoreLib.ATA.ATA_File));
                        ata.WriteFile(String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation)));
                        break;
                    case ".bai":
                        new Xv2CoreLib.BAI.Deserializer(fileLocation);
                        break;
                    case ".lcp":
                        var lcp = (Xv2CoreLib.LCP.LCP_File)File_Ex.LoadXml(fileLocation, typeof(Xv2CoreLib.LCP.LCP_File));
                        lcp.WriteFile(String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation)));
                        break;
                    case ".psa":
                        var psa = (Xv2CoreLib.PSA.PSA_File)File_Ex.LoadXml(fileLocation, typeof(Xv2CoreLib.PSA.PSA_File));
                        psa.WriteFile(String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation)));
                        break;
                    case ".cnc":
                        Xv2CoreLib.CNC.CNC_File.ReadXmlAndWriteBinary(fileLocation);
                        break;
                    case ".cns":
                        Xv2CoreLib.CNS.CNS_File.ReadXmlAndWriteBinary(fileLocation);
                        break;
                    case ".dem":
                        new Xv2CoreLib.DEM.Deserializer(fileLocation);
                        break;
                    case ".bcm":
                        new Xv2CoreLib.BCM.Deserializer(fileLocation);
                        break;
                    case ".dse":
                        Xv2CoreLib.DSE.DSE_File.LoadXmlAndSave(fileLocation);
                        break;
                    case ".dml":
                        Xv2CoreLib.DML.DML_File.LoadXmlAndSave(fileLocation);
                        break;
                    case ".acb":
                        Xv2CoreLib.UTF.UTF_File.SaveUtfFile(fileLocation);
                        break;
                    case ".acf":
                        Xv2CoreLib.UTF.UTF_File.SaveUtfFile(fileLocation);
                        break;
                    case ".awb":
                        Xv2CoreLib.AFS2.AFS2_File.SaveAfs2File(fileLocation);
                        break;
                    case ".tdb":
                        Xv2CoreLib.TDB.TDB_File.WriteFromXml(fileLocation);
                        break;
                    case ".fpf":
                        Xv2CoreLib.FPF.FPF_File.Write(fileLocation);
                        break;
                    case ".pfl":
                        Xv2CoreLib.PFL.PFL_File.Deserialize(fileLocation);
                        break;
                    case ".pfp":
                        Xv2CoreLib.PFP.PFP_File.Deserialize(fileLocation);
                        break;
                    case ".psl":
                        Xv2CoreLib.PSL.PSL_File.Deserialize(fileLocation);
                        break;
                    case ".oct":
                        new Xv2CoreLib.OCT.Deserializer(fileLocation);
                        break;
                    case ".occ":
                        new Xv2CoreLib.OCC.Deserializer(fileLocation);
                        break;
                    case ".oco":
                        new Xv2CoreLib.OCO.Deserializer(fileLocation);
                        break;
                    case ".ocp":
                        new Xv2CoreLib.OCP.Deserializer(fileLocation);
                        break;
                    case ".ocs":
                        new Xv2CoreLib.OCS.Deserializer(fileLocation);
                        break;
                    case ".odf":
                        Xv2CoreLib.ODF.ODF_File.Deserialize(fileLocation);
                        break;
                    case ".pso":
                        Xv2CoreLib.PSO.PSO_File.Deserialize(fileLocation);
                        break;
                    case ".ema":
                        Xv2CoreLib.EMA.EMA_File.Deserialize(fileLocation);
                        break;
                    case ".pup":
                        Xv2CoreLib.PUP.PUP_File.Deserialize(fileLocation);
                        break;
                    case ".aur":
                        Xv2CoreLib.AUR.AUR_File.Deserialize(fileLocation);
                        break;
                    case ".psc":
                        Xv2CoreLib.PSC.PSC_File.Deserialize(fileLocation);
                        break;
                    case ".sav":
                    case ".dec":
                        Xv2CoreLib.SAV.SAV_File.SaveXml(fileLocation);
                        break;
                    case ".ait":
                        var ait = (Xv2CoreLib.AIT.AIT_File)File_Ex.LoadXml(fileLocation, typeof(Xv2CoreLib.AIT.AIT_File));
                        ait.WriteFile(String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation)));
                        break;
                    case ".bac":
                        new Xv2CoreLib.BAC.Deserializer(fileLocation);
                        break;
                    case ".cus":
                        new Xv2CoreLib.CUS.Deserializer(fileLocation);
                        break;
                    case ".cms":
                        new Xv2CoreLib.CMS.Deserializer(fileLocation);
                        break;
                    case ".idb":
                        new Xv2CoreLib.IDB.Deserializer(fileLocation);
                        break;
                    case ".bcs":
                        new Xv2CoreLib.BCS.Deserializer(fileLocation);
                        break;
                    case ".emm":
                        new Xv2CoreLib.EMM.Deserializer(fileLocation);
                        break;
                    case ".ean":
                        new Xv2CoreLib.EAN.Deserializer(fileLocation);
                        break;
                    case ".emb":
                        new Xv2CoreLib.EMB_CLASS.Deserializer(fileLocation);
                        break;
                    case ".cso":
                        new Xv2CoreLib.CSO.Deserializer(fileLocation);
                        break;
                    case ".esk":
                        new Xv2CoreLib.ESK.Deserializer(fileLocation);
                        break;
                    case ".amk":
                        Xv2CoreLib.AMK.AMK_File.SaveXml(fileLocation);
                        break;
                    case ".emd":
                        new Xv2CoreLib.EMD.Deserializer(fileLocation);
                        break;
                    case ".obl":
                        Xv2CoreLib.OBL.OBL_File.Write(fileLocation);
                        break;
                    case ".pal":
                        Xv2CoreLib.PAL.PAL_File.Write(fileLocation);
                        break;
                    case ".sev":
                        Xv2CoreLib.SEV.SEV_File.Write(fileLocation);
                        break;
                    case ".ttc":
                        Xv2CoreLib.TTC.TTC_File.Write(fileLocation);
                        break;
                    case ".ttb":
                        Xv2CoreLib.TTB.TTB_File.Write(fileLocation);
                        break;
                    case ".hci":
                        Xv2CoreLib.HCI.HCI_File.Write(fileLocation);
                        break;
                    case ".cml":
                        Xv2CoreLib.CML.CML_File.Write(fileLocation);
                        break;
                    case ".cst":
                        Xv2CoreLib.CST.CST_File.ConvertFromXml(fileLocation);
                        break;
                    case ".emo":
                        Xv2CoreLib.EMO.EMO_File.ConvertFromXml(fileLocation);
                        break;
                    case ".nsk":
                        Xv2CoreLib.NSK.NSK_File.ConvertFromXml(fileLocation);
                        break;
                    case ".emg":
                        Xv2CoreLib.EMG.EMG_File.ConvertFromXml(fileLocation);
                        break;
                    case ".tnn":
                        Xv2CoreLib.TNN.TNN_File.Write(fileLocation);
                        break;
                    case ".x2s":
                        {
                            switch (Path.GetFileName(fileLocation))
                            {
                                case CharaSlotsFile.FILE_NAME_XML:
                                    CharaSlotsFile.ConvertFromXml(fileLocation);
                                    break;
                                case StageSlotsFile.FILE_NAME_XML:
                                case StageSlotsFile.FILE_NAME_LOCAL_XML:
                                    StageSlotsFile.ConvertFromXml(fileLocation);
                                    break;
                            }
                            break;
                        }
                    case ".sds":
                        Xv2CoreLib.SDS.SDS_File.Write(fileLocation);
                        break;
                    case ".emz":
                        if (!Path.GetFileName(fileLocation).Contains("_sds.")) goto default;
                        Xv2CoreLib.SDS.SDS_File.Write(fileLocation);
                        break;
                    case ".ems":
                        Xv2CoreLib.EMS.EMS_File.SaveXml(fileLocation);
                        break;
                    default:
                        FileTypeNotSupported(fileLocation);
                        break;
                }
            }
        }

        static void FileTypeNotSupported(string fileName)
        {
            Console.WriteLine(String.Format("\"{0}\" file type not supported.", fileName));
            Console.ReadLine();
        }
        
        private static bool LoadBinaryInitial_Debug(string fileLocation)
        {
            if (DEBUG_MODE == false) return false;

            switch (Path.GetExtension(fileLocation))
            {
                case ".bcm":
                    new Xv2CoreLib.BCM.Parser(fileLocation, true);
                    return true;
                case ".tsr":
                    Xv2CoreLib.TSR.TSR_File.Parse(fileLocation, true);
                    return true;
                //case ".acb":
                    //Xv2CoreLib.ACB_NEW.ACB_File.Load(fileLocation, true);
                    //return true;
                default:
                    return false;
            }
        }

        private static bool LoadXmlInitial_Debug(string fileLocation)
        {

            if (DEBUG_MODE == false) return false;

            switch (Path.GetExtension(Path.GetFileNameWithoutExtension(fileLocation)))
            {
                case ".bcm":
                    new Xv2CoreLib.BCM.Deserializer(fileLocation);
                    return true;
                //case ".acb":
                //    Xv2CoreLib.ACB_NEW.ACB_File.LoadXml(fileLocation, true);
                 //   return true;
                default:
                    return false;
            }
        }


        //Debug/testing code
#if DEBUG
        private static void MatDecompile(string[] args)
        {
            foreach(var file in args)
            {
                var emm = Xv2CoreLib.EMM.EMM_File.LoadEmm(file);
                emm.DecompileMaterials();
                emm.CompileMaterials();
                emm.SaveBinaryEmmFile($"{Path.GetDirectoryName(file)}/{Path.GetFileNameWithoutExtension(file)}_dec.emm");
            }
        }

        private static void BulkParseInitial(string fileLocation)
        {
            string[] files = Directory.GetFiles(fileLocation);
            string fileType = String.Empty;

            foreach (string s in files)
            {
                if (Path.GetExtension(s) != ".xml")
                {
                    fileType = Path.GetExtension(s);
                    break;
                }
            }

            switch (fileType)
            {
                case ".emo":
                    BulkParseEmo(fileLocation);
                    break;
                case ".emd":
                    BulkParseEmd(fileLocation);
                    break;
                case ".bac":
                    BulkParseBac(fileLocation);
                    break;
                case ".ema":
                    BulkParseEma(fileLocation);
                    break;
                case ".amk":
                    BulkParseAmk(fileLocation);
                    break;
                case ".emp":
                    BulkParseEmp(fileLocation);
                    break;
                case ".ecf":
                    BulkParseEcf(fileLocation);
                    break;
                case ".etr":
                    BulkParseEtr(fileLocation);
                    break;
                case ".eepk":
                    BulkParseEepk(fileLocation);
                    break;
                case ".bsa":
                    BulkParseBsa(fileLocation);
                    break;
                case ".bcs":
                    BulkParseBcs(fileLocation);
                    break;
                case ".emm":
                    BulkParseEmm(fileLocation);
                    break;
                case ".ean":
                    BulkParseEan(fileLocation);
                    break;
                case ".bcm":
                    BulkParseBcm(fileLocation);
                    break;
                case ".qxd":
                    BulkParseQxd(fileLocation);
                    break;
                case ".dem":
                    BulkParseDem(fileLocation);
                    break;
                case ".bai":
                    BulkParseBai(fileLocation);
                    break;
                case ".emb":
                    BulkParseEmb(fileLocation);
                    break;
                case ".bev":
                    BulkParseBev(fileLocation);
                    break;
                case ".esk":
                    BulkParseEsk(fileLocation);
                    break;
                case ".bdm":
                    BulkParseBdm(fileLocation);
                    break;
                case ".fpf":
                    BulkParseFpf(fileLocation);
                    break;
                case ".tsr":
                    BulkParseTsr(fileLocation);
                    break;
                case ".acb":
                    BulkParseAcb(fileLocation);
                    break;
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        static void BulkParseEmo(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                //try
                {
                    if (Path.GetExtension(s) == ".emo")
                    {
                        Console.WriteLine(s);
                        var emo = Xv2CoreLib.EMO.EMO_File.Load(s);

                        if(emo != null)
                        {
                            foreach(var part in emo.Parts)
                            {
                                foreach(var emg in part.EmgFiles)
                                {
                                    foreach(var model in emg.EmgMeshes)
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
                //catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    // Console.ReadLine();
                }
            }

        }

        static void BulkParseEmd(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            //BacHomingParse(files);


            List<uint> values = new List<uint>();
            List<uint> valuesTotal = new List<uint>();

            foreach (string s in files)
            {
                try
                {
                    if (Path.GetExtension(s) == ".emd")
                    {
                        Console.WriteLine(s);
                        var emd = Xv2CoreLib.EMD.EMD_File.Load(s);

                        if (emd.Models != null)
                        {
                            foreach (var entry in emd.Models)
                            {
                                if (entry.Meshes != null)
                                {
                                    foreach (var mesh in entry.Meshes)
                                    {
                                        if(mesh.Submeshes != null)
                                        {
                                            foreach(var submesh in mesh.Submeshes)
                                            {
                                                int count = 0;
                                                for(int i = 0; i < submesh.TriangleListCount; i++)
                                                {
                                                    for(int a = 0; a < submesh.Triangles[i].BonesCount; a++)
                                                    {
                                                        count++;
                                                    }
                                                }

                                                if(count > 24)
                                                {
                                                    Console.WriteLine("Here: " + count);
                                                    Console.ReadLine();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    // Console.ReadLine();
                }
            }

            values.Sort();
            StringBuilder str = new StringBuilder();

            foreach (var value in values)
            {
                //str.AppendLine($"{value.ToString()} ({valuesTotal.Count(x => x == value)})");
                str.AppendLine($"{HexConverter.GetHexString(value)} ({valuesTotal.Count(x => x == value)})");

                //str.AppendLine($"{Convert.ToSingle(value)} ({valuesTotal.Count(x => x == value)})");
            }

            File.WriteAllText("emd_test.txt", str.ToString());

            Process.Start("emd_test.txt");
            Environment.Exit(0);
        }

        static void BulkParseBac(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            //BacHomingParse(files);
            

            List<ushort> values = new List<ushort>();
            List<ushort> valuesTotal = new List<ushort>();

            foreach (string s in files)
            {
                try
                {
                    if (Path.GetExtension(s) == ".bac")
                    {
                        Console.WriteLine(s);
                        var bac = Xv2CoreLib.BAC.BAC_File.Load(s);

                        if (bac.BacEntries != null)
                        {
                            foreach (var entry in bac.BacEntries)
                            {
                                if (entry.Type1 != null)
                                {
                                    foreach (var type in entry.Type1)
                                    {
                                        //if(type.DragX != 0f || type.DragY != 0f || type.DragZ != 0f)
                                        //{
                                        //    Console.WriteLine($"{type.DragX} {type.DragY} {type.DragZ}");
                                        //   Console.ReadLine();
                                        //}

                                        //if ((type.MinX > 0f || type.MinY > 0f || type.MinZ > 0f) && (type.MaxX > 0f || type.MaxY > 0f || type.MaxZ > 0f))
                                        {
                                            //Console.WriteLine($"MatrixFlags: {type.BoundingBoxType}\nHitboxFlags: {HexConverter.GetHexString(type.HitboxFlags)}\nI_20: {type.I_20}\nSize: {type.Size}\nPos:{type.PositionX} {type.PositionY} {type.PositionZ}\nMin:{type.MinX} {type.MinY} {type.MinZ}\nMax: {type.MaxX} {type.MaxY} {type.MaxZ}");
                                            //Console.ReadLine();
                                        }

                                        if (!values.Contains((ushort)((int)type.HitboxFlags & 0x000F)))
                                        {
                                            values.Add((ushort)((int)type.HitboxFlags & 0x000F));
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                   // Console.ReadLine();
                }
            }

            values.Sort();
            StringBuilder str = new StringBuilder();

            foreach(var value in values)
            {
                //str.AppendLine($"{value.ToString()} ({valuesTotal.Count(x => x == value)})");
                //str.AppendLine($"{HexConverter.GetHexString(value)} ({valuesTotal.Count(x => x == value)})");
                str.AppendLine($"{value}");
                //str.AppendLine($"{Convert.ToSingle(value)} ({valuesTotal.Count(x => x == value)})");
            }

            File.WriteAllText("bac_test.txt", str.ToString());

            Process.Start("bac_test.txt");
            Environment.Exit(0);
        }

        private static void BacTcParse(string[] files)
        {
            bool[] exists = new bool[100];
            float[] param1 = new float[100];
            float[] param2 = new float[100];
            float[] param3 = new float[100];
            float[] param4 = new float[100];
            float[] param5 = new float[100];


            foreach (string s in files)
            {
                try
                {
                    if (Path.GetExtension(s) == ".bac")
                    {
                        Console.WriteLine(s);
                        var bac = Xv2CoreLib.BAC.BAC_File.Load(s);

                        if (bac.BacEntries != null)
                        {
                            foreach (var entry in bac.BacEntries)
                            {
                                if (entry.Type15 != null)
                                {
                                    foreach (var type in entry.Type15)
                                    {
                                        if (type.Param1 != 0f) param1[type.FunctionType] = type.Param1;
                                        if (type.Param2 != 0f) param2[type.FunctionType] = type.Param2;
                                        if (type.Param3 != 0f) param3[type.FunctionType] = type.Param3;
                                        if (type.Param4 != 0f) param4[type.FunctionType] = type.Param4;
                                        if (type.Param5 != 0f) param5[type.FunctionType] = type.Param5;
                                        exists[type.FunctionType] = true;


                                    }
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    // Console.ReadLine();
                }
            }

            StringBuilder str = new StringBuilder();

            for(int i = 0; i < exists.Length; i++)
            {
                if(exists[i])
                    str.AppendLine($"{i}: {param1[i]}, {param2[i]}, {param3[i]}, {param4[i]}, {param5[i]}");
            }

            File.WriteAllText("bac_test.txt", str.ToString());

            Process.Start("bac_test.txt");
            Environment.Exit(0);
        }

        static void BulkParseAmk(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<int> values = new List<int>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".amk")
                {
                    Console.WriteLine(s);
                    var ema = Xv2CoreLib.AMK.AMK_File.Read(s, true);
                    
                    if(ema.Animations != null)
                    {
                        foreach(var anim in ema.Animations)
                        {
                            if(anim.I_08 != 1)
                            {
                                Console.WriteLine("This");
                                Console.ReadLine();
                            }

                            if(anim.Keyframes != null)
                            {
                                foreach(var keyframe in anim.Keyframes)
                                {
                                    if(!values.Contains(keyframe.I_02))
                                    {
                                        //values.Add(keyframe.I_02);
                                    }
                                }
                            }
                        }
                    }

                }
            }

            //values.Sort();
            //foreach(var value in values)
            //{
            //    Console.WriteLine(value);
            //}
            //Console.ReadLine();

        }
        
        static void BulkParseEma(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<int> values = new List<int>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".ema")
                {
                    Console.WriteLine(s);
                    var ema = Xv2CoreLib.EMA.EMA_File.Serialize(s, false);
                    bool done = false;

                    if (ema.Animations != null)
                    {
                        foreach(var anim in ema.Animations)
                        {
                           
                        }
                    }

                }
            }

            //log
            StringBuilder str = new StringBuilder();
            values.Sort();

            foreach (var value in values)
            {
                str.AppendLine(value.ToString());
            }
            File.WriteAllText("ema_test.txt", str.ToString());
            Process.Start("ema_test.txt");
            Environment.Exit(0);
        }

        class KeyframeGroups
        {
            public int Group;
            public int Parameter;
            public int Component;
            public int Count = 1;
            public List<float> Values = new List<float>();

            public KeyframeGroups(int group, int parameter, int component)
            {
                Group = group;
                Parameter = parameter;
                Component = component;
            }

            public void AddValue(float value)
            {
                if (!Values.Contains(value))
                {
                    Values.Add(value);
                    Count++;
                }
            }
        }

        static void BulkParseEmp(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<int> values = new List<int>();
            List<KeyframeGroups> groups = new List<KeyframeGroups>();
            
            foreach (string s in files)
            {
                //try
                {
                    if (Path.GetExtension(s) == ".emp")
                    {
                        Console.WriteLine(s);
                        var emp = new Xv2CoreLib.EMP_NEW.Parser(s, false).EmpFile;
                        //var emp = new Xv2CoreLib.EMP.Parser(s, false).empFile;
                        var particleEffects = emp.GetAllParticleEffects_DEBUG();


                        foreach (var particle in particleEffects)
                        {
                            foreach(var modifier in particle.Modifiers)
                            {
                                if(particle.Modifiers.Any(x => x.Type == modifier.Type && x != modifier))
                                {
                                    Console.WriteLine("this");
                                    Console.Read();
                                }
                            }
                            /*
                            if(particle.GroupKeyframedValues.Count > 0)
                            {
                                foreach(var group in particle.GroupKeyframedValues)
                                {
                                    foreach(var value in group.KeyframedValues)
                                    {
                                        KeyframeGroups keyframeGroup = groups.FirstOrDefault(x => x.Group == group.Type && x.Parameter == value.Value && x.Component == value.Component);

                                        if (keyframeGroup == null)
                                        {
                                            keyframeGroup = new KeyframeGroups(group.Type, value.Value, value.Component);
                                            groups.Add(keyframeGroup);
                                        }

                                        foreach(var keyframe in value.Keyframes)
                                        {
                                            keyframeGroup.AddValue(keyframe.Value);
                                        }
                                    }
                                }
                            }
                            */
                        }

                    }
                }
                //catch
                {

                }
            }

            //Console.WriteLine(values.Max());
            //Console.ReadLine();

            //log
            StringBuilder str = new StringBuilder();
            values.Sort();

            foreach (var value in groups.OrderBy(x => x.Group))
            {
                str.AppendLine($"Group: {value.Group}, Param/Comp: {value.Parameter}/{value.Component} = ({value.Values.Min()} - {value.Values.Max()}) (Sample Count: {value.Count})");
            }

            File.WriteAllText("emp_test.txt", str.ToString());
            Process.Start("emp_test.txt");
            Environment.Exit(0);
        }
        
        static void BulkParseEcf(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<string> values = new List<string>();
            int count = 0;

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".ecf")
                {
                    
                    Console.WriteLine(s);
                    var ecf = Xv2CoreLib.ECF.ECF_File.Load(s);

                    foreach(var node in ecf.Nodes)
                    {

                    }
                    
                }
            }

            Console.WriteLine(count);
            Console.ReadLine();

            //log
            StringBuilder str = new StringBuilder();

            foreach (var value in values)
            {
                str.AppendLine($"\"{value.ToString()}\",");
            }

            //File.WriteAllText("ecf_log.txt", str.ToString());
            //Process.Start("ecf_log.txt");
            Environment.Exit(0);
        }

        static void BulkParseEtr(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<string> values = new List<string>();
            List<KeyframeGroups> groups = new List<KeyframeGroups>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".etr")
                {
                    Console.WriteLine(s);
                    Xv2CoreLib.ETR.ETR_File etr = Xv2CoreLib.ETR.ETR_File.Load(s);

                    foreach (var node in etr.Nodes)
                    {
                        if(node.AttachBone == "a_x_spear_front2" || node.AttachBone2 == "a_x_spear_front2")
                        {
                            Console.WriteLine("This");
                            Console.ReadLine();
                        }

                        foreach (var modifier in node.Modifiers)
                        {
                            foreach (var value in modifier.KeyframedValues)
                            {
                                /*
                                KeyframeGroups keyframeGroup = groups.FirstOrDefault(x => x.Group == (int)modifier.Type && x.Parameter == value.Parameter && x.Component == value.Component);

                                if (keyframeGroup == null)
                                {
                                    keyframeGroup = new KeyframeGroups((int)modifier.Type, value.Parameter, value.Component);
                                    groups.Add(keyframeGroup);
                                }

                                foreach (var keyframe in value.Keyframes)
                                {
                                    if ((int)modifier.Type == 2)
                                    {
                                        Console.WriteLine("This");
                                        Console.ReadLine();
                                    }
                                    keyframeGroup.AddValue(keyframe.Value);
                                }
                                //if (!values.Contains(value.I_04))
                                //    values.Add(value.I_04);
                                */
                            }
                        }
                    }

                }
            }

            //log
            StringBuilder str = new StringBuilder();
            //values.Sort();

            foreach (var value in values)
            {
                str.AppendLine($"\"{value.ToString()}\",");
            }
            foreach (var value in groups.OrderBy(x => x.Group))
            {
                //str.AppendLine($"Group: {value.Group}, Param/Comp: {value.Parameter}/{value.Component} = ({value.Values.Min()} - {value.Values.Max()}) (Sample Count: {value.Count})");
            }

            //File.WriteAllText("etr_log.txt", str.ToString());
            //Process.Start("etr_log.txt");
            Environment.Exit(0);
        }


        static void BulkParseEepk(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<int> values = new List<int>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".eepk")
                {
                    Console.WriteLine(s);
                    try
                    {
                        var eepk = new Xv2CoreLib.EEPK.Parser(s, false).GetEepkFile();

                        if (eepk.Effects != null)
                        {
                            foreach (var effect in eepk.Effects)
                            {
                                if (effect.EffectParts != null)
                                {
                                    foreach (var effectPart in effect.EffectParts)
                                    {
                                        if(effectPart.AssetType == Xv2CoreLib.EEPK.AssetType.LIGHT && effectPart.ScaleMin != 1f && effectPart.ScaleMax != 1f)
                                        {
                                            Console.WriteLine(effectPart.ScaleMin + ", " + effectPart.ScaleMax);
                                            Console.ReadLine();
                                        }


                                    }
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                    
                }
            }

            StringBuilder str = new StringBuilder();

            foreach(var value in values)
            {
                str.AppendLine(value.ToString());
            }


            File.WriteAllText("eepk_values.txt", str.ToString());
            Process.Start("eepk_values.txt");
            Environment.Exit(0);
        }
        

        static void BulkParseBsa(string directory)
        {
            List<int> values = new List<int>();
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".bsa")
                {
                    Console.WriteLine(s);
                    var bsaFile = new Xv2CoreLib.BSA.Parser(s, false).GetBsaFile();
                    
                    foreach(var entry in bsaFile.BSA_Entries)
                    {
                        if(entry.Type8 != null)
                        {
                            foreach (var type in entry.Type8)
                            {
                                if (!values.Contains(type.I_02))
                                    values.Add(type.I_02);
                            }
                        }
                    }
                }
            }


            values.Sort();
            StringBuilder str = new StringBuilder();

            foreach (var value in values)
            {
                str.AppendLine(value.ToString());
            }

            File.WriteAllText("bsa_values.txt", str.ToString());
            Process.Start("bsa_values.txt");
            Environment.Exit(0);
        }

        static void BulkParseBcs(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<string> stuff = new List<string>();

            foreach (string s in files)
            {
                try
                {
                    if (Path.GetExtension(s) == ".bcs")
                    {
                        Console.WriteLine(s);
                        var bcs = new Xv2CoreLib.BCS.Parser(s).bcsFile;

                        foreach (var bone in bcs.SkeletonData1.Bones)
                        {
                            if (!stuff.Contains(bone.BoneName))
                                stuff.Add(bone.BoneName);
                        }

                        foreach (var bone in bcs.SkeletonData2.Bones)
                        {
                            if (!stuff.Contains(bone.BoneName))
                                stuff.Add(bone.BoneName);
                        }

                    }
                }
                catch
                {

                }
            }

            //Debug log
            StringBuilder str = new StringBuilder();

            foreach (var val in stuff)
            {
                str.Append(string.Format("\"{0}\",", val)).AppendLine();
                //str.Append(HexConverter.GetHexString(val)).AppendLine();
            }

            File.WriteAllText("bcs_debug_log.txt", str.ToString());
            Process.Start("bcs_debug_log.txt");
            Environment.Exit(0);
        }

        static void BulkParseEmm(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<float> value = new List<float>();

            foreach (string s in files)
            {
                try
                {
                    if (Path.GetExtension(s) == ".emm")
                    {
                        Console.WriteLine(s);
                        var emm = new Xv2CoreLib.EMM.Parser(s, false).emmFile;
                        emm.DecompileMaterials();

                        foreach (var mat in emm.Materials)
                        {
                            if (!value.Contains(mat.DecompiledParameters.BillboardType))
                            {
                                value.Add(mat.DecompiledParameters.BillboardType);
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            //Debug log
            StringBuilder str = new StringBuilder();
            value.Sort();

            foreach (var val in value)
            {
                str.Append(val).AppendLine();
            }

            File.WriteAllText("emm_debug_log.txt", str.ToString());
            Process.Start("emm_debug_log.txt");
            Environment.Exit(0);

        }

        static void BulkParseEan(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            int i = 0;

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".ean")
                {
                    Console.WriteLine(String.Format("{0} (File {1} of {2})", s, i, files.Count()));
                    var ean = new Xv2CoreLib.EAN.Parser(s, false, false).eanFile;

                    if(ean.Skeleton != null)
                    {
                        if(ean.Skeleton.Unk1 != null)
                        {
                            Console.WriteLine("this");
                            Console.ReadLine();
                        }
                    }

                    i++;
                }
            }

        }

        static void BulkParseBcm(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".bcm")
                {
                    Console.WriteLine(s);
                    var bcm = new Xv2CoreLib.BCM.Parser(s, false).GetBcmFile();
                    bcm.TestSearch();
                    

                }
            }

        }

        static void BulkParseQxd(string directory)
        {
            List<int> values = new List<int>();
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".qxd")
                {
                    Console.WriteLine(s);
                    var qxd = new Xv2CoreLib.QXD.Parser(s, false).GetQxdFile();

                    foreach (var quest in qxd.Quests)
                    {
                        if(quest.EquipReward != null)
                        {
                            foreach(var equipment in quest.EquipReward)
                            {
                                if (!values.Contains(equipment.I_28))
                                    values.Add(equipment.I_28);
                            }
                        }
                    }


                }
            }
            StringBuilder str = new StringBuilder();
            values.Sort();

            foreach (var value in values)
            {
                str.AppendLine($"{value}");
            }

            File.WriteAllText("qxd_test.txt", str.ToString());

            Process.Start("qxd_test.txt");
            Environment.Exit(0);

        }

        static void BulkParseDem(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<int> values = new List<int>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".dem")
                {
                    Console.WriteLine(s);
                    var dem = new Xv2CoreLib.DEM.Parser(s, true).demFile;

                    foreach(var sum in dem.Section2Entries)
                    {
                        foreach(var sub in sum.SubEntries)
                        {
                            if(sub.Type6_18_7 != null)
                            {
                                if (!values.Contains(sub.Type6_18_7.I_7))
                                    values.Add(sub.Type6_18_7.I_7);
                            }
                        }
                    }
                }
            }

            StringBuilder str = new StringBuilder();

            foreach (var value in values)
            {
                str.AppendLine($"{value.ToString()}");
                //str.AppendLine($"{HexConverter.GetHexString(value)}");

                //str.AppendLine($"{Convert.ToSingle(value)} ({valuesTotal.Count(x => x == value)})");
            }

            File.WriteAllText("dem_test.txt", str.ToString());

            Process.Start("dem_test.txt");
            Environment.Exit(0);

        }

        static void BulkParseBai(string directory)
        {
            List<int> _UsedValues = new List<int>();
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".bai")
                {
                    Console.WriteLine(s);
                    var bai = new Xv2CoreLib.BAI.Parser(s, false);

                    foreach (int i in bai.UsedValues)
                    {
                        if (!_UsedValues.Contains(i))
                        {
                            _UsedValues.Add(i);
                        }
                    }

                }
            }

            StringBuilder log = new StringBuilder();
            _UsedValues.Sort();

            foreach (int i in _UsedValues)
            {
                log.Append(i.ToString()).AppendLine();
            }

            File.WriteAllText("bai_log.txt", log.ToString());
        }

        static void BulkParseEmb(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".emb")
                {
                    Console.WriteLine(s);
                    var emb = new Xv2CoreLib.EMB_CLASS.Parser(s, false).GetEmbFile();
                    
                    //Adding DATA05
                    while(emb.Entry.Count <= 6)
                    {
                        if(emb.Entry.Count < 3)
                        {
                            emb.Entry.Add(emb.Entry[0]);
                        }
                        else
                        {
                            emb.Entry.Add(emb.Entry[2]);
                        }
                    }

                    //Saving
                    new Xv2CoreLib.EMB_CLASS.Deserializer(s, emb);
                }
            }
            
        }

        static void BulkParseBev(string directory)
        {
            List<int> _UsedValues = new List<int>();
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".bev")
                {
                    Console.WriteLine(s);
                    var bev = new Xv2CoreLib.BEV.Parser(s, false);

                    foreach (int i in bev.UsedValues)
                    {
                        if (!_UsedValues.Contains(i))
                        {
                            _UsedValues.Add(i);
                        }
                    }

                }
            }

            StringBuilder log = new StringBuilder();
            _UsedValues.Sort();

            foreach (int i in _UsedValues)
            {
                log.Append(i.ToString()).AppendLine();
            }

            File.WriteAllText("bev_log.txt", log.ToString());
        }

        static void BulkParseEsk(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            int i = 0;

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".esk")
                {
                    Console.WriteLine(String.Format("{0} (File {1} of {2})", s, i, files.Count()));
                    new Xv2CoreLib.ESK.Parser(s, false);
                    i++;
                }
            }

        }

        static void BulkParseBdm(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<ushort> values = new List<ushort>();

            foreach (string s in files)
            {
                try
                {
                    if (Path.GetExtension(s) == ".bdm")
                    {
                        Console.WriteLine(s);
                        var bdm = new Xv2CoreLib.BDM.Parser(s, false).bdmFile;

                        if (bdm.BDM_Type == Xv2CoreLib.BDM.BDM_Type.XV2_1)
                            bdm.ConvertToXv2_0();

                        if (bdm.BDM_Type == Xv2CoreLib.BDM.BDM_Type.XV1)
                            continue;

                        foreach (var entry in bdm.BDM_Entries)
                        {
                            foreach (var subEntry in entry.Type0Entries)
                            {
                                //if (!values.Contains(subEntry.DamageType))
                                //    values.Add(subEntry.DamageType);
                            }
                        }
                    }
                }
                catch
                {

                }

            }

            values.Sort();
            //Log
            StringBuilder log = new StringBuilder();

            foreach (var val in values)
            {
                log.AppendLine(val.ToString());
            }

            File.WriteAllText("bdm_test.txt", log.ToString());

            Process.Start("bdm_test.txt");
            Environment.Exit(0);
        }

        static void BulkParseFpf(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<int> UsedValues = new List<int>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".fpf")
                {
                    //UsedValues = FPF.FPF_File.Test(s, UsedValues);
                }

            }
            Console.Read();

        }

        static void BulkParseTsr(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<string> UsedValues = new List<string>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".tsr")
                {
                    Console.WriteLine(s);
                    var tsr = Xv2CoreLib.TSR.TSR_File.Parse(s, false);

                    foreach (var function in tsr.Functions)
                    {
                        foreach(var sentence in function.Sentences)
                        {
                            if(sentence.Value == "DBG_LOG_INTEGER")
                            {
                                Console.WriteLine(s);
                                Console.Read();
                            }
                            if (UsedValues.IndexOf(String.Format("{0}: {1}", sentence.Value, sentence.GetArgumentTypes)) == -1)
                            {
                                UsedValues.Add(String.Format("{0}: {1}", sentence.Value, sentence.GetArgumentTypes));
                            }
                        }
                    }
                }
                
                //Log
                StringBuilder log = new StringBuilder();

                foreach (string e in UsedValues)
                {
                    log.Append(e);
                    log.AppendLine();
                }

                File.WriteAllText("debug_tsr_log.txt", log.ToString());
            }

        }

        static void BulkParseAcb(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<string> UsedValues = new List<string>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".acb")
                {
                    Console.WriteLine(s);

                    try
                    {
                        var acb = Xv2CoreLib.ACB.ACB_File.Load(s, false);
                        

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.ReadLine();
                    }
                }
                
            }
            return;
            //Create text file
            StringBuilder log = new StringBuilder();
            
            for(int i = 0; i < UsedValues.Count; i++)
            {
                log.AppendLine($"{UsedValues[i]}");
            }

            File.WriteAllText("acb_debug.txt", log.ToString());
        }
        
        static void CpkExtract()
        {
            //Extract all of a specific file type
            Xv2CoreLib.CPK.CPK_Reader cpk = new Xv2CoreLib.CPK.CPK_Reader(@"C:\Program Files (x86)\Steam\steamapps\common\DB Xenoverse 2\cpk", false);

            var task = cpk.ExtractAll(@"C:\XV2_MultiThreadExtractTest", ".emb");
            task.Wait();

            Environment.Exit(0);
        }
#endif

    }
}
