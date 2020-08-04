using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YAXLib;

namespace LB_Mod_Installer.Installer
{
    //Cut down copy of LB_Mod_Installer.Installer.InstallerXml

    public class InstallerXml
    {
        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }
        [YAXAttributeFor("Author")]
        [YAXSerializeAs("value")]
        public string Author { get; set; }
        [YAXAttributeFor("Version")]
        [YAXSerializeAs("value")]
        public string VersionString { get; set; }

        [YAXAttributeFor("LightEmaFix")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string LightEmaFix { get; set; }

        [YAXDontSerializeIfNull]
        public Overrides UiOverrides { get; set; } = new Overrides();

        [YAXSerializeAs("InstallSteps")]
        public List<InstallStep> InstallOptionSteps { get; set; }
        [YAXSerializeAs("FilesToInstall")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "File")]
        public List<FilePath> InstallFiles { get; set; }
        
    }
    
    public class Overrides
    {
        //Overrides for UI elements such as brushes and preset InstallSteps go in here.
        //Everything should be optional.

        [YAXAttributeFor("DefaultBackground")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string DefaultBackgroundBrush { get; set; }
        [YAXAttributeFor("DefaultFontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string DefaultFontColor { get; set; }
        [YAXAttributeFor("InstallingBackground")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string InstallingBackgroundBrush { get; set; }
        [YAXAttributeFor("InstallingFontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string InstallingFontColor { get; set; }
        [YAXAttributeFor("UninstallingBackground")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string UninstallingBackgroundBrush { get; set; }
        [YAXAttributeFor("UninstallingFontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string UninstallingFontColor { get; set; }

        [YAXAttributeFor("TitleBarBackground")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string TitleBarBackground { get; set; }
        [YAXAttributeFor("TitleBarFontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string TitleBarFontColor { get; set; }

        //Preset InstallStep Overwrites
        [YAXDontSerializeIfNull]
        public InstallStep InstallConfirm { get; set; }
        [YAXDontSerializeIfNull]
        public InstallStep UninstallConfirm { get; set; }
        [YAXDontSerializeIfNull]
        public InstallStep ReinstallInitialPrompt { get; set; }
        [YAXDontSerializeIfNull]
        public InstallStep UpdateInitialPrompt { get; set; }
        [YAXDontSerializeIfNull]
        public InstallStep DowngradeInitialPrompt { get; set; }
    }

    //These are the steps displayed during the install process. They can have choices between different files to install, or just a message (for introduction - first step)
    public class InstallStep
    {
        public enum StepTypes
        {
            Message,
            Options,
            OptionsMultiSelect
        }
        
        //Attributes
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string HasFlag { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string RequireSelection { get; set; }

        //Elements
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public StepTypes StepType { get; set; }
        [YAXAttributeFor("StepName")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }
        [YAXAttributeFor("Message")]
        [YAXSerializeAs("value")]
        public string Message { get; set; }
        [YAXAttributeFor("FontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string FontColor { get; set; }
        [YAXAttributeFor("Background")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string BackgroundImagePath { get; set; }

        [YAXAttributeFor("SelectedOptions")]
        [YAXSerializeAs("Default")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [YAXDontSerializeIfNull]
        public List<int> SelectedOptions { get; set; }


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Option")]
        [YAXDontSerializeIfNull]
        public List<InstallOption> OptionList { get; set; } = new List<InstallOption>();
        
    }

    [YAXSerializeAs("Option")]
    public class InstallOption
    {

        //Class properties
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string SetFlag { get; set; }
        [YAXAttributeFor("Message")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }
        [YAXAttributeFor("FontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string FontColor { get; set; }
        [YAXAttributeFor("Background")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string BackgroundImagePath { get; set; }
        [YAXAttributeFor("ToolTip")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string Tooltip { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "File")]
        public List<FilePath> Paths { get; set; }

        [YAXDontSerialize]
        public int PathCount
        {
            get
            {
                if (Paths != null) return Paths.Count;
                return 0;
            }
        }

    }
    
    [YAXSerializeAs("File")]
    public class FilePath
    {
        [YAXAttributeFor("SourcePath")]
        [YAXSerializeAs("value")]
        public string SourcePath { get; set; }
        [YAXAttributeFor("InstallPath")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string InstallPath { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string Overwrite { get; set; }

        public bool AllowOverwrite()
        {
            if(String.IsNullOrWhiteSpace(Overwrite))
            {
                return false;
            }
            else if(Overwrite.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
