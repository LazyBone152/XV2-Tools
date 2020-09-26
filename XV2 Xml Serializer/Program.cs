using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YAXLib;
using System.Globalization;
using System.Threading;
using Xv2CoreLib;

namespace XV2_Xml_Serializer
{
    class Program
    {
        private static bool DEBUG_MODE = false; //controls whether the debug parsers are used

        static void Main(string[] args)
        {
            //for debugging only
            //args = new string[1] { @"special_event_voice - Copy.sev" };

#if DEBUG
            DEBUG_MODE = true;
#endif

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
            
            for (int i = 0; i < args.Count(); i++)
            {
                fileLocation = args[i];
                if(args.Count() > 1)
                {
                    Console.WriteLine(String.Format("Processing File {2} of {1}: \"{0}\"...\n", fileLocation, args.Count(), i + 1));
                }

                if (Directory.Exists(fileLocation))
                {
                    new Xv2CoreLib.EMB.XmlRepack(fileLocation);

                    //Used for debugging
                    //BulkParseInitial(fileLocation);
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
                                    new Xv2CoreLib.ECF.Parser(fileLocation, true);
                                    break;
                                case ".bsa":
                                    new Xv2CoreLib.BSA.Parser(fileLocation, true);
                                    break;
                                case ".bcm":
                                    new Xv2CoreLib.BCM_XML.Parser(fileLocation, true);
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
                                    new Xv2CoreLib.EAN.Parser(fileLocation, true);
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
                        new Xv2CoreLib.ECF.Deserializer(fileLocation);
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
                        new Xv2CoreLib.BCM_XML.Deserializer(fileLocation);
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
                case ".acb":
                    Xv2CoreLib.ACB_NEW.ACB_File.Load(fileLocation, true);
                    return true;
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
                case ".acb":
                    Xv2CoreLib.ACB_NEW.ACB_File.LoadXml(fileLocation, true);
                    return true;
                default:
                    return false;
            }
        }



        //Debug/testing code

        private static void BulkParseInitial(string fileLocation)
        {
            string[] files = Directory.GetFiles(fileLocation);
            string fileType = String.Empty;

            foreach (string s in files)
            {
                if (Path.GetExtension(s) != ".xml")
                {
                    fileType = Path.GetExtension(s);
                }
            }

            switch (fileType)
            {
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
                case ".eepk":
                    BulkParseEepk(fileLocation);
                    break;
                case ".bac":
                    BulkParseBac(fileLocation);
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
            List<string> values = new List<string>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".ema")
                {
                    Console.WriteLine(s);
                    var ema = Xv2CoreLib.EMA.EMA_File.Serialize(s, false);

                    if (ema.Animations != null)
                    {
                        foreach(var anim in ema.Animations)
                        {
                            foreach(var cmd in anim.Commands)
                            {
                                if (!values.Contains(cmd.I_03_a.ToString()) && cmd.I_02 == 3)
                                    values.Add(cmd.I_03_a.ToString());
                            }
                        }
                    }

                }
            }

            //log
            StringBuilder str = new StringBuilder();

            foreach(var value in values)
            {
                str.AppendLine(value);
            }

            File.WriteAllText("log.txt", str.ToString());


        }

        static void BulkParseEmp(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".emp")
                {
                    Console.WriteLine(s);
                    var ema = new Xv2CoreLib.EMP.Parser(s, false).empFile;
                    
                }
            }
        }
        
        static void BulkParseEcf(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".ecf")
                {
                    Console.WriteLine(s);
                    var ecf = new Xv2CoreLib.ECF.Parser(s, true).GetEcfFile();

                    if(ecf.Entries[0].Type0 != null)
                    {
                        for(int i = 0; i < ecf.Entries[0].Type0.Count(); i++)
                        {
                            int count = ecf.Entries[0].Type0[i].Keyframes.Count();
                            

                        }
                    }

                }
            }

        }

        static void BulkParseEepk(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".eepk")
                {
                    Console.WriteLine(s);
                    var eepk = new Xv2CoreLib.EEPK.Parser(s, false).GetEepkFile();
                    
                    if(eepk.Assets != null)
                    {
                        foreach(var e in eepk.Assets)
                        {
                            if(e.FILES[0] != "NULL" && e.I_16 == Xv2CoreLib.EEPK.AssetType.EMO)
                            {
                                Console.WriteLine("This");
                                Console.Read();
                            }
                        }
                    }
                    
                }
            }

        }
        
        static void BulkParseBac(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<string> UsedValues = new List<string>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".bac")
                {
                    
                    Console.WriteLine(s);
                    var bac = new Xv2CoreLib.BAC.Parser(s, false).GetBacFile();

                    if (bac.BacEntries == null) continue;
                    foreach (var entry in bac.BacEntries)
                    {
                        if(entry.Type0 == null) continue;

                        foreach (var anim in entry.Type0)
                        {

                            if (anim.I_12.HasFlag(Xv2CoreLib.BAC.BAC_Type0.AnimationFlags.Unk9))
                            {
                                Console.WriteLine($"({entry.Index}) This. ({anim.I_12})");
                                Console.Read();
                            }
                            if (anim.F_28 < 0f)
                            {
                                Console.WriteLine($"({entry.Index})F_28 = " + anim.F_28);
                                Console.Read();
                            }
                            if (anim.F_32 < 0f)
                            {
                                Console.WriteLine($"({entry.Index})F_32 = " + anim.F_32);
                                Console.Read();
                            }
                        }
                    }
                    

                }

                UsedValues.Sort();
                //Log
                StringBuilder log = new StringBuilder();

                foreach (string e in UsedValues)
                {
                    log.Append(e);
                    log.AppendLine();
                }

                File.WriteAllText("debug_bac_log.txt", log.ToString());
            }

        }

        static void BulkParseBsa(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".bsa")
                {
                    Console.WriteLine(s);
                    var bsaFile = new Xv2CoreLib.BSA.Parser(s, true).GetBsaFile();
                    

                }
            }

        }

        static void BulkParseBcs(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".bcs")
                {
                    Console.WriteLine(s);
                    new Xv2CoreLib.BCS.Parser(s, true);


                }
            }

        }

        static void BulkParseEmm(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<string> shaderTypes = new List<string>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".emm")
                {
                    Console.WriteLine(s);
                    new Xv2CoreLib.EMM.Parser(s, false, shaderTypes);


                }
            }

            //Debug log
            StringBuilder str = new StringBuilder();

            foreach(var shader in shaderTypes)
            {
                str.Append(shader).AppendLine();
            }

            File.WriteAllText("emm_debug_log.txt", str.ToString());

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
                    new Xv2CoreLib.EAN.Parser(s, false);

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
            string[] files = Directory.GetFiles(directory);

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".qxd")
                {
                    Console.WriteLine(s);
                    var qxd = new Xv2CoreLib.QXD.Parser(s, false).GetQxdFile();

                    foreach (var e in qxd.Quests)
                    {
                        
                    }


                }
            }

        }

        static void BulkParseDem(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            List<Xv2CoreLib.DEM.DebugInfo> debugInfo = new List<Xv2CoreLib.DEM.DebugInfo>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".dem")
                {
                    Console.WriteLine(s);
                    debugInfo = new Xv2CoreLib.DEM.Parser(s, false, debugInfo).debugList;
                }
            }
            StringBuilder log = new StringBuilder();

            foreach(var e in debugInfo)
            {
                StringBuilder countList = new StringBuilder();

                foreach(var c in e.Count)
                {
                    countList.Append(String.Format("{0}, ", c));
                }

                log.Append(String.Format("Flag = {0}/{1}, Counts = ({2})", e.I_04, e.I_06, countList.ToString()));
                log.AppendLine();
            }
            File.WriteAllText("dem_debug_log.txt", log.ToString());

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
            List<string> UsedValues = new List<string>();

            foreach (string s in files)
            {
                if (Path.GetExtension(s) == ".bdm")
                {

                    Console.WriteLine(s);
                    var bdm = new Xv2CoreLib.BDM.Parser(s, false);
                    
                    foreach(string value in bdm.UsedValues)
                    {
                        if (UsedValues.IndexOf(value) == -1)
                        {
                            UsedValues.Add(value);
                        }
                    }
                }

                UsedValues.Sort();
                //Log
                StringBuilder log = new StringBuilder();

                foreach (string e in UsedValues)
                {
                    log.Append(e);
                    log.AppendLine();
                }

                File.WriteAllText("debug_bdm_log.txt", log.ToString());
            }

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

                    //try
                    {
                        var acb = Xv2CoreLib.ACB_NEW.ACB_File.Load(s, false);
                        

                        foreach (var commandGroup in acb.CommandTables.GetIterator())
                        {
                            foreach(var command in commandGroup.Commands)
                            {
                                if(command.CommandType == (Xv2CoreLib.ACB_NEW.CommandType)67)
                                {
                                    //if(!UsedValues.Contains(command.Param2.ToString()))
                                     //   UsedValues.Add(command.Param2.ToString());
                                    Console.WriteLine($"This (Param1: {command.Param1}, Param2: {command.Param2}, Param3: {command.Param3}): {commandGroup.Index}");
                                    Console.ReadLine();
                                }
                            }
                        }

                    }
                    //catch (Exception ex)
                    {
                     //   Console.WriteLine(ex.Message);
                     //   Console.ReadLine();
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
        

        
    }
}
