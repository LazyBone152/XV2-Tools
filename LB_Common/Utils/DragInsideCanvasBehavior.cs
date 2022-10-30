using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LB_Common.Utils
{
    public class DragInsideCanvasBehavior
    {
        private const int MouseTimeDif = 20;

        #region DragInsideCanvas

        public static bool GetDragInsideCanvas(DependencyObject obj)
        {
            return (bool)obj.GetValue(DragInsideCanvasProperty);
        }

        public static void SetDragInsideCanvas(DependencyObject obj, bool value)
        {
            obj.SetValue(DragInsideCanvasProperty, value);
        }

        // Using a DependencyProperty as the backing store for DragInsideCanvasBehavior.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragInsideCanvasProperty = DependencyProperty.RegisterAttached("DragInsideCanvas", typeof(bool), typeof(DragInsideCanvasBehavior), new PropertyMetadata(false, OnPropertyChanged));

        #region RegistredElements

        private static Dictionary<FrameworkElement, MyHandlersData> _registredElements;


        private static Dictionary<FrameworkElement, MyHandlersData> RegistredElements
        {
            get { return _registredElements ?? (_registredElements = new Dictionary<FrameworkElement, MyHandlersData>()); }
            set { _registredElements = value; }
        }

        #endregion

        private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var frameworkElement = dependencyObject as FrameworkElement;
            if (frameworkElement == null)
                return;


            var canvas = ViewUtils.GetParent(frameworkElement, (t) => t is Canvas) as Canvas;
            if (canvas == null)
                return;
            var mousePosition = Mouse.GetPosition(canvas);
            int lastMouseMoveTime = Environment.TickCount;
            bool itemIsClicked = false;

            double lastMousePosX = -1;
            double lastMousePosY = -1;
            double x = 0;
            double y = 0;

            RoutedEventHandler mouseDown = RegistredElements.ContainsKey(frameworkElement) ? RegistredElements[frameworkElement].MouseDown : null;
            RoutedEventHandler mouseUp = RegistredElements.ContainsKey(frameworkElement) ? RegistredElements[frameworkElement].MouseUp : null;
            RoutedEventHandler mouseMove = RegistredElements.ContainsKey(frameworkElement) ? RegistredElements[frameworkElement].MouseMove : null;

            if ((bool)dependencyPropertyChangedEventArgs.NewValue)
            {

                if (!RegistredElements.ContainsKey(frameworkElement))
                {
                    RegistredElements.Add(frameworkElement, new MyHandlersData());
                    RegistredElements[frameworkElement].MouseDown = (_, __) =>
                    {
                        itemIsClicked = true;

                        //always mouse down (start dragging), update the x,y 
                        x = (double)frameworkElement.GetValue(Canvas.LeftProperty);
                        y = (double)frameworkElement.GetValue(Canvas.TopProperty);

                        x = double.IsNaN(x) ? 0 : x;
                        y = double.IsNaN(y) ? 0 : y;

                        mousePosition = Mouse.GetPosition(canvas);
                        lastMousePosX = mousePosition.X;
                        lastMousePosY = mousePosition.Y;
                        Mouse.Capture(frameworkElement);
                    };

                    RegistredElements[frameworkElement].MouseUp = (_, __) =>
                    {
                        itemIsClicked = false;
                        Mouse.Capture(null);
                    };

                    RegistredElements[frameworkElement].MouseMove = (_, __) =>
                    {
                        if (itemIsClicked && ((Environment.TickCount - lastMouseMoveTime) > MouseTimeDif))
                        {
                            mousePosition = Mouse.GetPosition(canvas);
                            var containerHeight = (double)canvas.GetValue(FrameworkElement.ActualHeightProperty);
                            var containerWidth = (double)canvas.GetValue(FrameworkElement.ActualWidthProperty);
                            var mouseDiffX = mousePosition.X - lastMousePosX;
                            var mouseDiffY = mousePosition.Y - lastMousePosY;

                            //                                                      if (x + mouseDiffX > 0 && y + mouseDiffY > 0 && (containerWidth <= 0 || (x + mouseDiffX <= containerWidth) && (mousePosition.X <= containerWidth)) && (containerHeight <= 0 || (y + mouseDiffY <= containerHeight && mousePosition.Y <= containerHeight)))
                            //                                                      {
                            //                                                          x = x + mouseDiffX;
                            //                                                          y = y + mouseDiffY;
                            //                                                          frameworkElement.SetValue(Canvas.LeftProperty, x);
                            //                                                          frameworkElement.SetValue(Canvas.TopProperty, y);
                            //                                                      }

                            if (x + mouseDiffX >= 0 && mousePosition.X >= 0 && (containerWidth <= 0 || (x + mouseDiffX <= containerWidth) && (mousePosition.X <= containerWidth)))
                            {
                                x = x + mouseDiffX;
                                frameworkElement.SetValue(Canvas.LeftProperty, x);
                            }
                            if (y + mouseDiffY >= 0 && mousePosition.Y >= 0 && (containerHeight <= 0 || (y + mouseDiffY <= containerHeight && mousePosition.Y <= containerHeight)))
                            {
                                y = y + mouseDiffY;
                                frameworkElement.SetValue(Canvas.TopProperty, y);
                            }

                            lastMouseMoveTime = Environment.TickCount;
                            lastMousePosX = mousePosition.X;
                            lastMousePosY = mousePosition.Y;
                        }

                    };

                    frameworkElement.AddHandler(Mouse.MouseDownEvent, RegistredElements[frameworkElement].MouseDown, true);
                    frameworkElement.AddHandler(Mouse.MouseUpEvent, RegistredElements[frameworkElement].MouseUp, true);
                    frameworkElement.AddHandler(Mouse.MouseMoveEvent, RegistredElements[frameworkElement].MouseMove, false);
                }

            }
            else
            {
                if (mouseDown != null)
                    frameworkElement.RemoveHandler(Mouse.MouseDownEvent, mouseDown);
                if (mouseUp != null)
                    frameworkElement.RemoveHandler(Mouse.MouseUpEvent, mouseUp);
                if (mouseMove != null)
                    frameworkElement.RemoveHandler(Mouse.MouseMoveEvent, mouseMove);
                if (RegistredElements.ContainsKey(frameworkElement))
                    RegistredElements.Remove(frameworkElement);
            }
        }

        #endregion

        private class MyHandlersData
        {
            public RoutedEventHandler MouseDown { get; set; }
            public RoutedEventHandler MouseUp { get; set; }
            public RoutedEventHandler MouseMove { get; set; }
        }
    }


    internal static class ViewUtils
    {
        public static bool AnyParent(DependencyObject item, Func<DependencyObject, bool> condition)
        {
            if (item == null)
                return false;

            var logicalParent = LogicalTreeHelper.GetParent(item);
            var visualParent = VisualTreeHelper.GetParent(item);

            return condition(item) || AnyParent(visualParent, condition);
        }

        public static DependencyObject GetParent(DependencyObject item, Func<DependencyObject, bool> condition)
        {
            if (item == null)
                return null;

            var logicalParent = LogicalTreeHelper.GetParent(item);
            var visualParent = VisualTreeHelper.GetParent(item);

            return condition(item) ? item : GetParent(visualParent, condition);
        }

        public static DependencyObject GetVisualChild(DependencyObject item, Func<DependencyObject, bool> condition)
        {
            if (item == null)
                return null;

            var q = new Queue<DependencyObject>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(item); i++)
            {
                var t = VisualTreeHelper.GetChild(item, i);
                if (condition(t))
                    return t;
                q.Enqueue(t);
            }

            while (q.Count > 0)
            {
                var subchild = q.Dequeue();
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(subchild); i++)
                {
                    var t = VisualTreeHelper.GetChild(subchild, i);
                    if (condition(t))
                        return t;
                    q.Enqueue(t);
                }
            }
            return null;
        }

        public static DependencyObject GetLogicalChild(DependencyObject item, Func<DependencyObject, bool> condition)
        {
            if (item == null)
                return null;

            var q = new Queue<DependencyObject>();
            foreach (var w in LogicalTreeHelper.GetChildren(item))
            {
                var t = w as DependencyObject;
                if (condition(t))
                    return t;
                q.Enqueue(t);
            }

            while (q.Count > 0)
            {
                var subchild = q.Dequeue();
                foreach (var w in LogicalTreeHelper.GetChildren(subchild))
                {
                    var t = w as DependencyObject;
                    if (condition(t))
                        return t;
                    q.Enqueue(t);
                }
            }
            return null;
        }
    }

}
