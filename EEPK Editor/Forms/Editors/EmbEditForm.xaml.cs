using EEPK_Organiser.View;
using MahApps.Metro.Controls;
using System.ComponentModel;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMB_CLASS;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for MaterialsEditorForm.xaml
    /// </summary>
    public partial class EmbEditForm : MetroWindow
    {

        public EMB_File EmbFile { get;  set; }
        public AssetContainerTool AssetContainer { get;  set; }
        public AssetType AssetType { get;  set; }
        public TextureEditorType EditorType { get;  set; }

        public EmbEditForm(EMB_File _embFile, AssetContainerTool _container, AssetType assetType, string windowTitle)
        {
            DataContext = this;
            EmbFile = _embFile;
            AssetContainer = _container;
            AssetType = assetType;

            if (AssetType == AssetType.PBIND)
                EditorType = TextureEditorType.Pbind;
            else if (AssetType == AssetType.TBIND)
                EditorType = TextureEditorType.Tbind;
            else
                EditorType = TextureEditorType.Emo;

            InitializeComponent();
            Closing += MaterialsEditorForm_Closing;

            if (windowTitle != null)
            {
                Title += string.Format(" ({0})", windowTitle);
            }
        }

        public EmbEditForm(EMB_File _embFile, TextureEditorType editorType, string windowTitle)
        {
            DataContext = this;
            EmbFile = _embFile;
            EditorType = editorType;

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
            textureEditor.Dispose();
        }
    
        public void SelectTexture(EmbEntry embEntry)
        {
            textureEditor.SelectedTexture = embEntry;
            textureEditor.textureDataGrid.ScrollIntoView(embEntry);
        }
    }
}
