using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Xv2CoreLib.Xv1SavFile
{
    public class Offsets
    {
        public const int CaC = 199404;
        public const int CaCSize = 880;

    }

    public class Xv1SavFile
    {
        public List<CaC> Characters { get; set; }

        public static Xv1SavFile Load(string path)
        {
            byte[] rawBytes = File.ReadAllBytes(path);
            List<byte> bytes = rawBytes.ToList();

            Xv1SavFile savFile = new Xv1SavFile() { Characters = new List<CaC>() };

            for (int i = 0; i < 8; i++)
            {
                savFile.Characters.Add(CaC.Load(rawBytes, bytes, i));
            }

            return savFile;
        }
    }

    public class CaC
    {
        public string RaceName
        {
            get
            {
                string name = "";
                SAV.CaC.RaceEnumDictionary.TryGetValue(Race, out name);
                return name;
            }
        }
        public SAV.Race Race { get; set; }
        public int Voice { get; set; }
        public int BodySize { get; set; }
        public ushort SkinColor1 { get; set; }
        public ushort SkinColor2 { get; set; }
        public ushort SkinColor3 { get; set; }
        public ushort SkinColor4 { get; set; }
        public ushort HairColor { get; set; }
        public ushort EyeColor { get; set; }
        public ushort MakeupColor1 { get; set; }
        public ushort MakeupColor2 { get; set; }
        public string Name { get; set; }
        public int FaceBase { get; set; }
        public int FaceForehead { get; set; }
        public int Eyes { get; set; }
        public int Nose { get; set; }
        public int Ears { get; set; }
        public int Hair { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int AttributePoints { get; set; }
        public int HEA { get; set; }
        public int KI { get; set; }
        public int ATK { get; set; }
        public int STR { get; set; }
        public int BLA { get; set; }
        public int STM { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }
        public int Gloves { get; set; }
        public int Shoes { get; set; }
        public int Accessory { get; set; }
        public int ZSoul { get; set; }
        public ushort TopColor1 { get; set; }
        public ushort TopColor2 { get; set; }
        public ushort TopColor3 { get; set; }
        public ushort TopColor4 { get; set; }
        public ushort BottomColor1 { get; set; }
        public ushort BottomColor2 { get; set; }
        public ushort BottomColor3 { get; set; }
        public ushort BottomColor4 { get; set; }
        public ushort GlovesColor1 { get; set; }
        public ushort GlovesColor2 { get; set; }
        public ushort GlovesColor3 { get; set; }
        public ushort GlovesColor4 { get; set; }
        public ushort ShoesColor1 { get; set; }
        public ushort ShoesColor2 { get; set; }
        public ushort ShoesColor3 { get; set; }
        public ushort ShoesColor4 { get; set; }
        public int SuperSkill1 { get; set; }
        public int SuperSkill2 { get; set; }
        public int SuperSkill3 { get; set; }
        public int SuperSkill4 { get; set; }
        public int UltimateSkill1 { get; set; }
        public int UltimateSkill2 { get; set; }
        public int EvasiveSkill { get; set; }

        public static CaC Load(byte[] rawBytes, List<byte> bytes, int cacIdx)
        {
            int offset = Offsets.CaC + (Offsets.CaCSize * cacIdx);

            return new CaC()
            {
                Race = (SAV.Race)BitConverter.ToInt32(rawBytes, offset + 4),
                Voice = BitConverter.ToInt32(rawBytes, offset + 8),
                BodySize = BitConverter.ToInt32(rawBytes, offset + 12),
                SkinColor1 = BitConverter.ToUInt16(rawBytes, offset + 20),
                SkinColor2 = BitConverter.ToUInt16(rawBytes, offset + 22),
                SkinColor3 = BitConverter.ToUInt16(rawBytes, offset + 24),
                SkinColor4 = BitConverter.ToUInt16(rawBytes, offset + 26),
                HairColor = BitConverter.ToUInt16(rawBytes, offset + 28),
                EyeColor = BitConverter.ToUInt16(rawBytes, offset + 30),
                MakeupColor1 = BitConverter.ToUInt16(rawBytes, offset + 32),
                MakeupColor2 = BitConverter.ToUInt16(rawBytes, offset + 34),
                Name = StringEx.GetString(bytes, offset + 36, false, StringEx.EncodingType.ASCII),
                FaceBase = BitConverter.ToInt32(rawBytes, offset + 100),
                FaceForehead = BitConverter.ToInt32(rawBytes, offset + 104),
                Eyes = BitConverter.ToInt32(rawBytes, offset + 108),
                Nose = BitConverter.ToInt32(rawBytes, offset + 112),
                Ears = BitConverter.ToInt32(rawBytes, offset + 116),
                Hair = BitConverter.ToInt32(rawBytes, offset + 120),
                Level = BitConverter.ToInt32(rawBytes, offset + 140),
                Experience = BitConverter.ToInt32(rawBytes, offset + 144),
                AttributePoints = BitConverter.ToInt32(rawBytes, offset + 148),
                HEA = BitConverter.ToInt32(rawBytes, offset + 152),
                KI = BitConverter.ToInt32(rawBytes, offset + 156),
                ATK = BitConverter.ToInt32(rawBytes, offset + 160),
                STR = BitConverter.ToInt32(rawBytes, offset + 164),
                BLA = BitConverter.ToInt32(rawBytes, offset + 168),
                STM = BitConverter.ToInt32(rawBytes, offset + 172),
                Top = BitConverter.ToInt32(rawBytes, offset + 176),
                Bottom = BitConverter.ToInt32(rawBytes, offset + 180),
                Gloves = BitConverter.ToInt32(rawBytes, offset + 184),
                Shoes = BitConverter.ToInt32(rawBytes, offset + 188),
                Accessory = BitConverter.ToInt32(rawBytes, offset + 192),
                ZSoul = BitConverter.ToInt32(rawBytes, offset + 196),
                TopColor1 = BitConverter.ToUInt16(rawBytes, offset + 200),
                TopColor2 = BitConverter.ToUInt16(rawBytes, offset + 202),
                TopColor3 = BitConverter.ToUInt16(rawBytes, offset + 204),
                TopColor4 = BitConverter.ToUInt16(rawBytes, offset + 206),
                BottomColor1 = BitConverter.ToUInt16(rawBytes, offset + 208),
                BottomColor2 = BitConverter.ToUInt16(rawBytes, offset + 210),
                BottomColor3 = BitConverter.ToUInt16(rawBytes, offset + 212),
                BottomColor4 = BitConverter.ToUInt16(rawBytes, offset + 214),
                GlovesColor1 = BitConverter.ToUInt16(rawBytes, offset + 216),
                GlovesColor2 = BitConverter.ToUInt16(rawBytes, offset + 218),
                GlovesColor3 = BitConverter.ToUInt16(rawBytes, offset + 220),
                GlovesColor4 = BitConverter.ToUInt16(rawBytes, offset + 222),
                ShoesColor1 = BitConverter.ToUInt16(rawBytes, offset + 224),
                ShoesColor2 = BitConverter.ToUInt16(rawBytes, offset + 226),
                ShoesColor3 = BitConverter.ToUInt16(rawBytes, offset + 228),
                ShoesColor4 = BitConverter.ToUInt16(rawBytes, offset + 230),
                SuperSkill1 = BitConverter.ToInt32(rawBytes, offset + 232),
                SuperSkill2 = BitConverter.ToInt32(rawBytes, offset + 236),
                SuperSkill3 = BitConverter.ToInt32(rawBytes, offset + 240),
                SuperSkill4 = BitConverter.ToInt32(rawBytes, offset + 244),
                UltimateSkill1 = BitConverter.ToInt32(rawBytes, offset + 248),
                UltimateSkill2 = BitConverter.ToInt32(rawBytes, offset + 252),
                EvasiveSkill = BitConverter.ToInt32(rawBytes, offset + 256),
            };
        }
    }
}