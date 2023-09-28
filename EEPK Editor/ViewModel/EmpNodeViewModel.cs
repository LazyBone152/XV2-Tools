using Xv2CoreLib;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.Resource.UndoRedo;
using LB_Common.Numbers;
using GalaSoft.MvvmLight;
using Xv2CoreLib.EMM;

namespace EEPK_Organiser.ViewModel
{
    public class EmpNodeViewModel : ObservableObject
    {
        private EMP_File empFile;
        private ParticleNode node;

        public string Name
        {
            get => node.Name;
            set
            {
                if(node.Name != value && value?.Length <= 32)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.Name), node, node.Name, value, "Node Name"));
                    node.Name = value;
                }

                RaisePropertyChanged(nameof(Name));
            }
        }
        public ParticleNodeType NodeType
        {
            get => node.NodeType;
            set
            {
                if (node.NodeType != value)
                {
                    UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                    {
                        new UndoablePropertyGeneric(nameof(node.NodeType), node, node.NodeType, value),
                        new UndoablePropertyGeneric(nameof(empFile.HasBeenEdited), empFile, true, true, true)
                    }, "Node Type");

                    node.NodeType = value;
                    RaisePropertyChanged(nameof(NodeType));
                    empFile.HasBeenEdited = true;
                }
            }
        }
        public byte StartTime
        {
            get => node.StartTime;
            set
            {
                if (node.StartTime != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.StartTime), node, node.StartTime, value, "Node StartTime"));
                    node.StartTime = value;
                    RaisePropertyChanged(nameof(StartTime));
                }
            }
        }
        public byte StartTime_Variance
        {
            get => node.StartTime_Variance;
            set
            {
                if (node.StartTime_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.StartTime_Variance), node, node.StartTime_Variance, value, "Node StartTimeVariance"));
                    node.StartTime_Variance = value;
                    RaisePropertyChanged(nameof(StartTime_Variance));
                }
            }
        }
        public ushort Lifetime
        {
            get => node.Lifetime;
            set
            {
                if (node.Lifetime != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.Lifetime), node, node.Lifetime, value, "Node Lifetime"));
                    node.Lifetime = value;
                    RaisePropertyChanged(nameof(Lifetime));
                }
            }
        }
        public ushort Lifetime_Variance
        {
            get => node.Lifetime_Variance;
            set
            {
                if (node.Lifetime_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.Lifetime_Variance), node, node.Lifetime_Variance, value, "Node Lifetime_Variance"));
                    node.Lifetime_Variance = value;
                    RaisePropertyChanged(nameof(Lifetime_Variance));
                }
            }
        }
        public short MaxInstances //-1 to 1000
        {
            get => node.MaxInstances;
            set
            {
                if (node.MaxInstances != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.MaxInstances), node, node.MaxInstances, value, "Node MaxInstances"));
                    node.MaxInstances = value;
                    RaisePropertyChanged(nameof(MaxInstances));
                }
            }
        }
        public byte BurstFrequency
        {
            get => node.BurstFrequency;
            set
            {
                if (node.BurstFrequency != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.BurstFrequency), node, node.BurstFrequency, value, "Node BurstFrequency"));
                    node.BurstFrequency = value;
                    RaisePropertyChanged(nameof(BurstFrequency));
                }
            }
        }
        public byte BurstFrequency_Variance
        {
            get => node.BurstFrequency_Variance;
            set
            {
                if (node.BurstFrequency_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.BurstFrequency_Variance), node, node.BurstFrequency_Variance, value, "Node BurstFrequency_Variance"));
                    node.BurstFrequency_Variance = value;
                    RaisePropertyChanged(nameof(BurstFrequency_Variance));
                }
            }
        }
        public ushort Burst
        {
            get => node.Burst;
            set
            {
                if (node.Burst != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.Burst), node, node.Burst, value, "Node Burst"));
                    node.Burst = value;
                    RaisePropertyChanged(nameof(Burst));
                }
            }
        }
        public ushort Burst_Variance
        {
            get => node.Burst_Variance;
            set
            {
                if (node.Burst_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.Burst_Variance), node, node.Burst_Variance, value, "Node Burst_Variance"));
                    node.Burst_Variance = value;
                    RaisePropertyChanged(nameof(Burst_Variance));
                }
            }
        }
        public KeyframedVector3Value Position => node.Position;
        public KeyframedVector3Value Rotation => node.Rotation;
        public CustomVector4 Position_Variance => node.Position_Variance;
        public CustomVector4 Rotation_Variance => node.Rotation_Variance;

        //Emiiter:
        public ParticleEmitter.ParticleEmitterShape Shape
        {
            get => node.EmitterNode.Shape;
            set
            {
                if (node.EmitterNode.Shape != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.Shape), node.EmitterNode, node.EmitterNode.Shape, value, "Emitter Shape"));
                    node.EmitterNode.Shape = value;
                    RaisePropertyChanged(nameof(Shape));
                }
            }
        }
        public bool EmitFromArea
        {
            get => node.EmitterNode.EmitFromArea;
            set
            {
                if (node.EmitterNode.EmitFromArea != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.EmitFromArea), node.EmitterNode, node.EmitterNode.EmitFromArea, value, "Emitter EmitFromArea"));
                    node.EmitterNode.EmitFromArea = value;
                    RaisePropertyChanged(nameof(EmitFromArea));
                }
            }
        }
        public KeyframedFloatValue EmitterPositionY => node.EmitterNode.Position;
        public float EmitterPositionY_Variance
        {
            get => node.EmitterNode.Position_Variance;
            set
            {
                if (node.EmitterNode.Position_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.Position_Variance), node.EmitterNode, node.EmitterNode.Position_Variance, value, "Emitter Position_Variance"));
                    node.EmitterNode.Position_Variance = value;
                    RaisePropertyChanged(nameof(EmitterPositionY_Variance));
                }
            }
        }
        public KeyframedFloatValue EmitterVelocity => node.EmitterNode.Velocity;
        public float EmitterVelocity_Variance
        {
            get => node.EmitterNode.Velocity_Variance;
            set
            {
                if (node.EmitterNode.Velocity_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.Velocity_Variance), node.EmitterNode, node.EmitterNode.Velocity_Variance, value, "Emitter Velocity_Variance"));
                    node.EmitterNode.Velocity_Variance = value;
                    RaisePropertyChanged(nameof(EmitterVelocity_Variance));
                }
            }
        }
        public KeyframedFloatValue EmitterSize => node.EmitterNode.Size;
        public float EmitterSize_Variance
        {
            get => node.EmitterNode.Size_Variance;
            set
            {
                if (node.EmitterNode.Size_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.Size_Variance), node.EmitterNode, node.EmitterNode.Size_Variance, value, "Emitter Size_Variance"));
                    node.EmitterNode.Size_Variance = value;
                    RaisePropertyChanged(nameof(EmitterSize_Variance));
                }
            }
        }
        public KeyframedFloatValue EmitterSize2 => node.EmitterNode.Size2;
        public float EmitterSize2_Variance
        {
            get => node.EmitterNode.Size2_Variance;
            set
            {
                if (node.EmitterNode.Size2_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.Size2_Variance), node.EmitterNode, node.EmitterNode.Size2_Variance, value, "Emitter Size2_Variance"));
                    node.EmitterNode.Size2_Variance = value;
                    RaisePropertyChanged(nameof(EmitterSize2_Variance));
                }
            }
        }
        public KeyframedFloatValue EmitterAngle => node.EmitterNode.Angle;
        public float EmitterAngle_Variance
        {
            get => node.EmitterNode.Angle_Variance;
            set
            {
                if (node.EmitterNode.Angle_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.Angle_Variance), node.EmitterNode, node.EmitterNode.Angle_Variance, value, "Emitter Angle_Variance"));
                    node.EmitterNode.Angle_Variance = value;
                    RaisePropertyChanged(nameof(EmitterAngle_Variance));
                }
            }
        }
        public float Emitter_EdgeIncrement
        {
            get => node.EmitterNode.EdgeIncrement;
            set
            {
                if (node.EmitterNode.EdgeIncrement != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.EdgeIncrement), node.EmitterNode, node.EmitterNode.EdgeIncrement, value, "Edge Increment"));
                    node.EmitterNode.EdgeIncrement = value;
                    RaisePropertyChanged(nameof(Emitter_EdgeIncrement));
                }
            }
        }
        public float Emitter_F1
        {
            get => node.EmitterNode.F_1;
            set
            {
                if (node.EmitterNode.F_1 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.F_1), node.EmitterNode, node.EmitterNode.F_1, value, "Emitter F1"));
                    node.EmitterNode.F_1 = value;
                    RaisePropertyChanged(nameof(Emitter_F1));
                }
            }
        }
        public float Emitter_F2
        {
            get => node.EmitterNode.F_2;
            set
            {
                if (node.EmitterNode.F_2 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmitterNode.F_2), node.EmitterNode, node.EmitterNode.F_2, value, "Emitter F2"));
                    node.EmitterNode.F_2 = value;
                    RaisePropertyChanged(nameof(Emitter_F2));
                }
            }
        }

        //Emission:
        public ParticleEmission.ParticleEmissionType EmissionType
        {
            get => node.EmissionNode.EmissionType;
            set
            {
                if (node.EmissionNode.EmissionType != value)
                {
                    UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                    {
                        new UndoablePropertyGeneric(nameof(node.EmissionNode.EmissionType), node.EmissionNode, node.EmissionNode.EmissionType, value),
                        new UndoablePropertyGeneric(nameof(empFile.HasBeenEdited), empFile, true, true, true)
                    }, "Emission Type");

                    node.EmissionNode.EmissionType = value;
                    RaisePropertyChanged(nameof(EmissionType));
                    empFile.HasBeenEdited = true;
                }
            }
        }
        public ParticleBillboardType BillboardType
        {
            get => node.EmissionNode.BillboardType;
            set
            {
                if (node.EmissionNode.BillboardType != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.BillboardType), node.EmissionNode, node.EmissionNode.BillboardType, value, "BillboardType"));
                    node.EmissionNode.BillboardType = value;
                    RaisePropertyChanged(nameof(BillboardType));
                }
            }
        }
        public bool BillboardEnabled => BillboardType != ParticleBillboardType.Front;
        public bool VisibleOnlyOnMotion
        {
            get => node.EmissionNode.VelocityOriented;
            set
            {
                if (node.EmissionNode.VelocityOriented != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.VelocityOriented), node.EmissionNode, node.EmissionNode.VelocityOriented, value, "Emission VisibleOnlyOnMotion"));
                    node.EmissionNode.VelocityOriented = value;
                    RaisePropertyChanged(nameof(VisibleOnlyOnMotion));
                }
            }
        }
        public float StartRotation
        {
            get => node.EmissionNode.StartRotation;
            set
            {
                if (node.EmissionNode.StartRotation != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.StartRotation), node.EmissionNode, node.EmissionNode.StartRotation, value, "Emission StartRotation"));
                    node.EmissionNode.StartRotation = value;
                    RaisePropertyChanged(nameof(StartRotation));
                }
            }
        }
        public float StartRotation_Variance
        {
            get => node.EmissionNode.StartRotation_Variance;
            set
            {
                if (node.EmissionNode.StartRotation_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.StartRotation_Variance), node.EmissionNode, node.EmissionNode.StartRotation_Variance, value, "Emission StartRotation_Variance"));
                    node.EmissionNode.StartRotation_Variance = value;
                    RaisePropertyChanged(nameof(StartRotation_Variance));
                }
            }
        }
        public KeyframedFloatValue ActiveRotation => node.EmissionNode.ActiveRotation;
        public float ActiveRotation_Variance
        {
            get => node.EmissionNode.ActiveRotation_Variance;
            set
            {
                if (node.EmissionNode.ActiveRotation_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ActiveRotation_Variance), node.EmissionNode, node.EmissionNode.ActiveRotation_Variance, value, "Emission ActiveRotation_Variance"));
                    node.EmissionNode.ActiveRotation_Variance = value;
                    RaisePropertyChanged(nameof(ActiveRotation_Variance));
                }
            }
        }
        public CustomVector4 EmissionRotationAxis => node.EmissionNode.RotationAxis;

        //ConeExtrude
        public ushort ConeExtrude_Duration
        {
            get => node.EmissionNode.ConeExtrude.Duration;
            set
            {
                if (node.EmissionNode.ConeExtrude.Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ConeExtrude.Duration), node.EmissionNode.ConeExtrude, node.EmissionNode.ConeExtrude.Duration, value, "ConeExtrude Duration"));
                    node.EmissionNode.ConeExtrude.Duration = value;
                    RaisePropertyChanged(nameof(ConeExtrude_Duration));
                }
            }
        }
        public ushort ConeExtrude_Duration_Variance
        {
            get => node.EmissionNode.ConeExtrude.Duration_Variance;
            set
            {
                if (node.EmissionNode.ConeExtrude.Duration_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ConeExtrude.Duration_Variance), node.EmissionNode.ConeExtrude, node.EmissionNode.ConeExtrude.Duration_Variance, value, "ConeExtrude Duration Variance"));
                    node.EmissionNode.ConeExtrude.Duration_Variance = value;
                    RaisePropertyChanged(nameof(ConeExtrude_Duration_Variance));
                }
            }
        }
        public ushort ConeExtrude_TimeBetweenTwoStep
        {
            get => node.EmissionNode.ConeExtrude.StepDuration;
            set
            {
                if (node.EmissionNode.ConeExtrude.StepDuration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ConeExtrude.StepDuration), node.EmissionNode.ConeExtrude, node.EmissionNode.ConeExtrude.StepDuration, value, "ConeExtrude TimeBetweenTwoStep"));
                    node.EmissionNode.ConeExtrude.StepDuration = value;
                    RaisePropertyChanged(nameof(ConeExtrude_TimeBetweenTwoStep));
                }
            }
        }
        public ushort ConeExtrude_I_08
        {
            get => node.EmissionNode.ConeExtrude.I_08;
            set
            {
                if (node.EmissionNode.ConeExtrude.I_08 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ConeExtrude.I_08), node.EmissionNode.ConeExtrude, node.EmissionNode.ConeExtrude.I_08, value, "ConeExtrude I_08"));
                    node.EmissionNode.ConeExtrude.I_08 = value;
                    RaisePropertyChanged(nameof(ConeExtrude_I_08));
                }
            }
        }
        public ushort ConeExtrude_I_10
        {
            get => node.EmissionNode.ConeExtrude.I_10;
            set
            {
                if (node.EmissionNode.ConeExtrude.I_10 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ConeExtrude.I_10), node.EmissionNode.ConeExtrude, node.EmissionNode.ConeExtrude.I_10, value, "ConeExtrude I_10"));
                    node.EmissionNode.ConeExtrude.I_10 = value;
                    RaisePropertyChanged(nameof(ConeExtrude_I_10));
                }
            }
        }

        //Mesh
        public string Mesh_HasMesh => node.EmissionNode.Mesh.EmgFile != null ? "Has mesh data" : "Has NO mesh data";


        //ParticleTexture
        public KeyframedColorValue Color1 => node.EmissionNode.Texture.Color1;
        public KeyframedColorValue Color2 => node.EmissionNode.Texture.Color2;
        public KeyframedFloatValue Color1_Transparency => node.EmissionNode.Texture.Color1_Transparency;
        public KeyframedFloatValue Color2_Transparency => node.EmissionNode.Texture.Color2_Transparency;
        public CustomColor Color_Variance => node.EmissionNode.Texture.Color_Variance;
        public float ColorVariance_Transparency
        {
            get => node.EmissionNode.Texture.Color_Variance.A;
            set
            {
                if (node.EmissionNode.Texture.Color_Variance.A != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.Color_Variance.A), node.EmissionNode.Texture.Color_Variance, node.EmissionNode.Texture.Color_Variance.A, value, "Color Variance Alpha"));
                    node.EmissionNode.Texture.Color_Variance.A = value;
                    RaisePropertyChanged(nameof(ColorVariance_Transparency));
                }
            }
        }
        public KeyframedFloatValue ScaleBase => node.EmissionNode.Texture.ScaleBase;
        public float ScaleBase_Variance
        {
            get => node.EmissionNode.Texture.ScaleBase_Variance;
            set
            {
                if (node.EmissionNode.Texture.ScaleBase_Variance != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.ScaleBase_Variance), node.EmissionNode.Texture, node.EmissionNode.Texture.ScaleBase_Variance, value, "ScaleBase Variance"));
                    node.EmissionNode.Texture.ScaleBase_Variance = value;
                    RaisePropertyChanged(nameof(ScaleBase_Variance));
                }
            }
        }
        public KeyframedVector2Value ScaleXY => node.EmissionNode.Texture.ScaleXY;
        public CustomVector4 ScaleXY_Variance => node.EmissionNode.Texture.ScaleXY_Variance;
        public byte Texture_I_00
        {
            get => node.EmissionNode.Texture.I_00;
            set
            {
                if (node.EmissionNode.Texture.I_00 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.I_00), node.EmissionNode.Texture, node.EmissionNode.Texture.I_00, value, "Texture I_00"));
                    node.EmissionNode.Texture.I_00 = value;
                    RaisePropertyChanged(nameof(Texture_I_00));
                }
            }
        }
        public byte Texture_I_01
        {
            get => node.EmissionNode.Texture.I_01;
            set
            {
                if (node.EmissionNode.Texture.I_01 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.I_01), node.EmissionNode.Texture, node.EmissionNode.Texture.I_01, value, "Texture I_01"));
                    node.EmissionNode.Texture.I_01 = value;
                    RaisePropertyChanged(nameof(Texture_I_01));
                }
            }
        }
        public byte Texture_I_02
        {
            get => node.EmissionNode.Texture.I_02;
            set
            {
                if (node.EmissionNode.Texture.I_02 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.I_02), node.EmissionNode.Texture, node.EmissionNode.Texture.I_02, value, "Texture I_02"));
                    node.EmissionNode.Texture.I_02 = value;
                    RaisePropertyChanged(nameof(Texture_I_02));
                }
            }
        }
        public byte Texture_I_03
        {
            get => node.EmissionNode.Texture.I_03;
            set
            {
                if (node.EmissionNode.Texture.I_03 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.I_03), node.EmissionNode.Texture, node.EmissionNode.Texture.I_03, value, "Texture I_03"));
                    node.EmissionNode.Texture.I_03 = value;
                    RaisePropertyChanged(nameof(Texture_I_03));
                }
            }
        }
        public int Texture_I_08
        {
            get => node.EmissionNode.Texture.I_08;
            set
            {
                if (node.EmissionNode.Texture.I_08 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.I_08), node.EmissionNode.Texture, node.EmissionNode.Texture.I_08, value, "Texture I_08"));
                    node.EmissionNode.Texture.I_08 = value;
                    RaisePropertyChanged(nameof(Texture_I_08));
                }
            }
        }
        public int Texture_I_12
        {
            get => node.EmissionNode.Texture.I_12;
            set
            {
                if (node.EmissionNode.Texture.I_12 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.I_12), node.EmissionNode.Texture, node.EmissionNode.Texture.I_12, value, "Texture I_12"));
                    node.EmissionNode.Texture.I_12 = value;
                    RaisePropertyChanged(nameof(Texture_I_12));
                }
            }
        }
        public float Texture_F_96
        {
            get => node.EmissionNode.Texture.F_96;
            set
            {
                if (node.EmissionNode.Texture.F_96 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.F_96), node.EmissionNode.Texture, node.EmissionNode.Texture.F_96, value, "Texture F_96"));
                    node.EmissionNode.Texture.F_96 = value;
                    RaisePropertyChanged(nameof(Texture_F_96));
                }
            }
        }
        public float Texture_F_100
        {
            get => node.EmissionNode.Texture.F_100;
            set
            {
                if (node.EmissionNode.Texture.F_100 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.F_100), node.EmissionNode.Texture, node.EmissionNode.Texture.F_100, value, "Texture F_100"));
                    node.EmissionNode.Texture.F_100 = value;
                    RaisePropertyChanged(nameof(Texture_F_100));
                }
            }
        }
        public float Texture_F_104
        {
            get => node.EmissionNode.Texture.F_104;
            set
            {
                if (node.EmissionNode.Texture.F_104 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.F_104), node.EmissionNode.Texture, node.EmissionNode.Texture.F_104, value, "Texture F_104"));
                    node.EmissionNode.Texture.F_104 = value;
                    RaisePropertyChanged(nameof(Texture_F_104));
                }
            }
        }
        public float Texture_F_108
        {
            get => node.EmissionNode.Texture.F_108;
            set
            {
                if (node.EmissionNode.Texture.F_108 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.F_108), node.EmissionNode.Texture, node.EmissionNode.Texture.F_108, value, "Texture F_108"));
                    node.EmissionNode.Texture.F_108 = value;
                    RaisePropertyChanged(nameof(Texture_F_108));
                }
            }
        }
        public float Texture_RenderDepth
        {
            get => node.EmissionNode.Texture.RenderDepth;
            set
            {
                if (node.EmissionNode.Texture.RenderDepth != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.RenderDepth), node.EmissionNode.Texture, node.EmissionNode.Texture.RenderDepth, value, "Texture RenderDepth"));
                    node.EmissionNode.Texture.RenderDepth = value;
                    RaisePropertyChanged(nameof(Texture_RenderDepth));
                }
            }
        }

        //Node Flags:
        public bool NodeFlags_Unk1
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk1);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk1, value);
                RaisePropertyChanged(() => NodeFlags_Unk1);
            }
        }
        public bool NodeFlags_Loop
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Loop);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Loop, value);
                RaisePropertyChanged(() => NodeFlags_Loop);
            }
        }
        public bool NodeFlags_FlashOnGen
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.FlashOnGen);
            }
            set
            {
                SetNodeFlags(NodeFlags1.FlashOnGen, value);
                RaisePropertyChanged(() => NodeFlags_FlashOnGen);
            }
        }
        public bool NodeFlags_Unk3
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk3);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk3, value);
                RaisePropertyChanged(() => NodeFlags_Unk3);
            }
        }
        public bool NodeFlags_Unk5
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk5);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk5, value);
                RaisePropertyChanged(() => NodeFlags_Unk5);
            }
        }
        public bool NodeFlags_Unk6
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk6);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk6, value);
                RaisePropertyChanged(() => NodeFlags_Unk6);
            }
        }
        public bool NodeFlags_Unk7
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk7);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk7, value);
                RaisePropertyChanged(() => NodeFlags_Unk7);
            }
        }
        public bool NodeFlags_Unk8
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk8);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk8, value);
                RaisePropertyChanged(() => NodeFlags_Unk8);
            }
        }
        public bool NodeFlags_Unk9
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk9);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk9, value);
                RaisePropertyChanged(() => NodeFlags_Unk9);
            }
        }
        public bool NodeFlags_Unk10
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk10);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk10, value);
                RaisePropertyChanged(() => NodeFlags_Unk10);
            }
        }
        public bool NodeFlags_Unk11
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk11);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk11, value);
                RaisePropertyChanged(() => NodeFlags_Unk11);
            }
        }
        public bool NodeFlags_Hide
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Hide);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Hide, value);
                RaisePropertyChanged(() => NodeFlags_Hide);
            }
        }
        public bool NodeFlags_ScaleXY
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.EnableScaleXY);
            }
            set
            {
                SetNodeFlags(NodeFlags1.EnableScaleXY, value);
                RaisePropertyChanged(() => NodeFlags_ScaleXY);
            }
        }
        public bool NodeFlags_Unk14
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk14);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk14, value);
                RaisePropertyChanged(() => NodeFlags_Unk14);
            }
        }
        public bool NodeFlags_SecondaryColor
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.EnableSecondaryColor);
            }
            set
            {
                SetNodeFlags(NodeFlags1.EnableSecondaryColor, value);
                RaisePropertyChanged(() => NodeFlags_SecondaryColor);
            }
        }
        public bool NodeFlags_Unk16
        {
            get
            {
                return node.NodeFlags.HasFlag(NodeFlags1.Unk16);
            }
            set
            {
                SetNodeFlags(NodeFlags1.Unk16, value);
                RaisePropertyChanged(() => NodeFlags_Unk16);
            }
        }

        public bool NodeFlags2_Unk1
        {
            get
            {
                return node.NodeFlags2.HasFlag(NodeFlags2.Unk1);
            }
            set
            {
                SetNodeFlags2(NodeFlags2.Unk1, value);
                RaisePropertyChanged(() => NodeFlags2_Unk1);
            }
        }
        public bool NodeFlags2_Unk2
        {
            get
            {
                return node.NodeFlags2.HasFlag(NodeFlags2.Unk2);
            }
            set
            {
                SetNodeFlags2(NodeFlags2.Unk2, value);
                RaisePropertyChanged(() => NodeFlags2_Unk2);
            }
        }
        public bool NodeFlags2_Unk3
        {
            get
            {
                return node.NodeFlags2.HasFlag(NodeFlags2.Unk3);
            }
            set
            {
                SetNodeFlags2(NodeFlags2.Unk3, value);
                RaisePropertyChanged(() => NodeFlags2_Unk3);
            }
        }
        public bool NodeFlags2_Unk4
        {
            get
            {
                return node.NodeFlags2.HasFlag(NodeFlags2.Unk4);
            }
            set
            {
                SetNodeFlags2(NodeFlags2.Unk4, value);
                RaisePropertyChanged(() => NodeFlags2_Unk4);
            }
        }
        public bool NodeFlags2_RandomRotationDir
        {
            get
            {
                return node.NodeFlags2.HasFlag(NodeFlags2.RandomRotationDir);
            }
            set
            {
                SetNodeFlags2(NodeFlags2.RandomRotationDir, value);
                RaisePropertyChanged(() => NodeFlags2_RandomRotationDir);
            }
        }
        public bool NodeFlags2_Unk6
        {
            get
            {
                return node.NodeFlags2.HasFlag(NodeFlags2.Unk6);
            }
            set
            {
                SetNodeFlags2(NodeFlags2.Unk6, value);
                RaisePropertyChanged(() => NodeFlags2_Unk6);
            }
        }
        public bool NodeFlags2_RandomUpVector
        {
            get
            {
                return node.NodeFlags2.HasFlag(NodeFlags2.RandomUpVector);
            }
            set
            {
                SetNodeFlags2(NodeFlags2.RandomUpVector, value);
                RaisePropertyChanged(() => NodeFlags2_RandomUpVector);
            }
        }
        public bool NodeFlags2_Unk8
        {
            get
            {
                return node.NodeFlags2.HasFlag(NodeFlags2.Unk8);
            }
            set
            {
                SetNodeFlags2(NodeFlags2.Unk8, value);
                RaisePropertyChanged(() => NodeFlags2_Unk8);
            }
        }

        //Node Unknown:
        public ushort Node_I_128
        {
            get => node.I_128;
            set
            {
                if (node.I_128 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.I_128), node, node.I_128, value, "Node I_128"));
                    node.I_128 = value;
                    RaisePropertyChanged(nameof(Node_I_128));
                }
            }
        }
        public float Node_F_132
        {
            get => node.F_132;
            set
            {
                if (node.F_132 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.F_132), node, node.F_132, value, "Node F_132"));
                    node.F_132 = value;
                    RaisePropertyChanged(nameof(Node_F_132));
                }
            }
        }

        public EmmMaterial MaterialRef
        {
            get => node.EmissionNode.Texture.MaterialRef;
            set
            {
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Texture.MaterialRef), node.EmissionNode.Texture, node.EmissionNode.Texture.MaterialRef, value, "ETR -> Material"));
                node.EmissionNode.Texture.MaterialRef = value;
                RaisePropertyChanged(nameof(MaterialRef));
            }
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged(nameof(MaterialRef));

            //Base
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(NodeType));
            RaisePropertyChanged(nameof(StartTime));
            RaisePropertyChanged(nameof(StartTime_Variance));
            RaisePropertyChanged(nameof(Lifetime));
            RaisePropertyChanged(nameof(Lifetime_Variance));
            RaisePropertyChanged(nameof(MaxInstances));
            RaisePropertyChanged(nameof(BurstFrequency));
            RaisePropertyChanged(nameof(BurstFrequency_Variance));
            RaisePropertyChanged(nameof(Burst));
            RaisePropertyChanged(nameof(Burst_Variance));
            RaisePropertyChanged(nameof(BillboardType));
            RaisePropertyChanged(nameof(Position));
            RaisePropertyChanged(nameof(Rotation));
            RaisePropertyChanged(nameof(Position_Variance));
            RaisePropertyChanged(nameof(Rotation_Variance));

            //Emitters
            RaisePropertyChanged(nameof(Shape));
            RaisePropertyChanged(nameof(EmitFromArea));
            RaisePropertyChanged(nameof(EmitterPositionY));
            RaisePropertyChanged(nameof(EmitterPositionY_Variance));
            RaisePropertyChanged(nameof(EmitterVelocity));
            RaisePropertyChanged(nameof(EmitterVelocity_Variance));
            RaisePropertyChanged(nameof(EmitterSize));
            RaisePropertyChanged(nameof(EmitterSize_Variance));
            RaisePropertyChanged(nameof(EmitterSize2));
            RaisePropertyChanged(nameof(EmitterSize2_Variance));
            RaisePropertyChanged(nameof(EmitterAngle));
            RaisePropertyChanged(nameof(EmitterAngle_Variance));
            RaisePropertyChanged(nameof(Emitter_EdgeIncrement));
            RaisePropertyChanged(nameof(Emitter_F1));
            RaisePropertyChanged(nameof(Emitter_F2));

            //Emission base
            RaisePropertyChanged(nameof(EmissionType));
            RaisePropertyChanged(nameof(BillboardEnabled));
            RaisePropertyChanged(nameof(VisibleOnlyOnMotion));
            RaisePropertyChanged(nameof(StartRotation));
            RaisePropertyChanged(nameof(StartRotation_Variance));
            RaisePropertyChanged(nameof(ActiveRotation));
            RaisePropertyChanged(nameof(ActiveRotation_Variance));
            RaisePropertyChanged(nameof(EmissionRotationAxis));

            //Cone Extrude
            RaisePropertyChanged(nameof(ConeExtrude_Duration));
            RaisePropertyChanged(nameof(ConeExtrude_Duration_Variance));
            RaisePropertyChanged(nameof(ConeExtrude_TimeBetweenTwoStep));
            RaisePropertyChanged(nameof(ConeExtrude_I_08));
            RaisePropertyChanged(nameof(ConeExtrude_I_10));

            //Mesh
            RaisePropertyChanged(nameof(Mesh_HasMesh));

            //Texture
            RaisePropertyChanged(nameof(Color1));
            RaisePropertyChanged(nameof(Color2));
            RaisePropertyChanged(nameof(Color1_Transparency));
            RaisePropertyChanged(nameof(Color2_Transparency));
            RaisePropertyChanged(nameof(Color_Variance));
            RaisePropertyChanged(nameof(ColorVariance_Transparency));
            RaisePropertyChanged(nameof(ScaleBase));
            RaisePropertyChanged(nameof(ScaleBase_Variance));
            RaisePropertyChanged(nameof(ScaleXY));
            RaisePropertyChanged(nameof(ScaleXY_Variance));
            RaisePropertyChanged(nameof(Texture_I_00));
            RaisePropertyChanged(nameof(Texture_I_01));
            RaisePropertyChanged(nameof(Texture_I_02));
            RaisePropertyChanged(nameof(Texture_I_03));
            RaisePropertyChanged(nameof(Texture_I_08));
            RaisePropertyChanged(nameof(Texture_I_12));
            RaisePropertyChanged(nameof(Texture_F_96));
            RaisePropertyChanged(nameof(Texture_F_100));
            RaisePropertyChanged(nameof(Texture_F_104));
            RaisePropertyChanged(nameof(Texture_F_108));
            RaisePropertyChanged(nameof(Texture_RenderDepth));


            RaisePropertyChanged(nameof(NodeFlags_Unk1));
            RaisePropertyChanged(nameof(NodeFlags_Loop));
            RaisePropertyChanged(nameof(NodeFlags_FlashOnGen));
            RaisePropertyChanged(nameof(NodeFlags_Unk3));
            RaisePropertyChanged(nameof(NodeFlags_Unk5));
            RaisePropertyChanged(nameof(NodeFlags_Unk6));
            RaisePropertyChanged(nameof(NodeFlags_Unk7));
            RaisePropertyChanged(nameof(NodeFlags_Unk8));
            RaisePropertyChanged(nameof(NodeFlags_Unk9));
            RaisePropertyChanged(nameof(NodeFlags_Unk10));
            RaisePropertyChanged(nameof(NodeFlags_Unk11));
            RaisePropertyChanged(nameof(NodeFlags_Hide));
            RaisePropertyChanged(nameof(NodeFlags_ScaleXY));
            RaisePropertyChanged(nameof(NodeFlags_Unk14));
            RaisePropertyChanged(nameof(NodeFlags_SecondaryColor));
            RaisePropertyChanged(nameof(NodeFlags_Unk16));

            RaisePropertyChanged(nameof(NodeFlags2_Unk1));
            RaisePropertyChanged(nameof(NodeFlags2_Unk2));
            RaisePropertyChanged(nameof(NodeFlags2_Unk3));
            RaisePropertyChanged(nameof(NodeFlags2_Unk4));
            RaisePropertyChanged(nameof(NodeFlags2_RandomRotationDir));
            RaisePropertyChanged(nameof(NodeFlags2_Unk6));
            RaisePropertyChanged(nameof(NodeFlags2_RandomUpVector));
            RaisePropertyChanged(nameof(NodeFlags2_Unk8));

            RaisePropertyChanged(nameof(Node_I_128));
            RaisePropertyChanged(nameof(Node_F_132));

        }

        public void SetContext(ParticleNode node, EMP_File empFile)
        {
           this.node = node;
           this.empFile = empFile;
           UpdateProperties();
        }


        private void SetNodeFlags(NodeFlags1 flag, bool state)
        {
            var newFlag = node.NodeFlags.SetFlag(flag, state);

            if (node.NodeFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ParticleNode>(nameof(ParticleNode.NodeFlags), node, node.NodeFlags, newFlag, "Node Flags"));
                node.NodeFlags = newFlag;
            }
        }

        private void SetNodeFlags2(NodeFlags2 flag, bool state)
        {
            var newFlag = node.NodeFlags2.SetFlag(flag, state);

            if (node.NodeFlags2 != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ParticleNode>(nameof(ParticleNode.NodeFlags2), node, node.NodeFlags2, newFlag, "Node Flags"));
                node.NodeFlags2 = newFlag;
            }
        }

        private void AddUndo(IUndoRedo undo, string desc)
        {
            //todo: implement particle update event
        }
    }
}
