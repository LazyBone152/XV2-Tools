using System.Collections.Generic;

namespace Xv2CoreLib.ValuesDictionary
{
    public static class BCM
    {
        // OpponentSizeConditions, family mask 0x000F0000.
        public static Dictionary<uint, string> OpponentSize { get; private set; } = new Dictionary<uint, string>()
        {
            { 0x0, "All sizes (0x0)" },
            { 0x20000, "Small characters (0x20000)" },
            { 0x40000, "Default size (0x40000)" },
            { 0x50000, "Medium (0x50000)" },
            { 0x60000, "Medium Large (0x60000)" },
            { 0x70000, "Large (0x70000)" },
            { 0x80000, "Great Ape (0x80000)" }
        };

        // OpponentSizeConditions, low unknown bits, mask 0x0000000F.
        public static Dictionary<uint, string> OpponentSizeUnknown { get; private set; } = new Dictionary<uint, string>()
        {
            { 0x0, "None (0x0)" },
            { 0x1, "Unknown (0x1)" },
            { 0x3, "Unknown (0x3)" }
        };

        public static Dictionary<uint, string> ReceiverLinkId { get; private set; } = new Dictionary<uint, string>()
        {
            { 0x0, "None (0x0)" },
            { 0x1, "Combos (0x1)" },
            { 0x2, "Supers (0x2)" },
            { 0x4, "Ultimate / Awoken / Evasive (0x4)" },
            { 0x8, "Z-Vanish (0x8)" },
            { 0x10, "Ki Blasts (0x10)" },
            { 0x20, "Jump (0x20)" },
            { 0x40, "Guard (0x40)" },
            { 0x80, "Flying / Step Dash (0x80)" }
        };

        public static Dictionary<uint, string> CharacterCondition { get; private set; } = new Dictionary<uint, string>()
        {
            { 0, "None / Default" },
            { 1, "Custom Character (CAC)" },
            { 2, "Human Male (HUM)" },
            { 3, "Human Female (HUF)" },
            { 4, "Saiyan Male (SYM)" },
            { 5, "Saiyan Female (SYF)" },
            { 6, "Namekian (NMC)" },
            { 7, "Frieza Race (FRI)" },
            { 8, "Majin Male (MAM)" },
            { 9, "Majin Female (MAF)" }
        };
    }
}
