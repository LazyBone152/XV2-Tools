using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Xv2CoreLib.SAV
{
    //Based on eternity's original C++ source code
    public static class Encryption
    {
        //Item1: EncryptedSize, Item2: DecryptedSize
        private static readonly Tuple<int, int>[] SaveFileSizes = new Tuple<int, int>[]
        {
            new Tuple<int, int>(Offsets.ENCRYPTED_SAVE_SIZE_V1, Offsets.DECRYPTED_SAVE_SIZE_V1),
            new Tuple<int, int>(Offsets.ENCRYPTED_SAVE_SIZE_V10, Offsets.DECRYPTED_SAVE_SIZE_V10),
            new Tuple<int, int>(Offsets.ENCRYPTED_SAVE_SIZE_V21, Offsets.DECRYPTED_SAVE_SIZE_V21),
            new Tuple<int, int>(Offsets.ENCRYPTED_SAVE_SIZE_V30, Offsets.DECRYPTED_SAVE_SIZE_V30)
        };

        private const uint ENCRYPTED_SIGNATURE = 0x4C018948;
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("PR]-<Q9*WxHsV8rcW!JuH7k_ug:T5ApX");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("_Y7]mD1ziyH#Ar=0");

        public static byte[] DecryptSaveFile(byte[] input)
        {
            //When enabled, perform data integrity checks
            const bool validate = true;

            if(Encoding.UTF8.GetString(input, 0, 4) == "#SAV")
            {
                //Save file is already decrypted
                return input;
            }

            if (SaveFileSizes.All(x => x.Item1 != input.Length))
            {
                throw new InvalidDataException($"DecryptSaveFile: Unrecognized file size. This is probably a newer version of the save format that is not yet supported.");
            }

            int encryptedSectionSize = BitConverter.ToInt32(input, 8);
            int decryptedSize = SaveFileSizes.FirstOrDefault(x => x.Item1 == input.Length).Item2;

            //Create copy of encrypted part
            byte[] copy = new byte[encryptedSectionSize];
            Buffer.BlockCopy(input, 0x20, copy, 0, encryptedSectionSize);

            if (validate)
            {
                byte[] encryptedSectionMd5Hash = new byte[0x10];
                Buffer.BlockCopy(input, 0x10, encryptedSectionMd5Hash, 0, 0x10);

                using (MD5 _md5 = MD5Cng.Create())
                {
                    byte[] computedHash = _md5.ComputeHash(copy);

                    if (!Utils.CompareArray(computedHash, encryptedSectionMd5Hash))
                    {
                        throw new InvalidDataException("DecryptSaveFile: Md5 mismatch");
                    }
                }
            }

            //Decrypt the encrypted header
            //This contains the secondary key that is needed to decrypt the rest of the file
            AesCtrEncrypt(copy, 0, 0x80, Key, IV);

            if (Encoding.UTF8.GetString(copy, 0, 4) == "#SAV")
            {
                byte[] key2 = new byte[32];
                byte[] iv2 = new byte[16];

                if ((copy[0x5] & 4) != 0)
                {
                    Buffer.BlockCopy(copy, 0x4C, key2, 0, 32);
                    Buffer.BlockCopy(copy, 0x6C, iv2, 0, 16);
                }
                else
                {
                    Buffer.BlockCopy(copy, 0x1C, key2, 0, 32);
                    Buffer.BlockCopy(copy, 0x3C, iv2, 0, 16);
                }

                AesCtrEncrypt(copy, 0x80, encryptedSectionSize - 0x80, key2, iv2);

                if (Encoding.UTF8.GetString(copy, 0x80, 4) == "#SAV")
                {
                    byte[] decryptedFile = new byte[decryptedSize];
                    Buffer.BlockCopy(copy, 0x80, decryptedFile, 0, decryptedSize);
                    return decryptedFile;
                }
                else
                {
                    throw new InvalidDataException("DecryptSaveFile: Failed at signature on second decryption step.");
                }
            }

            return null;
        }

        public static byte[] EncryptSaveFile(byte[] input)
        {
            if (Encoding.UTF8.GetString(input, 0, 4) != "#SAV")
            {
                throw new InvalidDataException($"EncryptSaveFile: the input buffer does not contain a unencrypted save file.");
            }

            if(SaveFileSizes.All(x => x.Item2 != input.Length))
            {
                throw new InvalidDataException($"EncryptSaveFile: Unrecognized file size.");
            }

            int size = input.Length;
            bool isV1 = input.Length == Offsets.DECRYPTED_SAVE_SIZE_V1;
            size += 0x80 + (!isV1 ? 0x28 : 0x30);

            int decryptedSize = input.Length;
            int encryptedSize = SaveFileSizes.FirstOrDefault(x => x.Item2 == input.Length).Item1;
            byte[] buf = new byte[size];

            //if (input.Length + 0xA0 != encryptedSize)
            //{
                //throw new InvalidDataException($"EncryptSaveFile: Size mismatch between input buffer and expected size.");
            //}

            //Copy unencrypted save to output
            Buffer.BlockCopy(input, 0, buf, 0xA0, decryptedSize);

            //Create encrypted header section and fill it with random bytes
            //These bytes will be used as the secondary encryption key
            byte[] encryptedHeader = new byte[0x80];
            Random.NextBytes(encryptedHeader);
            Buffer.BlockCopy(encryptedHeader, 0, buf, 0x20, 0x80);
            buf[0x25] = 0x34;

            buf[0x3A] = 0; 
            
            for (int i = 0; i < decryptedSize; i += 0x20)
            {
                buf[0x3A] += buf[0xA0 + i];
            }

            //Encrypt the save data with the secondary key
            byte[] key2 = new byte[32];
            byte[] iv2 = new byte[16];
            Buffer.BlockCopy(buf, 0x6C, key2, 0, 32);
            Buffer.BlockCopy(buf, 0x8C, iv2, 0, 16);

            AesCtrEncrypt(buf, 0xA0, decryptedSize, key2, iv2);


            buf[0x35] = 0;

            for (int i = 0; i < 14; i++)
            {
                buf[0x35] += buf[0x26 + i];
            }

            buf[0x36] = 0;
            for (int i = 0; i < 8; i++)
            {
                buf[0x36] += buf[0x3C + i * 4];
            }

            buf[0x37] = 0;
            for (int i = 0; i < 8; i++)
            {
                buf[0x37] += buf[0x6C + i * 4];
            }

            buf[0x38] = 0;
            for (int i = 0; i < 4; i++)
            {
                buf[0x38] += buf[0x5C + i * 4];
            }

            buf[0x39] = 0;
            for (int i = 0; i < 4; i++)
            {
                buf[0x39] += buf[0x8C + i * 4];
            }

            buf[0x3B] = 0;
            for (int i = 0; i < decryptedSize; i += 0x20)
            {
                buf[0x3B] += buf[0xA0 + i];
            }

            buf[0x34] = buf[0x25];
            for (int i = 0; i < 7; i++)
            {
                buf[0x34] += buf[0x35 + i];
            }

            Buffer.BlockCopy(Encoding.UTF8.GetBytes("#SAV"), 0, buf, 0x20, 4);

            buf[0x24] = 0;
            Buffer.BlockCopy(BitConverter.GetBytes(decryptedSize), 0, buf, 0x9C, sizeof(int));

            //Encrypt the "encrypted header" + save file data
            AesCtrEncrypt(buf, 0x20, 0x80, Key, IV);

            //Write header
            Buffer.BlockCopy(BitConverter.GetBytes(ENCRYPTED_SIGNATURE), 0, buf, 0, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(encryptedSize), 0, buf, 4, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(decryptedSize + 0x80), 0, buf, 8, sizeof(int));

            using(MD5 md5 = MD5Cng.Create())
            {
                byte[] copy = new byte[decryptedSize + 0x80];
                Buffer.BlockCopy(buf, 0x20, copy, 0, copy.Length);

                Buffer.BlockCopy(md5.ComputeHash(copy), 0, buf, 0x10, 0x10);
            }

            if (!IsEncrypted(buf))
                throw new InvalidDataException("EncryptSaveFile: encryption method produced a save file with the wrong size.");

            return buf;
        }

        public static bool IsEncrypted(byte[] input)
        {
            return SaveFileSizes.Any(x => x.Item1 == input.Length);
        }

        public static bool IsValidSaveFile(byte[] input)
        {
            if(SaveFileSizes.Any(x => x.Item1 == input.Length)) return true; //Check if file is encrypted
            if(SaveFileSizes.Any(x => x.Item2 == input.Length)) return true; //Check if decrypted

            //If its not either of those, it's not a known save file
            return false;
        }

        private static void AesCtrEncrypt(byte[] buffer, int startOffset, int length, byte[] key, byte[] iv)
        {
            const int BlockSize = 16;

            byte[] ctr = new byte[BlockSize];

            Buffer.BlockCopy(iv, 0, ctr, 0, BlockSize);

            int nblocks = length / BlockSize;

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    for (int i = 0; i < nblocks; i++)
                    {
                        byte[] temp = new byte[BlockSize];

                        Buffer.BlockCopy(ctr, 0, temp, 0, BlockSize);

                        //AesEcb encryption
                        byte[] output = new byte[16];

                        encryptor.TransformBlock(temp, 0, 16, output, 0);
                        Buffer.BlockCopy(output, 0, temp, 0, 16);

                        int xorSize;

                        if (i == (nblocks - 1) && (length & (BlockSize - 1)) != 0)
                        {
                            xorSize = length & (BlockSize - 1);
                        }
                        else
                        {
                            xorSize = BlockSize;
                        }

                        for (int j = 0; j < xorSize; j++)
                        {
                            buffer[startOffset + i * BlockSize + j] ^= temp[j];
                        }

                        if (i != (nblocks - 1))
                        {
                            int carry = 1;

                            for (int k = BlockSize - 1; k >= 0 && carry != 0; k--)
                            {
                                ctr[k]++;
                                carry = (ctr[k] == 0) ? 1 : 0;
                            }
                        }
                    }
                }
            }
        }

    }
}