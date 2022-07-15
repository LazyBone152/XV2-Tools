using System.Linq;
using System.Reflection;

namespace Xv2CoreLib.EMM
{
    public static class DefaultMaterialValues
    {
        private readonly static FieldInfo[] Fields;

        static DefaultMaterialValues()
        {
            Fields = typeof(DefaultMaterialValues).GetFields();
        }

        //All default values for materials will be declared here. 
        //The format must be as follows:
        //The value types must be the same for Int, float, and bools
        //For Vectors, Colors and MatUVs they must be float[4].

        public readonly static int AlphaBlendType = -1;
        public readonly static float[] MatOffset0 = new float[4] { 0, 0, 0, 0 };
        public readonly static float[] MatOffset1 = MatOffset0;
        public readonly static float[] MatScale0 = new float[4] { 1, 1, 1, 1 };
        public readonly static float[] MatScale1 = MatScale0;
        public readonly static float[] MatCol0 = new float[4] { 0, 0, 0, 1 };
        public readonly static float[] MatCol1 = MatCol0;
        public readonly static float[] MatCol2 = new float[4] { 1, 1, 1, 1 };
        public readonly static float[] MatCol3 = MatCol0;



        public static float[] GetDefautVector(string fieldName)
        {
            var field = Fields.FirstOrDefault(x => x.Name == fieldName);

            if(field != null)
            {
                return (float[])field.GetValue(null);
            }

            return null;
        }

        public static float GetDefautFloat(string fieldName)
        {
            var field = Fields.FirstOrDefault(x => x.Name == fieldName);

            if (field != null)
            {
                return (float)field.GetValue(null);
            }

            return 0f;
        }

        public static int GetDefautInt(string fieldName)
        {
            var field = Fields.FirstOrDefault(x => x.Name == fieldName);

            if (field != null)
            {
                return (int)field.GetValue(null);
            }

            return 0;
        }

        public static bool GetDefautBool(string fieldName)
        {
            var field = Fields.FirstOrDefault(x => x.Name == fieldName);

            if (field != null)
            {
                return (bool)field.GetValue(null);
            }

            return false;
        }
    }
}
