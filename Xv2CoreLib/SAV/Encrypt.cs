using System;
using System.Runtime.InteropServices;

namespace Xv2CoreLib.SAV
{
    /// <summary>
    /// Manages the encryption and decrypting of the save file, interacting with AesCtrLibrary.dll.
    /// </summary>
    public static class Crypt
    {

        /// <summary>
        /// Encrypts verion 21 of the save file.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] EncryptManaged_V21(byte[] bytes)
        {
            UIntPtr arraySize = (new UIntPtr((uint)Offsets.DECRYPTED_SAVE_SIZE_V21));
            IntPtr retPtr = SaveEncrypt(bytes, arraySize);
            byte[] newBytes = new byte[Offsets.ENCRYPTED_SAVE_SIZE_V21];
            Marshal.Copy(retPtr, newBytes, 0, Offsets.ENCRYPTED_SAVE_SIZE_V21);
            return newBytes;
        }

        /// <summary>
        /// Decrypts verion 21 of the save file.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] DecryptManaged_V21(byte[] bytes)
        {
            UIntPtr arraySize = (new UIntPtr((uint)Offsets.ENCRYPTED_SAVE_SIZE_V21));
            IntPtr retPtr = SaveDecrypt(bytes, arraySize);
            byte[] newBytes = new byte[Offsets.DECRYPTED_SAVE_SIZE_V21];
            Marshal.Copy(retPtr, newBytes, 0, Offsets.DECRYPTED_SAVE_SIZE_V21);
            return newBytes;
        }

        /// <summary>
        /// Decrypts verion 10 of the save file.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] DecryptManaged_V10(byte[] bytes)
        {
            UIntPtr arraySize = (new UIntPtr((uint)Offsets.ENECRYPTED_SAVE_SIZE_V10));
            IntPtr retPtr = SaveDecrypt(bytes, arraySize);
            byte[] newBytes = new byte[Offsets.DECRYPTED_SAVE_SIZE_V10];
            Marshal.Copy(retPtr, newBytes, 0, Offsets.DECRYPTED_SAVE_SIZE_V10);
            return newBytes;
        }


        /// <summary>
        /// Decrypts verion 1 of the save file.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] DecryptManaged_V1(byte[] bytes)
        {
            UIntPtr arraySize = (new UIntPtr((uint)Offsets.ENECRYPTED_SAVE_SIZE_V1));
            IntPtr retPtr = SaveDecrypt(bytes, arraySize);
            byte[] newBytes = new byte[Offsets.DECRYPTED_SAVE_SIZE_V1];
            Marshal.Copy(retPtr, newBytes, 0, Offsets.DECRYPTED_SAVE_SIZE_V1);
            return newBytes;
        }

        /// <summary>
        /// Encrypts verion 1 of the save file.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] EncryptManaged_V10(byte[] bytes)
        {
            UIntPtr arraySize = (new UIntPtr((uint)Offsets.DECRYPTED_SAVE_SIZE_V10));
            IntPtr retPtr = SaveEncrypt(bytes, arraySize);
            byte[] newBytes = new byte[Offsets.ENECRYPTED_SAVE_SIZE_V10];
            Marshal.Copy(retPtr, newBytes, 0, Offsets.ENECRYPTED_SAVE_SIZE_V10);
            return newBytes;
        }

        /// <summary>
        /// Encrypts verion 1 of the save file.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] EncryptManaged_V1(byte[] bytes)
        {
            UIntPtr arraySize = (new UIntPtr((uint)Offsets.DECRYPTED_SAVE_SIZE_V1));
            IntPtr retPtr = SaveEncrypt(bytes, arraySize);
            byte[] newBytes = new byte[Offsets.ENECRYPTED_SAVE_SIZE_V1];
            Marshal.Copy(retPtr, newBytes, 0, Offsets.ENECRYPTED_SAVE_SIZE_V1);
            return newBytes;
        }

        [DllImport("AesCtrLibrary.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SaveDecrypt(byte[] file, UIntPtr arraySize);

        [DllImport("AesCtrLibrary.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SaveEncrypt(byte[] file, UIntPtr arraySize);

    }
}
