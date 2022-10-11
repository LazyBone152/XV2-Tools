using LB_Common.Numbers;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;

namespace EEPK_Organiser.ViewModel
{
    public class EmpNodeViewModel : ObservableObject
    {
        private readonly ParticleNode node;

        public string Name
        {
            get => node.Name;
            set
            {
                if(node.Name != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.Name), node, node.Name, value, "Node Name"));
                    node.Name = value;
                    RaisePropertyChanged(nameof(Name));
                }
            }
        }
        public ParticleNodeType NodeType
        {
            get => node.NodeType;
            set
            {
                if (node.NodeType != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.NodeType), node, node.NodeType, value, "NodeType"));
                    node.NodeType = value;
                    RaisePropertyChanged(nameof(NodeType));
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
        public ParticleAutoRotationType AutoRotationType
        {
            get => node.AutoRotationType;
            set
            {
                if (node.AutoRotationType != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.AutoRotationType), node, node.AutoRotationType, value, "Node AutoRotationType"));
                    node.AutoRotationType = value;
                    RaisePropertyChanged(nameof(AutoRotationType));
                }
            }
        }
        public bool Loop
        {
            get => node.Loop;
            set
            {
                if (node.Loop != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.Loop), node, node.Loop, value, "Node Loop"));
                    node.Loop = value;
                    RaisePropertyChanged(nameof(Loop));
                }
            }
        }
        public bool Hide
        {
            get => node.Hide;
            set
            {
                if (node.Hide != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.Hide), node, node.Hide, value, "Node Hide"));
                    node.Hide = value;
                    RaisePropertyChanged(nameof(Hide));
                }
            }
        }
        public bool UseColor2
        {
            get => node.UseColor2;
            set
            {
                if (node.UseColor2 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.UseColor2), node, node.UseColor2, value, "Node UseColor2"));
                    node.UseColor2 = value;
                    RaisePropertyChanged(nameof(UseColor2));
                }
            }
        }
        public bool UseScaleXY
        {
            get => node.UseScaleXY;
            set
            {
                if (node.UseScaleXY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.UseScaleXY), node, node.UseScaleXY, value, "Node UseScaleXY"));
                    node.UseScaleXY = value;
                    RaisePropertyChanged(nameof(UseScaleXY));
                }
            }
        }
        public bool RandomRotationDirection
        {
            get => node.EnableRandomRotationDirection;
            set
            {
                if (node.EnableRandomRotationDirection != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EnableRandomRotationDirection), node, node.EnableRandomRotationDirection, value, "Node RandomRotationDirection"));
                    node.EnableRandomRotationDirection = value;
                    RaisePropertyChanged(nameof(RandomRotationDirection));
                }
            }
        }
        public bool RandomUpVector
        {
            get => node.EnableRandomUpVectorOnVirtualCone;
            set
            {
                if (node.EnableRandomUpVectorOnVirtualCone != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EnableRandomUpVectorOnVirtualCone), node, node.EnableRandomUpVectorOnVirtualCone, value, "Node RandomUpVector"));
                    node.EnableRandomUpVectorOnVirtualCone = value;
                    RaisePropertyChanged(nameof(RandomUpVector));
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
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.EmissionType), node.EmissionNode, node.EmissionNode.EmissionType, value, "Emission Type"));
                    node.EmissionNode.EmissionType = value;
                    RaisePropertyChanged(nameof(EmissionType));
                }
            }
        }
        public bool AutoRotationEnabled
        {
            get => node.EmissionNode.AutoRotation;
            set
            {
                if (node.EmissionNode.AutoRotation != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.AutoRotation), node.EmissionNode, node.EmissionNode.AutoRotation, value, "Emission AutoRotation"));
                    node.EmissionNode.AutoRotation = value;
                    RaisePropertyChanged(nameof(AutoRotationEnabled));
                }
            }
        }
        public bool VisibleOnlyOnMotion
        {
            get => node.EmissionNode.VisibleOnlyOnMotion;
            set
            {
                if (node.EmissionNode.VisibleOnlyOnMotion != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.VisibleOnlyOnMotion), node.EmissionNode, node.EmissionNode.VisibleOnlyOnMotion, value, "Emission VisibleOnlyOnMotion"));
                    node.EmissionNode.VisibleOnlyOnMotion = value;
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
        public CustomVector4 EmissionRotationAxis => node.EmissionNode.RotationAxis; //I think, defines an up-vector?

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
            get => node.EmissionNode.ConeExtrude.TimeBetweenTwoStep;
            set
            {
                if (node.EmissionNode.ConeExtrude.TimeBetweenTwoStep != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ConeExtrude.TimeBetweenTwoStep), node.EmissionNode.ConeExtrude, node.EmissionNode.ConeExtrude.TimeBetweenTwoStep, value, "ConeExtrude TimeBetweenTwoStep"));
                    node.EmissionNode.ConeExtrude.TimeBetweenTwoStep = value;
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
        public int Mesh_I_32
        {
            get => node.EmissionNode.Mesh.I_32;
            set
            {
                if (node.EmissionNode.Mesh.I_32 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Mesh.I_32), node.EmissionNode.Mesh, node.EmissionNode.Mesh.I_32, value, "Mesh I_32"));
                    node.EmissionNode.Mesh.I_32 = value;
                    RaisePropertyChanged(nameof(Mesh_I_32));
                }
            }
        }
        public int Mesh_I_40
        {
            get => node.EmissionNode.Mesh.I_40;
            set
            {
                if (node.EmissionNode.Mesh.I_40 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Mesh.I_40), node.EmissionNode.Mesh, node.EmissionNode.Mesh.I_40, value, "Mesh I_40"));
                    node.EmissionNode.Mesh.I_40 = value;
                    RaisePropertyChanged(nameof(Mesh_I_40));
                }
            }
        }
        public int Mesh_I_44
        {
            get => node.EmissionNode.Mesh.I_44;
            set
            {
                if (node.EmissionNode.Mesh.I_44 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.Mesh.I_44), node.EmissionNode.Mesh, node.EmissionNode.Mesh.I_44, value, "Mesh I_44"));
                    node.EmissionNode.Mesh.I_44 = value;
                    RaisePropertyChanged(nameof(Mesh_I_44));
                }
            }
        }

        //ShapeDraw
        public ParticleAutoRotationType ShapeDraw_AutoRotationType
        {
            get => node.EmissionNode.ShapeDraw.AutoRotationType;
            set
            {
                if (node.EmissionNode.ShapeDraw.AutoRotationType != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ShapeDraw.AutoRotationType), node.EmissionNode.ShapeDraw, node.EmissionNode.ShapeDraw.AutoRotationType, value, "ShapwDraw AutoRotationType"));
                    node.EmissionNode.ShapeDraw.AutoRotationType = value;
                    RaisePropertyChanged(nameof(ShapeDraw_AutoRotationType));
                }
            }
        }
        public ushort ShapwDraw_I_24
        {
            get => node.EmissionNode.ShapeDraw.I_24;
            set
            {
                if (node.EmissionNode.ShapeDraw.I_24 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ShapeDraw.I_24), node.EmissionNode.ShapeDraw, node.EmissionNode.ShapeDraw.I_24, value, "ShapeDraw I_24"));
                    node.EmissionNode.ShapeDraw.I_24 = value;
                    RaisePropertyChanged(nameof(ShapwDraw_I_24));
                }
            }
        }
        public ushort ShapwDraw_I_26
        {
            get => node.EmissionNode.ShapeDraw.I_26;
            set
            {
                if (node.EmissionNode.ShapeDraw.I_26 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ShapeDraw.I_26), node.EmissionNode.ShapeDraw, node.EmissionNode.ShapeDraw.I_26, value, "ShapeDraw I_26"));
                    node.EmissionNode.ShapeDraw.I_26 = value;
                    RaisePropertyChanged(nameof(ShapwDraw_I_26));
                }
            }
        }
        public ushort ShapwDraw_I_28
        {
            get => node.EmissionNode.ShapeDraw.I_28;
            set
            {
                if (node.EmissionNode.ShapeDraw.I_28 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ShapeDraw.I_28), node.EmissionNode.ShapeDraw, node.EmissionNode.ShapeDraw.I_28, value, "ShapeDraw I_28"));
                    node.EmissionNode.ShapeDraw.I_28 = value;
                    RaisePropertyChanged(nameof(ShapwDraw_I_28));
                }
            }
        }
        public ushort ShapwDraw_I_30
        {
            get => node.EmissionNode.ShapeDraw.I_30;
            set
            {
                if (node.EmissionNode.ShapeDraw.I_30 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(node.EmissionNode.ShapeDraw.I_30), node.EmissionNode.ShapeDraw, node.EmissionNode.ShapeDraw.I_30, value, "ShapeDraw I_30"));
                    node.EmissionNode.ShapeDraw.I_30 = value;
                    RaisePropertyChanged(nameof(ShapwDraw_I_30));
                }
            }
        }

        //ParticleTexture


        public EmpNodeViewModel(ParticleNode node)
        {
            this.node = node;
        }

        public void UpdateProperties()
        {
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
            RaisePropertyChanged(nameof(AutoRotationType));
            RaisePropertyChanged(nameof(Loop));
            RaisePropertyChanged(nameof(Hide));
            RaisePropertyChanged(nameof(UseColor2));
            RaisePropertyChanged(nameof(UseScaleXY));
            RaisePropertyChanged(nameof(RandomRotationDirection));
            RaisePropertyChanged(nameof(RandomUpVector));
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
            RaisePropertyChanged(nameof(Emitter_F1));
            RaisePropertyChanged(nameof(Emitter_F2));

            //Emission base
            RaisePropertyChanged(nameof(EmissionType));
            RaisePropertyChanged(nameof(AutoRotationEnabled));
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
            RaisePropertyChanged(nameof(Mesh_I_32));
            RaisePropertyChanged(nameof(Mesh_I_40));
            RaisePropertyChanged(nameof(Mesh_I_44));

            //ShapeDraw
            RaisePropertyChanged(nameof(ShapeDraw_AutoRotationType));
            RaisePropertyChanged(nameof(ShapwDraw_I_24));
            RaisePropertyChanged(nameof(ShapwDraw_I_26));
            RaisePropertyChanged(nameof(ShapwDraw_I_28));
            RaisePropertyChanged(nameof(ShapwDraw_I_30));

        }

    }
}
