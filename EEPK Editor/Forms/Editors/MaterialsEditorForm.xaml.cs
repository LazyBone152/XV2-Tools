using MahApps.Metro.Controls;
using System.ComponentModel;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMM;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for MaterialsEditorForm.xaml
    /// </summary>
    public partial class MaterialsEditorForm : MetroWindow, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public EMM_File EmmFile { get; set; }
        public AssetContainerTool AssetContainer { get; set; }
        public AssetType AssetType { get; set; }


        public MaterialsEditorForm(EMM_File _emmFile, AssetContainerTool _container, AssetType assetType, string windowTitle)
        {
            EmmFile = _emmFile;
            AssetContainer = _container;
            AssetType = assetType;

            InitializeComponent();
            Closing += MaterialsEditorForm_Closing;

            if (windowTitle != null)
            {
                Title += string.Format(" ({0})", windowTitle);
            }
        }

        public MaterialsEditorForm(EMM_File _emmFile, string windowTitle)
        {
            EmmFile = _emmFile;

            InitializeComponent();
            Closing += MaterialsEditorForm_Closing;

            if (windowTitle != null)
            {
                Title += string.Format(" ({0})", windowTitle);
            }
        }

        private void MaterialsEditorForm_Closing(object sender, CancelEventArgs e)
        {
            Closing -= MaterialsEditorForm_Closing;
            materialsEditor.Dispose();
        }
    
        public void SelectMaterial(EmmMaterial material)
        {
            materialsEditor.SelectedMaterial = material;
            materialsEditor.materialDataGrid.ScrollIntoView(material);
        }
    }
}
