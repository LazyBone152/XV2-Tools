using System;
using System.Security.Cryptography;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib
{
    public static class Random
    {
        private static int _randomSeed = 0;
        public static int RandomSeed
        {
            get => _randomSeed;
            set
            {
                if(_randomSeed != value)
                {
                    _randomSeed = value;
                    RandomGenerator = new System.Random(_randomSeed);
                }
            }
        }

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
            if (MathHelpers.FloatEquals(min, max)) return max;
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

            //RandomGenerator.Next returns an int equal or greater than min and less than max, but we need a random value that can also include max, so we increment it here
            if (max != int.MaxValue) //This ensures there is no overflow back to 0
                max++;

            return RandomGenerator.Next(min, max);
        }

        public static int RangeNoRepeat(int min, int max, int lastResult)
        {
            int tries = 0;
            int result = Range(min, max);

            while (result == lastResult)
            {
                result = Range(min, max);
                tries++;

                if (tries >= 300)
                    break;
            }

            return result;
        }

        public static bool RandomBool()
        {
            return Range(0, 1) == 0;
        }

        public static double Next()
        {
            InitRandomGenerator();
            return RandomGenerator.NextDouble();
        }

        private static void InitRandomGenerator()
        {
            if (RandomGenerator == null) GenerateNewSeed();
        }

        public static void GenerateNewSeed()
        {
            RandomSeed = _getRandomInt(352, 242142142);
        }

        public static void ResetWithCurrentSeed()
        {
            RandomGenerator = new System.Random(_randomSeed);
        }
    }

}
