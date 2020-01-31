using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UsefulThings.WPF
{
    public static class Misc
    {
        /// <summary>
        /// Finds visual child of given element. Optionally matches name of FrameWorkElement.
        /// </summary>
        /// <typeparam name="T">Visual Container Object to search within.</typeparam>
        /// <param name="obj">Object type to search for. e.g. TextBox, Label, etc</param>
        /// <param name="itemName">Name of FrameWorkElement in XAML.</param>
        /// <returns>FrameWorkElement</returns>
        public static T FindVisualChild<T>(DependencyObject obj, string itemName = null) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T && (itemName != null ? ((T)child).Name == itemName : true))
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child, itemName);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }


        /// <summary>
        /// Draws line graph on a canvas.
        /// Adaptive, but only draws dataline.
        /// </summary>
        /// <param name="canvas">Canvas to draw on.</param>
        /// <param name="values">Y values.</param>
        public static void DrawGraph(Canvas canvas, Queue<double> values)
        {
            // Build adaptive points list
            double xSize = canvas.ActualWidth;
            double ySize = canvas.ActualHeight;

            // Draw points
            int numPoints = values.Count;
            double maxValue = values.Max();

            for (int i = 0; i < numPoints; i += 2)
            {
                Line line = new Line();
                line.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
                line.StrokeThickness = 0.5;

                line.X1 = (i / numPoints) * xSize;
                line.X2 = ((i + 1) / numPoints ) * xSize;
                line.Y1 = (values.ElementAt(i) / maxValue) * ySize;
                line.Y2 = (values.ElementAt(i + 1) / maxValue) * ySize;
                
                canvas.Children.Add(line);
            }
        }
    }
}
