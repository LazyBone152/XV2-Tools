using System;
using System.ComponentModel;

namespace LB_Common.Numbers
{
    [Serializable]
    public class CustomVector4 : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public float[] Values = new float[4];

        public float X { get { return GetValue(0); } set { SetValue(0, value); } }
        public float Y { get { return GetValue(1); } set { SetValue(1, value); } }
        public float Z { get { return GetValue(02); } set { SetValue(2, value); } }
        public float W { get { return GetValue(3); } set { SetValue(3, value); } }

        public CustomVector4() { }

        public CustomVector4(float value)
        {
            X = Y = Z = W = value;
        }

        public CustomVector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public void SetValue(int idx, float value)
        {
            //Initialization of internal array is required here when using XML serialization
            if (Values == null)
                Values = new float[4];

            Values[idx] = value;

            switch (idx)
            {
                case 0:
                    NotifyPropertyChanged(nameof(X));
                    break;
                case 1:
                    NotifyPropertyChanged(nameof(Y));
                    break;
                case 2:
                    NotifyPropertyChanged(nameof(Z));
                    break;
                case 3:
                    NotifyPropertyChanged(nameof(W));
                    break;
            }
        }

        public float GetValue(int idx)
        {
            //Initialization of internal array is required here when using XML serialization
            if (Values == null)
                Values = new float[4];

            return Values[idx];
        }

        #region Operators
        public override bool Equals(object obj)
        {
            return this == (CustomVector4)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash1 = 2256559;
                const int hash2 = 5023567;
                hash1 *= hash2 ^ X.GetHashCode();
                hash1 *= hash2 ^ Y.GetHashCode();
                hash1 *= hash2 ^ Z.GetHashCode();
                hash1 *= hash2 ^ W.GetHashCode();
                return hash1;
            }
        }

        public override string ToString()
        {
            return string.Format("[X: {0}, Y: {1}, Z: {2}, W: {3}]", X, Y, Z, W);
        }

        public static bool operator ==(CustomVector4 a, CustomVector4 b)
        {
            return a?.X == b?.X && a?.Y == b?.Y && a?.Z == b?.Z && a?.W == b?.W;
        }

        public static bool operator !=(CustomVector4 a, CustomVector4 b)
        {
            return (a == b) ? false : true;
        }

        public static CustomVector4 operator *(CustomVector4 a, float b)
        {
            return new CustomVector4(a.X * b, a.Y * b, a.Z * b, a.W * b);
        }
        
        public static CustomVector4 operator -(CustomVector4 a, CustomVector4 b)
        {
            return new CustomVector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        }

        public static CustomVector4 operator +(CustomVector4 a, CustomVector4 b)
        {
            return new CustomVector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }
        #endregion


    }
}
