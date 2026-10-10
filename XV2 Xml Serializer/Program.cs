using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Xv2CoreLib;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMZ;
using Xv2CoreLib.Eternity;
using Xv2CoreLib.SDS;
using YAXLib.Exceptions;

namespace XV2_Xml_Serializer
{
    public static partial class Program
    {
        private static bool DEBUG_MODE = false;

        static void Main(string[] args)
        {
#if DEBUG
            //for debugging only
            //args = new string[1] { @"C:\Program Files (x86)\Steam\steamapps\common\DB Xenoverse 2\cpk\data_1_24\data\vfx\cmn\BTL_AURA.eepk" };
            //args = new string[1] { @"G:\VS_Test\EMB" };

            DEBUG_MODE = true;
#endif

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            //TestMain(args);

            string fileLocation = null;

            if (args.Length > 0)
            {
                fileLocation = args[0];
            }
            else
            {
                Environment.Exit(0);
            }
            
            for (int i = 0; i < args.Length; i++)
            {
                fileLocation = args[i];
                if(args.Length > 1)
                {
                    Console.WriteLine(string.Format("Processing File {2} of {1}: \"{0}\"...\n", fileLocation, args.Length, i + 1));
                }

                if (Directory.Exists(fileLocation))
                {
                    _ = new Xv2CoreLib.EMB.XmlRepack(fileLocation);
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
                                    Xv2CoreLib.EEPK.EEPK_File.CreateXml(fileLocation);
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
                                    new Xv2CoreLib.EMP_XML.Parser(fileLocation, true);
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
                                    Xv2CoreLib.AIT.AIT_File.Parse(fileLocation, true);
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
                                    EMB_SerializedFile.CreateXml(fileLocation);
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
                                    //if(!Path.GetFileName(fileLocation).Contains("_sds.")) goto default;
                                    //Xv2CoreLib.SDS.SDS_File.Parse(fileLocation, true);

                                    object emzData = EMZ_File.LoadData(File.ReadAllBytes(fileLocation));

                                    if(emzData is EMB_File emb)
                                    {
                                        EMB_SerializedFile serializedEmb = new EMB_SerializedFile(emb);
                                        serializedEmb.SaveAsXml(fileLocation + ".xml");
                                    }
                                    else if (emzData is SDS_File sds)
                                    {
                                        sds.SaveXml(fileLocation + ".xml");
                                    }

                                    break;
                                case ".ems":
                                    Xv2CoreLib.EMS.EMS_File.CreateXml(fileLocation);
                                    break;
                                case ".ikd":
                                    Xv2CoreLib.IKD.IKD_File.Parse(fileLocation, true);
                                    break;
                                case ".vlc":
                                    Xv2CoreLib.VLC.VLC_File.Parse(fileLocation, true);
                                    break;
                                case ".cdt":
                                    new Xv2CoreLib.CDT.Parser(fileLocation, true);
                                    break;
                                case ".map":
                                    Xv2CoreLib.FMP.FMP_File.SerializeToXml(fileLocation);
                                    break;
                                case ".spm":
                                    Xv2CoreLib.SPM.SPM_File.SerializeToXml(fileLocation);
                                    break;
                                case ".cat":
                                    Xv2CoreLib.CAT.CAT_File.Parse(fileLocation, true);
									break;
                                case ".hkx":
                                case ".hvk":
                                    Xv2CoreLib.Havok.HavokTagFile.SerializeToXml(fileLocation);
                                    break;
                                case ".cbs":
                                    Xv2CoreLib.CBS.CBS_File.Parse(fileLocation, true);
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
                        Console.WriteLine(string.Format("An error occured during the XML serialization process.\nThe given reason is: {0}\n\nFull Exception:\n{1}", ex.Message, ex.ToString()));
                        Console.ReadLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("An error occured.\nThe given reason is: {0}\n\nFull Exception:\n{1}", ex.Message, ex.ToString()));
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
                        Xv2CoreLib.EEPK.EEPK_File.SaveXml(fileLocation);
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
                        new Xv2CoreLib.EMP_XML.Deserializer(fileLocation);
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
                        Xv2CoreLib.AIT.AIT_File.Write(fileLocation);
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
                        EMB_SerializedFile.SaveXml(fileLocation);
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
                        //if (!Path.GetFileName(fileLocation).Contains("_sds.")) goto default;
                        //Xv2CoreLib.SDS.SDS_File.Write(fileLocation);

                        string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation));
                        object data = EMZ_File.LoadFromXml(fileLocation);

                        if(data is EMB_File emb)
                        {
                            emb.Save(saveLocation);
                        }
                        else if (data is SDS_File sds)
                        {
                            sds.Save(saveLocation);
                        }

                        break;
                    case ".ems":
                        Xv2CoreLib.EMS.EMS_File.SaveXml(fileLocation);
                        break;
                    case ".ikd":
                        Xv2CoreLib.IKD.IKD_File.Write(fileLocation);
                        break;
                    case ".vlc":
                        Xv2CoreLib.VLC.VLC_File.Write(fileLocation);
                        break;
                    case ".cdt":
                        new Xv2CoreLib.CDT.Deserializer(fileLocation);
                        break;
                    case ".map":
                        Xv2CoreLib.FMP.FMP_File.DeserializeFromXml(fileLocation);
                        break;
                    case ".spm":
                        Xv2CoreLib.SPM.SPM_File.DeserializeFromXml(fileLocation);
                        break;
                    case ".cat":
                        Xv2CoreLib.CAT.CAT_File.Write(fileLocation);
                        break;
                    case ".hkx":
                    case ".hvk":
                        Xv2CoreLib.Havok.HavokTagFile.DeserializeFromXml(fileLocation);
                        break;
                    case ".cbs":
                        Xv2CoreLib.CBS.CBS_File.Write(fileLocation);
                        break;
                    default:
                        FileTypeNotSupported(fileLocation);
                        break;
                }
            }
        }

        static void FileTypeNotSupported(string fileName)
        {
            Console.WriteLine(string.Format("\"{0}\" file type not supported.", fileName));
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

    }
}