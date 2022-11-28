using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.View.Controls
{
    //Used as an initial base:
    //https://www.codeproject.com/articles/769055/interpolate-d-points-usign-bezier-curves-in-wpf

    public partial class ShapeDraw : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DP

        public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(nameof(Points), typeof(IEnumerable), typeof(ShapeDraw), new PropertyMetadata(null, PointsChangedCallback));

        public AsyncObservableCollection<ShapeDrawPoint> Points
        {
            get { return (AsyncObservableCollection<ShapeDrawPoint>)GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }


        public static readonly DependencyProperty SelectedPointProperty = DependencyProperty.Register(nameof(SelectedPoint), typeof(ShapePointRef), typeof(ShapeDraw), new PropertyMetadata(null, SelectedPointChangedCallback));

        public ShapePointRef SelectedPoint
        {
            get => (ShapePointRef)GetValue(SelectedPointProperty);
            set => SetValue(SelectedPointProperty, value);
        }


        private static void PointsChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ShapeDraw shapeDrawControl = dependencyObject as ShapeDraw;
            if (shapeDrawControl == null) return;

            if (dependencyPropertyChangedEventArgs.NewValue is AsyncObservableCollection<ShapeDrawPoint> newPoints)
            {
                newPoints.CollectionChanged += shapeDrawControl.OnPointCollectionChanged;
                shapeDrawControl.RegisterCollectionItemPropertyChanged(newPoints);
            }

            if (dependencyPropertyChangedEventArgs.OldValue is AsyncObservableCollection<ShapeDrawPoint> oldPoints)
            {
                oldPoints.CollectionChanged -= shapeDrawControl.OnPointCollectionChanged;
                shapeDrawControl.UnRegisterCollectionItemPropertyChanged(oldPoints);
            }

            if (dependencyPropertyChangedEventArgs.NewValue != null)
                shapeDrawControl.SetPathData();
        }

        private static void SelectedPointChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if(dependencyObject is ShapeDraw shapeDrawControl)
            {
                if(dependencyPropertyChangedEventArgs.OldValue is ShapePointRef oldPoint)
                {
                    oldPoint.PropertyChanged -= SelectedPoint_PropertyChanged;
                }

                if (dependencyPropertyChangedEventArgs.NewValue is ShapePointRef newPoint)
                {
                    newPoint.PropertyChanged += SelectedPoint_PropertyChanged;
                }

                shapeDrawControl.NotifyPropertyChanged(nameof(SelectedPoint));
            }
        }

        private static void SelectedPoint_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //Do something...
        }

        #endregion

        #region PathColor

        public Brush PathColor
        {
            get { return (Brush)GetValue(PathColorProperty); }
            set { SetValue(PathColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PathColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathColorProperty =
            DependencyProperty.Register("PathColor", typeof(Brush), typeof(ShapeDraw),
                                        new PropertyMetadata(Brushes.Black));

        #endregion

        public ShapeDraw()
        {
            DataContext = this;
            InitializeComponent();

            //Size must always be set to this constant
            Width = ShapeDrawPoint.SHAPE_DRAW_CONTROL_SIZE;
            Height = ShapeDrawPoint.SHAPE_DRAW_CONTROL_SIZE;
        }

        private void SetPathData()
        {
            if (Points == null) return;
            List<Point> points = new List<Point>();

            foreach (ShapeDrawPoint point in Points)
            {
                float x = ShapeDrawPoint.ConvertPointToPixelSpace(point.X);
                float y = ShapeDrawPoint.ConvertPointToPixelSpace(point.Y);

                points.Add(new Point(x, y));
            }

            if (points.Count <= 1)
            {
                path.Data = null;
                return;
            }

            PathFigure myPathFigure = new PathFigure { StartPoint = points.FirstOrDefault() };


            PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();

            foreach (var point in points.GetRange(1, points.Count - 1))
            {
                LineSegment myLineSegment = new LineSegment { Point = point };
                myPathSegmentCollection.Add(myLineSegment);
            }

            myPathFigure.Segments = myPathSegmentCollection;

            PathFigureCollection myPathFigureCollection = new PathFigureCollection {myPathFigure};

            PathGeometry myPathGeometry = new PathGeometry { Figures = myPathFigureCollection };

            path.Data = myPathGeometry;
        }

        private void RegisterCollectionItemPropertyChanged(AsyncObservableCollection<ShapeDrawPoint> collection)
        {
            if (collection == null)
                return;

            foreach (ShapeDrawPoint point in collection)
                point.PropertyChanged += OnPointPropertyChanged;
        }

        private void UnRegisterCollectionItemPropertyChanged(AsyncObservableCollection<ShapeDrawPoint> collection)
        {
            if (collection == null)
                return;

            foreach (ShapeDrawPoint point in collection)
                point.PropertyChanged -= OnPointPropertyChanged;
        }

        private void OnPointCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    if (item is ShapeDrawPoint point)
                        point.PropertyChanged -= OnPointPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (object newItem in e.NewItems)
                {
                    if (newItem is ShapeDrawPoint point)
                        point.PropertyChanged += OnPointPropertyChanged;
                }
            }

            RegisterCollectionItemPropertyChanged(e.NewItems as AsyncObservableCollection<ShapeDrawPoint>);

            UnRegisterCollectionItemPropertyChanged(e.OldItems as AsyncObservableCollection<ShapeDrawPoint>);

            SetPathData();
        }

        private void OnPointPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "X" || e.PropertyName == "Y")
                SetPathData();
        }


        public RelayCommand CreatePointCommand => new RelayCommand(CreatePoint, HasPoints);
        private void CreatePoint()
        {
            Point mousePos = Mouse.GetPosition(path);

            ShapeDrawPoint newPoint = new ShapeDrawPoint();
            newPoint.X = ShapeDrawPoint.ConvertPointFromPixelSpace((float)mousePos.X);
            newPoint.Y = ShapeDrawPoint.ConvertPointFromPixelSpace((float)mousePos.Y);
            newPoint.PropertyChanged += OnPointPropertyChanged;

            int insertIdx = SelectedPoint != null ? Points.IndexOf(SelectedPoint.Point) + 1 : Points.Count;

            UndoManager.Instance.AddUndo(new UndoableListInsert<ShapeDrawPoint>(Points, insertIdx, newPoint, "Shape Draw -> Add Point"));
            Points.Insert(insertIdx, newPoint);

            SelectedPoint.Point = newPoint;
            NotifyPropertyChanged(nameof(SelectedPoint));
        }

        public RelayCommand DeletePointCommand => new RelayCommand(DeletePoint, CanDeletePoint);
        private void DeletePoint()
        {
            UndoManager.Instance.AddUndo(new UndoableListRemove<ShapeDrawPoint>(Points, SelectedPoint.Point, "Shape Draw -> Delete Point"));
            Points.Remove(SelectedPoint.Point);
        }

        public RelayCommand ReducePointsCommand => new RelayCommand(ReducePoints, CanReducePoints);
        private void ReducePoints()
        {
            var undos = Xv2CoreLib.EMP_NEW.ShapeDraw.ReducePoints(Points);
            UndoManager.Instance.AddCompositeUndo(undos, "Shape Draw -> Reduce Points");
        }

        private bool CanReducePoints()
        {
            return Points?.Count > 4 && SelectedPoint != null;
        }

        private bool HasPoints()
        {
            return Points != null && SelectedPoint != null;
        }

        private bool CanDeletePoint()
        {
            if (!IsPointSelected()) return false;
            return Points?.Count > 2; //Must always be 2 points
        }

        private bool IsPointSelected()
        {
            return SelectedPoint != null;
        }

    }

    public class ShapePointRef : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ShapeDrawPoint _point = null;

        public ShapeDrawPoint Point
        {
            get => _point;
            set
            {
                if(_point != value)
                {
                    _point = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Point)));
                }
            }
        }
    }

}
