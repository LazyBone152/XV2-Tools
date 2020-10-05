﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YAXLib;
using Xv2CoreLib.HslColor;

namespace Xv2CoreLib.ETR
{
    //Partial parser
    //Doesn't support xml serialisation

    [Serializable]
    public class ETR_File
    {
        public byte[] Bytes { get; set; }

        public List<ETR_MainEntry> ETR_Entries { get; set; }
        public List<ETR_TextureEntry> ETR_TextureEntries { get; set; }
        

        public static ETR_File Load(byte[] bytes)
        {
            ETR_File etrFile = new ETR_File();
            etrFile.Bytes = bytes;
            etrFile.ETR_Entries = new List<ETR_MainEntry>();
            etrFile.ETR_TextureEntries = new List<ETR_TextureEntry>();
            
            int section1Count = BitConverter.ToInt16(bytes, 12);
            int section2Count = BitConverter.ToInt16(bytes, 14);

            //number of Extrude Points (LineShape)
            int section3Count = 0;

            int section1Offset = BitConverter.ToInt32(bytes, 16);
            int section2Offset = BitConverter.ToInt32(bytes, 20);

            
            int section3Offset = 0;

            for (int i = 0; i < section1Count; i++)
            {
                ETR_MainEntry newEntry = new ETR_MainEntry();


                //+16 is to skip the 4 floats for EndPoint that come BEFORE Extrude Points
                //XenoXMLConverter output has it come after 
                section3Offset = BitConverter.ToInt32(bytes, section1Offset + 116) + section1Offset + 16;


             
                section3Count = BitConverter.ToInt32(bytes, section1Offset + 156);

                //actual number of Extrude Points = number of Extrude Points in binary file + 1
                section3Count += section3Count >= 1 ? 1 : 0;
             



                newEntry.I_108 = BitConverter.ToUInt16(bytes, section1Offset + 108);
                newEntry.Color1_R = BitConverter.ToSingle(bytes, section1Offset + 120);
                newEntry.Color1_G = BitConverter.ToSingle(bytes, section1Offset + 124);
                newEntry.Color1_B = BitConverter.ToSingle(bytes, section1Offset + 128);
                newEntry.Color1_A = BitConverter.ToSingle(bytes, section1Offset + 132);
                newEntry.Color2_R = BitConverter.ToSingle(bytes, section1Offset + 136);
                newEntry.Color2_G = BitConverter.ToSingle(bytes, section1Offset + 140);
                newEntry.Color2_B = BitConverter.ToSingle(bytes, section1Offset + 144);
                newEntry.Color2_A = BitConverter.ToSingle(bytes, section1Offset + 148);

              
                for(int j = 0; j < section3Count; j++)
                {
                    float X = BitConverter.ToSingle(bytes, section3Offset);
                    float Y = BitConverter.ToSingle(bytes, section3Offset + 4);

                    newEntry.ExtrudePoints.Add(new Point(X, Y));

                    section3Offset += 8;
                }

                //Parse main entry
                etrFile.ETR_Entries.Add(newEntry);
                section1Offset += 176;
            }

            for(int i = 0; i < section2Count; i++)
            {
                etrFile.ETR_TextureEntries.Add(new ETR_TextureEntry()
                {
                    I_01 = bytes[section2Offset + 1]
                });
                section2Offset += 28;
            }


            return etrFile;
        }

        public byte[] Save(bool writeToDisk, string path = null)
        {
            int section1Count = BitConverter.ToInt16(Bytes, 12);
            int section2Count = BitConverter.ToInt16(Bytes, 14);
            int section3Count = 0;
            int section1Offset = BitConverter.ToInt32(Bytes, 16);
            int section2Offset = BitConverter.ToInt32(Bytes, 20);
            int section3Offset = 0;







            if (section1Count != ETR_Entries.Count)
            {
                throw new InvalidDataException("Etr save fail: number of section1 entries in binary file is not equal to the ones in code.");
            }

            if (section2Count != ETR_TextureEntries.Count)
            {
                throw new InvalidDataException("Etr save fail: number of section2 entries in binary file is not equal to the ones in code.");
            }

            for (int i = 0; i < ETR_Entries.Count; i++)
            {
                byte[] emmIndex = BitConverter.GetBytes(ETR_Entries[i].I_108);
                Bytes[section1Offset + 108] = emmIndex[0];
                Bytes[section1Offset + 109] = emmIndex[1];

                section3Offset = BitConverter.ToInt32(Bytes, section1Offset + 116) + section1Offset + 16;
                section3Count = BitConverter.ToInt32(Bytes, section1Offset + 156);



                section3Count += section3Count >= 1 ? 1 : 0;

                if (section3Count != ETR_Entries[i].ExtrudePoints.Count)
                {
                    throw new InvalidDataException($"Etr save fail: number of extrude points for part index {i} in binary file is not equal to the ones in code.");
                }

                //Colors
                Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].Color1_R), section1Offset + 120);
                Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].Color1_G), section1Offset + 124);
                Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].Color1_B), section1Offset + 128);
                Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].Color1_A), section1Offset + 132);
                Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].Color2_R), section1Offset + 136);
                Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].Color2_G), section1Offset + 140);
                Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].Color2_B), section1Offset + 144);
                Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].Color2_A), section1Offset + 148);



                for(int j = 0; j < ETR_Entries[i].ExtrudePoints.Count; j++)
                {
                    Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].ExtrudePoints[j].X), section3Offset);
                    Bytes = Utils.ReplaceRange(Bytes, BitConverter.GetBytes(ETR_Entries[i].ExtrudePoints[j].Y), section3Offset + 4);

                    section3Offset += 8;
                }

                section1Offset += 176;
            }


            for (int i = 0; i < ETR_TextureEntries.Count; i++)
            {
                Bytes[section2Offset + 1] = ETR_TextureEntries[i].I_01;
                section2Offset += 28;
            }

            if (writeToDisk)
            {
                File.WriteAllBytes(path, Bytes);
            }
            return Bytes;
        }

        public void RemoveColorAnimations()
        {
            //Parse the byte array and for all Type0 Animations that have Color Component R,G,B, change keyframes/floats to 0.

            int section1Count = BitConverter.ToInt16(Bytes, 12);
            int section1Offset = BitConverter.ToInt32(Bytes, 16);

            for (int i = 0; i < section1Count; i++)
            {
                int type0Offset = BitConverter.ToInt32(Bytes, section1Offset + 172);
                int type0Count = BitConverter.ToInt16(Bytes, section1Offset + 170);

                if(type0Count > 0 && type0Offset > 0)
                {
                   type0Offset += 168 + section1Offset;
                
                    for (int a = 0; a < type0Count; a++)
                    {
                        byte parameter = Bytes[type0Offset + 0];
                        byte component = Int4Converter.ToInt4(Bytes[type0Offset + 2])[0];

                        if(parameter == 0)
                        {
                            //Color Factor
                            if(component == 0 || component == 1 || component == 2)
                            {
                                //Nullify keyframe count
                                Bytes[type0Offset + 6] = 0;
                                Bytes[type0Offset + 7] = 0;
                            }
                        }

                        type0Offset += 16;
                    }
                }
                
                section1Offset += 176;
            }

        }

        public List<RgbColor> GetUsedColors()
        {
            if (ETR_Entries == null) return new List<RgbColor>();
            List<RgbColor> colors = new List<RgbColor>();

            foreach(var etrEntry in ETR_Entries)
            {
                //Color1
                if(etrEntry.Color1_R != 0.0 || etrEntry.Color1_G != 0.0 || etrEntry.Color1_B != 0.0)
                {
                    if (etrEntry.Color1_R != 1.0 || etrEntry.Color1_G != 1.0 || etrEntry.Color1_B != 1.0)
                    {
                        colors.Add(new RgbColor(etrEntry.Color1_R, etrEntry.Color1_G, etrEntry.Color1_B));
                    }
                }
                //Color2
                if (etrEntry.Color2_R != 0.0 || etrEntry.Color2_G != 0.0 || etrEntry.Color2_B != 0.0)
                {
                    if (etrEntry.Color2_R != 1.0 || etrEntry.Color2_G != 1.0 || etrEntry.Color2_B != 1.0)
                    {
                        colors.Add(new RgbColor(etrEntry.Color2_R, etrEntry.Color2_G, etrEntry.Color2_B));
                    }
                }
            }

            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness)
        {
            //Remove color animations as we cant hue shift them without a full ETR parser.
            RemoveColorAnimations();

            if (ETR_Entries == null) return;

            foreach(var etrEntry in ETR_Entries)
            {
                //Color1
                if (etrEntry.Color1_R != 0.0 || etrEntry.Color1_G != 0.0 || etrEntry.Color1_B != 0.0)
                {
                    if (etrEntry.Color1_R != 1.0 || etrEntry.Color1_G != 1.0 || etrEntry.Color1_B != 1.0)
                    {
                        HslColor.HslColor newCol1 = new RgbColor(etrEntry.Color1_R, etrEntry.Color1_G, etrEntry.Color1_B).ToHsl();
                        newCol1.ChangeHue(hue);
                        newCol1.ChangeSaturation(saturation);
                        newCol1.ChangeLightness(lightness);
                        RgbColor convertedColor = newCol1.ToRgb();
                        etrEntry.Color1_R = (float)convertedColor.R;
                        etrEntry.Color1_G = (float)convertedColor.G;
                        etrEntry.Color1_B = (float)convertedColor.B;
                    }
                }

                //Color2
                if (etrEntry.Color2_R != 0.0 || etrEntry.Color2_G != 0.0 || etrEntry.Color2_B != 0.0)
                {
                    if (etrEntry.Color2_R != 1.0 || etrEntry.Color2_G != 1.0 || etrEntry.Color2_B != 1.0)
                    {
                        HslColor.HslColor newCol1 = new RgbColor(etrEntry.Color2_R, etrEntry.Color2_G, etrEntry.Color2_B).ToHsl();
                        newCol1.ChangeHue(hue);
                        newCol1.ChangeSaturation(saturation);
                        newCol1.ChangeLightness(lightness);
                        RgbColor convertedColor = newCol1.ToRgb();
                        etrEntry.Color2_R = (float)convertedColor.R;
                        etrEntry.Color2_G = (float)convertedColor.G;
                        etrEntry.Color2_B = (float)convertedColor.B;
                    }
                }
            }

        }

        //scales all the Extrude Points found in all Parts
        public void ScaleETRParts(float scaleFactor)
        {
            foreach (var etrEntry in ETR_Entries)
            {
                foreach (var point in etrEntry.ExtrudePoints)
                {
                    point.X *= scaleFactor;
                    point.Y *= scaleFactor;
                }
            }
        }
    }

    [Serializable]
    public class ETR_MainEntry
    {
        public EMM.Material MaterialRef { get; set; }

        public List<Point> ExtrudePoints { get; set; }
        public ushort I_108 { get; set; } //EMM Index
        

        public float Color1_R { get; set; }
        public float Color1_G { get; set; }
        public float Color1_B { get; set; }
        public float Color1_A { get; set; }

        public float Color2_R { get; set; }
        public float Color2_G { get; set; }
        public float Color2_B { get; set; }
        public float Color2_A { get; set; }
        public ETR_MainEntry()
        {
            ExtrudePoints = new List<Point>();
        }
    }

    [Serializable]
    public class ETR_TextureEntry
    {

        public EMB_CLASS.EmbEntry TextureRef { get; set; }

        public byte I_01 { get; set; } = byte.MaxValue; //EMB Index
        
    }
    [Serializable]
    public class Point
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Point(float _x, float _y)
        {
            X = _x;
            Y = _y;
        }
      
    }



}
