using System;
using System.ComponentModel;

namespace LB_Common.Numbers
{
    [Serializable]
    public class CustomMatRepUV : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public bool[] Values = new bool[4];

        public bool U { get { return GetValue(0); } set { SetValue(0, value); } }
        public bool V { get { return GetValue(1); } set { SetValue(1, value); } }

        public CustomMatRepUV() { }

        public CustomMatRepUV(bool u, bool v)
        {
            U = u;
            V = v;
        }

        public void SetValue(int idx, bool value)
        {
            //Initialization of internal array is required here when using XML serialization
            if (Values == null)
                Values = new bool[4];

            Values[idx] = value;

            switch (idx)
            {
                case 0:
                    NotifyPropertyChanged(nameof(U));
                    break;
                case 1:
                    NotifyPropertyChanged(nameof(V));
                    break;
            }
        }

        public bool GetValue(int idx)
        {
            //Initialization of internal array is required here when using XML serialization
            if (Values == null)
                Values = new bool[4];

            return Values[idx];
        }

        #region Operators
        public override bool Equals(object obj)
        {
            return this == (CustomMatRepUV)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash1 = 2256559;
                const int hash2 = 5023567;
                hash1 *= hash2 ^ U.GetHashCode();
                hash1 *= hash2 ^ V.GetHashCode();
                return hash1;
            }
        }

        public static bool operator ==(CustomMatRepUV a, CustomMatRepUV b)
        {
            return a?.U == b?.U && a?.V == b?.V;
        }

        public static bool operator !=(CustomMatRepUV a, CustomMatRepUV b)
        {
            return (a == b) ? false : true;
        }

        #endregion

    }
}
