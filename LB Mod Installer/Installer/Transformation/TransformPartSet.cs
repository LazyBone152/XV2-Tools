using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.BCS;
using YAXLib;

namespace LB_Mod_Installer.Installer.Transformation
{
    public class TransformPartSets
    {
        public List<TransformPartSet> PartSets { get; set; }
    }

    public class TransformPartSet
    {
        [YAXAttributeForClass]
        public string Key { get; set; }
        [YAXAttributeForClass]
        public Race Race { get; set; }

        public PartSet PartSet { get; set; }
    }
}
