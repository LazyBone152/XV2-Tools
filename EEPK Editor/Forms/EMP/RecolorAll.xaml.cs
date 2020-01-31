using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xv2CoreLib.EMP;
using Xv2CoreLib;

namespace EEPK_Organiser.Forms.EMP
{
    /// <summary>
    /// Interaction logic for RecolorAll.xaml
    /// </summary>
    public partial class RecolorAll : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        
        private EMP_File _empFile = null;
        private ObservableCollection<EmpParticleEffect> _particleEffectsView = null;
        private Colors _colors = new Colors();

        public EMP_File empFile
        {
            get
            {
                return this._empFile;
            }

            set
            {
                if (value != this._empFile)
                {
                    this._empFile = value;
                    NotifyPropertyChanged("empFile");
                }
            }
        }

        public ObservableCollection<EmpParticleEffect> ParticleEffectsView
        {
            get
            {
                return this._particleEffectsView;
            }

            set
            {
                if (value != this._particleEffectsView)
                {
                    this._particleEffectsView = value;
                    NotifyPropertyChanged("ParticleEffectsView");
                }
            }
        }
        public Colors SelectedColors
        {
            get
            {
                return this._colors;
            }

            set
            {
                if (value != this._colors)
                {
                    this._colors = value;
                    NotifyPropertyChanged("SelectedColors");
                }
            }
        }
        public bool IgnoreAlpha { get; set; } = true;
        
        public RecolorAll(EMP_File _inputFile)
        {
            empFile = _inputFile;
            ParticleEffectsView = EmpParticleEffect.Create(empFile);
            InitializeComponent();
            DataContext = this;
            GetAvgColor_Click(null, null);
        }

        private byte[] GetAverageColor()
        {
            //Gets average color for Color1 & Color2
            //0-3 = color1
            //4-7 = color2
            //8 = count
            int[] totals = new int[9];

            //Get the totals
            foreach (var particle in ParticleEffectsView)
            {
                if (particle.Selected)
                {
                    //Check for children, and recursively get colors from them
                    if (particle.ParticleEffect.ChildParticleEffects != null)
                    {
                        totals = GetTotalColor_Recursive(totals, particle.ParticleEffect);
                    }

                    //Get colors from this texture part
                    if (particle.ParticleEffect.IsTextureType())
                    {
                        totals[0] += Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_48);
                        totals[1] += Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_52);
                        totals[2] += Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_56);
                        totals[3] += Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_60);
                        totals[4] += Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_80);
                        totals[5] += Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_84);
                        totals[6] += Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_88);
                        totals[7] += Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_92);
                        totals[8]++;
                    }
                }
                
            }

            //Convert the totals into the averages
            if(totals[8] == 0)
            {
                MessageBox.Show(this, "No color information was found. Setting defaults.", "Get Average Color", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new byte[8] { 255, 255, 255, 255, 255, 255, 255, 255 };
            }
            byte[] avgs = new byte[8];
            avgs[0] = (byte)(totals[0] / totals[8]);
            avgs[1] = (byte)(totals[1] / totals[8]);
            avgs[2] = (byte)(totals[2] / totals[8]);
            avgs[3] = (byte)(totals[3] / totals[8]);
            avgs[4] = (byte)(totals[4] / totals[8]);
            avgs[5] = (byte)(totals[5] / totals[8]);
            avgs[6] = (byte)(totals[6] / totals[8]);
            avgs[7] = (byte)(totals[7] / totals[8]);

            return avgs;
        }

        private int[] GetTotalColor_Recursive(int[] totals, ParticleEffect particleEffect)
        {

            foreach (var particle in particleEffect.ChildParticleEffects)
            {
                //Check for children, and recursively get colors from them
                if (particle.ChildParticleEffects != null)
                {
                    totals = GetTotalColor_Recursive(totals, particle);
                }

                //Get colors from this texture part
                if (particle.IsTextureType())
                {
                    totals[0] += Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_48);
                    totals[1] += Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_52);
                    totals[2] += Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_56);
                    totals[3] += Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_60);
                    totals[4] += Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_80);
                    totals[5] += Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_84);
                    totals[6] += Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_88);
                    totals[7] += Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_92);
                    totals[8]++;
                }
            }

            return totals;
        }

        private void GetAvgColor_Click(object sender, RoutedEventArgs e)
        {
            byte[] colors = GetAverageColor();
            SelectedColors.SetColor1(colors[0], colors[1], colors[2], colors[3]);
            SelectedColors.SetColor2(colors[4], colors[5], colors[6], colors[7]);
        }

        private void SetAvgColor_Click(object sender, RoutedEventArgs e)
        {
            byte[] shift = new byte[8];

            shift[0] = (byte)(SelectedColors.R_Avg_1 - SelectedColors.R_1);
            shift[1] = (byte)(SelectedColors.G_Avg_1 - SelectedColors.G_1);
            shift[2] = (byte)(SelectedColors.B_Avg_1 - SelectedColors.B_1);
            shift[3] = (byte)(SelectedColors.A_Avg_1 - SelectedColors.A_1);
            shift[4] = (byte)(SelectedColors.R_Avg_2 - SelectedColors.R_2);
            shift[5] = (byte)(SelectedColors.G_Avg_2 - SelectedColors.G_2);
            shift[6] = (byte)(SelectedColors.B_Avg_2 - SelectedColors.B_2);
            shift[7] = (byte)(SelectedColors.A_Avg_2 - SelectedColors.A_2);

            EmpParticleEffect.ShiftColors(ParticleEffectsView, shift, IgnoreAlpha);

            MessageBox.Show(this, "Average colors successfully set!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void RemoveAnimations_Click(object sender, RoutedEventArgs e)
        {
            EmpParticleEffect.RemoveAnimations(ParticleEffectsView, IgnoreAlpha);
            MessageBox.Show(this, "Animations removed.", "Remove Animations", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveRandom_Click(object sender, RoutedEventArgs e)
        {
            EmpParticleEffect.RemoveRandomRange(ParticleEffectsView, IgnoreAlpha);
            MessageBox.Show(this, "Random color range removed.", "Remove Random", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class Colors : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public Color? Color1
        {
            get
            {
                return new Color()
                {
                    R = R_1,
                    G = G_1,
                    B = B_1,
                    A = A_1
                };
            }
            set
            {
                R_1 = value.Value.R;
                G_1 = value.Value.G;
                B_1 = value.Value.B;
                A_1 = value.Value.A;
            }
        }
        public Color? Color2
        {
            get
            {
                return new Color()
                {
                    R = R_2,
                    G = G_2,
                    B = B_2,
                    A = A_2
                };
            }
            set
            {
                R_2 = value.Value.R;
                G_2 = value.Value.G;
                B_2 = value.Value.B;
                A_2 = value.Value.A;
            }
        }


        private byte _r_1 = 0;
        private byte _g_1 = 0;
        private byte _b_1 = 0;
        private byte _a_1 = 0;
        private byte _r_2 = 0;
        private byte _g_2 = 0;
        private byte _b_2 = 0;
        private byte _a_2 = 0;


        public byte R_Avg_1 { get; set; }
        public byte G_Avg_1 { get; set; }
        public byte B_Avg_1 { get; set; }
        public byte A_Avg_1 { get; set; }
        public byte R_Avg_2 { get; set; }
        public byte G_Avg_2 { get; set; }
        public byte B_Avg_2 { get; set; }
        public byte A_Avg_2 { get; set; }

        public byte R_1
        {
            get
            {
                return this._r_1;
            }

            set
            {
                if (value != this._r_1)
                {
                    this._r_1 = value;
                    NotifyPropertyChanged("R_1");
                    NotifyPropertyChanged("Color1");
                }
            }
        }
        public byte G_1
        {
            get
            {
                return this._g_1;
            }

            set
            {
                if (value != this._g_1)
                {
                    this._g_1 = value;
                    NotifyPropertyChanged("G_1");
                    NotifyPropertyChanged("Color1");
                }
            }
        }
        public byte B_1
        {
            get
            {
                return this._b_1;
            }

            set
            {
                if (value != this._b_1)
                {
                    this._b_1 = value;
                    NotifyPropertyChanged("B_1");
                    NotifyPropertyChanged("Color1");
                }
            }
        }
        public byte A_1
        {
            get
            {
                return this._a_1;
            }

            set
            {
                if (value != this._a_1)
                {
                    this._a_1 = value;
                    NotifyPropertyChanged("A_1");
                    NotifyPropertyChanged("Color1");
                }
            }
        }

        public byte R_2
        {
            get
            {
                return this._r_2;
            }

            set
            {
                if (value != this._r_2)
                {
                    this._r_2 = value;
                    NotifyPropertyChanged("R_2");
                    NotifyPropertyChanged("Color2");
                }
            }
        }
        public byte G_2
        {
            get
            {
                return this._g_2;
            }

            set
            {
                if (value != this._g_2)
                {
                    this._g_2 = value;
                    NotifyPropertyChanged("G_2");
                    NotifyPropertyChanged("Color2");
                }
            }
        }
        public byte B_2
        {
            get
            {
                return this._b_2;
            }

            set
            {
                if (value != this._b_2)
                {
                    this._b_2 = value;
                    NotifyPropertyChanged("B_2");
                    NotifyPropertyChanged("Color2");
                }
            }
        }
        public byte A_2
        {
            get
            {
                return this._a_2;
            }

            set
            {
                if (value != this._a_2)
                {
                    this._a_2 = value;
                    NotifyPropertyChanged("A_2");
                    NotifyPropertyChanged("Color2");
                }
            }
        }


        public void SetColor1(byte _R, byte _G, byte _B, byte _A)
        {
            R_1 = _R;
            R_Avg_1 = _R;
            G_1 = _G;
            G_Avg_1 = _G;
            B_1 = _B;
            B_Avg_1 = _B;
            A_1 = _A;
            A_Avg_1 = _A;
        }

        public void SetColor2(byte _R, byte _G, byte _B, byte _A)
        {
            R_2 = _R;
            R_Avg_2 = _R;
            G_2 = _G;
            G_Avg_2 = _G;
            B_2 = _B;
            B_Avg_2 = _B;
            A_2 = _A;
            A_Avg_2 = _A;
        }


    }

    public class EmpParticleEffect : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool _selected = true;
        public bool Selected
        {
            get
            {
                return this._selected;
            }

            set
            {
                if (value != this._selected)
                {
                    this._selected = value;
                    NotifyPropertyChanged("Selected");
                }
            }
        }

        public string Name
        {
            get
            {
                return ParticleEffect.Name;
            }
        }
        public ParticleEffect ParticleEffect { get; set; }

        public static ObservableCollection<EmpParticleEffect> Create(EMP_File empFile)
        {
            ObservableCollection<EmpParticleEffect> newParticleEffects = new ObservableCollection<EmpParticleEffect>();
            
            foreach (var particleEffect in empFile.ParticleEffects)
            {
                newParticleEffects.Add(new EmpParticleEffect() { ParticleEffect = particleEffect, Selected = true });
            }

            return newParticleEffects;
        }

        public static void ShiftColors(ObservableCollection<EmpParticleEffect> particles, byte[] colorShift, bool ignoreAlpha)
        {

            foreach(var particle in particles)
            {
                if (particle.Selected)
                {
                    //Children check
                    if (particle.ParticleEffect.ChildParticleEffects != null)
                    {
                        ShiftColors_Recursive(particle.ParticleEffect.ChildParticleEffects, colorShift, ignoreAlpha);
                    }

                    //Shift colors
                    if (particle.ParticleEffect.IsTextureType())
                    {
                        //Convert float colors to byte
                        byte R_1 = Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_48);
                        byte G_1 = Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_52);
                        byte B_1 = Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_56);
                        byte A_1 = Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_60);
                        byte R_2 = Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_80);
                        byte G_2 = Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_84);
                        byte B_2 = Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_88);
                        byte A_2 = Xv2ColorConverter.ConvertColor(particle.ParticleEffect.Type_Texture.F_92);

                        //Shift the byte colors
                        R_1 -= colorShift[0];
                        G_1 -= colorShift[1];
                        B_1 -= colorShift[2];
                        if (!ignoreAlpha)
                        {
                            A_1 -= colorShift[3];
                        }
                        R_2 -= colorShift[4];
                        G_2 -= colorShift[5];
                        B_2 -= colorShift[6];
                        if (!ignoreAlpha)
                        {
                            A_2 -= colorShift[7];
                        }

                        //Convert byte colors back to floats
                        particle.ParticleEffect.Type_Texture.F_48 = Xv2ColorConverter.ConvertColor(R_1);
                        particle.ParticleEffect.Type_Texture.F_52 = Xv2ColorConverter.ConvertColor(G_1);
                        particle.ParticleEffect.Type_Texture.F_56 = Xv2ColorConverter.ConvertColor(B_1);
                        particle.ParticleEffect.Type_Texture.F_60 = Xv2ColorConverter.ConvertColor(A_1);
                        particle.ParticleEffect.Type_Texture.F_80 = Xv2ColorConverter.ConvertColor(R_2);
                        particle.ParticleEffect.Type_Texture.F_84 = Xv2ColorConverter.ConvertColor(G_2);
                        particle.ParticleEffect.Type_Texture.F_88 = Xv2ColorConverter.ConvertColor(B_2);
                        particle.ParticleEffect.Type_Texture.F_92 = Xv2ColorConverter.ConvertColor(A_2);

                    }
                }
                
            }
        }

        private static void ShiftColors_Recursive(ObservableCollection<ParticleEffect> particles, byte[] colorShift, bool ignoreAlpha)
        {

            foreach (var particle in particles)
            {
                //Children check
                if (particle.ChildParticleEffects != null)
                {
                    ShiftColors_Recursive(particle.ChildParticleEffects, colorShift, ignoreAlpha);
                }

                //Shift colors
                if (particle.IsTextureType())
                {
                    //Convert float colors to byte
                    byte R_1 = Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_48);
                    byte G_1 = Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_52);
                    byte B_1 = Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_56);
                    byte A_1 = Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_60);
                    byte R_2 = Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_80);
                    byte G_2 = Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_84);
                    byte B_2 = Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_88);
                    byte A_2 = Xv2ColorConverter.ConvertColor(particle.Type_Texture.F_92);

                    //Shift the byte colors
                    R_1 -= colorShift[0];
                    G_1 -= colorShift[1];
                    B_1 -= colorShift[2];
                    if (!ignoreAlpha)
                    {
                        A_1 -= colorShift[3];
                    }
                    R_2 -= colorShift[4];
                    G_2 -= colorShift[5];
                    B_2 -= colorShift[6];
                    if (!ignoreAlpha)
                    {
                        A_2 -= colorShift[7];
                    }

                    //Convert byte colors back to floats
                    particle.Type_Texture.F_48 = Xv2ColorConverter.ConvertColor(R_1);
                    particle.Type_Texture.F_52 = Xv2ColorConverter.ConvertColor(G_1);
                    particle.Type_Texture.F_56 = Xv2ColorConverter.ConvertColor(B_1);
                    particle.Type_Texture.F_60 = Xv2ColorConverter.ConvertColor(A_1);
                    particle.Type_Texture.F_80 = Xv2ColorConverter.ConvertColor(R_2);
                    particle.Type_Texture.F_84 = Xv2ColorConverter.ConvertColor(G_2);
                    particle.Type_Texture.F_88 = Xv2ColorConverter.ConvertColor(B_2);
                    particle.Type_Texture.F_92 = Xv2ColorConverter.ConvertColor(A_2);

                }
            }
        }

        public static void RemoveRandomRange(ObservableCollection<EmpParticleEffect> particles, bool ignoreAlpha)
        {
            foreach(var particle in particles)
            {
                if (particle.Selected)
                {
                    if (particle.ParticleEffect.ChildParticleEffects != null)
                    {
                        RemoveRandomRange_Recursive(particle.ParticleEffect.ChildParticleEffects, ignoreAlpha);
                    }

                    if (particle.ParticleEffect.IsTextureType())
                    {
                        particle.ParticleEffect.Type_Texture.F_64 = 0;
                        particle.ParticleEffect.Type_Texture.F_68 = 0;
                        particle.ParticleEffect.Type_Texture.F_72 = 0;
                        if (!ignoreAlpha)
                        {
                            particle.ParticleEffect.Type_Texture.F_76 = 0;
                        }
                    }
                }
                
            }
        }

        private static void RemoveRandomRange_Recursive(ObservableCollection<ParticleEffect> particles, bool ignoreAlpha)
        {
            foreach (var particle in particles)
            {
                if (particle.ChildParticleEffects != null)
                {
                    RemoveRandomRange_Recursive(particle.ChildParticleEffects, ignoreAlpha);
                }

                if (particle.IsTextureType())
                {
                    particle.Type_Texture.F_64 = 0;
                    particle.Type_Texture.F_68 = 0;
                    particle.Type_Texture.F_72 = 0;
                    if (!ignoreAlpha)
                    {
                        particle.Type_Texture.F_76 = 0;
                    }
                }
            }
        }

        public static void RemoveAnimations(ObservableCollection<EmpParticleEffect> particles, bool ignoreAlpha)
        {
            foreach (var particle in particles)
            {
                if (particle.Selected)
                {
                    if (particle.ParticleEffect.ChildParticleEffects != null)
                    {
                        RemoveAnimations_Recursive(particle.ParticleEffect.ChildParticleEffects, ignoreAlpha);
                    }

                    if (particle.ParticleEffect.Type_0 != null)
                    {
                        List<Type0> type0s = new List<Type0>();

                        foreach (var anim in particle.ParticleEffect.Type_0)
                        {
                            if (anim.SelectedParameter == Type0.Parameter.Color1 || anim.SelectedParameter == Type0.Parameter.Color2)
                            {
                                if (anim.SelectedComponentColor1 == Type0.ComponentColor1.A || anim.SelectedComponentColor2 == Type0.ComponentColor2.A)
                                {
                                    if (!ignoreAlpha)
                                    {
                                        type0s.Add(anim);
                                    }
                                }
                                else
                                {
                                    type0s.Add(anim);
                                }
                            }
                        }

                        foreach (var type0 in type0s)
                        {
                            particle.ParticleEffect.Type_0.Remove(type0);
                        }

                    }
                }
                
            }
        }

        private static void RemoveAnimations_Recursive(ObservableCollection<ParticleEffect> particles, bool ignoreAlpha)
        {
            foreach (var particle in particles)
            {
                if (particle.ChildParticleEffects != null)
                {
                    RemoveAnimations_Recursive(particle.ChildParticleEffects, ignoreAlpha);
                }

                if (particle.Type_0 != null)
                {
                    List<Type0> type0s = new List<Type0>();

                    foreach (var anim in particle.Type_0)
                    {
                        if (anim.SelectedParameter == Type0.Parameter.Color1 || anim.SelectedParameter == Type0.Parameter.Color2)
                        {
                            if (anim.SelectedComponentColor1 == Type0.ComponentColor1.A || anim.SelectedComponentColor2 == Type0.ComponentColor2.A)
                            {
                                if (!ignoreAlpha)
                                {
                                    type0s.Add(anim);
                                }
                            }
                            else
                            {
                                type0s.Add(anim);
                            }
                        }
                    }

                    foreach (var type0 in type0s)
                    {
                        particle.Type_0.Remove(type0);
                    }

                }
            }
        }


    }

}
