using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.EMP_NEW;

namespace EEPK_Organiser.Forms.Editors
{
    /// <summary>
    /// Interaction logic for EmpEditorWindow.xaml
    /// </summary>
    public partial class EmpEditorWindow : MetroWindow
    {
        public EMP_File EmpFile { get; set; }

        public EmpEditorWindow(EMP_File empFile, string empName)
        {
            EmpFile = empFile;
            InitializeComponent();

            Title += $" ({empName})";
        }
    }
}
