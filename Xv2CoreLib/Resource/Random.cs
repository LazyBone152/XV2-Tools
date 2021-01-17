using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib
{
    public static class Random
    {
        private static System.Random RandomGenerator;
        private static readonly RNGCryptoServiceProvider _generator = new RNGCryptoServiceProvider();

        private static int _getRandomInt(int minimumValue, int maximumValue)
        {
            byte[] randomNumber = new byte[1];

            _generator.GetBytes(randomNumber);

            double asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

            // We are using Math.Max, and substracting 0.00000000001, 
            // to ensure "multiplier" will always be between 0.0 and .99999999999
            // Otherwise, it's possible for it to be "1", which causes problems in our rounding.
            double multiplier = Math.Max(0, (asciiValueOfRandomCharacter / 255d) - 0.00000000001d);

            // We need to add one to the range, to allow for the rounding done with Math.Floor
            int range = maximumValue - minimumValue + 1;

            double randomValueInRange = Math.Floor(multiplier * range);

            return (int)(minimumValue + randomValueInRange);
        }

        public static float Range(float min, float max)
        {
            InitRandomGenerator();

            return (float)RandomGenerator.NextDouble() * (max - min) + min;
        }

        public static double Range(double min, double max)
        {
            InitRandomGenerator();

            return RandomGenerator.NextDouble() * (max - min) + min;
        }

        public static int Range(int min, int max)
        {
            InitRandomGenerator();

            return RandomGenerator.Next(min, max);
        }

        private static void InitRandomGenerator()
        {
            if (RandomGenerator == null) RandomGenerator = new System.Random(_getRandomInt(352, 242142142));
        }
    }

}
