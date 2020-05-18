using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UVRGApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double eps = 0.000001;

        public MainWindow()
        {
            InitializeComponent();
        }

        private class Vector
        {
            public Vector(double _x, double _y)
            {
                x = _x; y = _y;
            }
            public double x;
            public double y;

            public double Length()
            {
                return Math.Sqrt(x * x + y * y);
            }

            public void Normalize()
            {
                double len = Length();
                x *= 1 / len; y *= 1 / len;
            }   

            public double ScalarProduct(Vector v)
            {
                return x * v.x + y * v.y;
            }

            public bool Same(Vector v)
            {
                return Math.Abs(v.x - x) < eps && Math.Abs(v.y - y) < eps;
            }
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            cvs.Children.Clear();

            IEnumerable<Panel> panels = inputPannel.Children.OfType<Panel>();
            List<TextBox> controls = new List<TextBox>();
            foreach (var panel in panels)
            {
                controls.AddRange(panel.Children.OfType<TextBox>());
            }

            int[] coords = new int[8];
            int i = 0;
            while (i < 8 && controls[i] != null && int.TryParse(controls[i].Text, out coords[i]))
            {
                i++;
            }

            switch (i/2)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    ManageOneLine(new Vector(coords[0], coords[1]), new Vector(coords[2], coords[3]));
                    break;
                case 3:
                    ManagePointProjection(new Vector(coords[0], coords[1]), new Vector(coords[2], coords[3]), new Vector(coords[4], coords[5]));
                    break;
                case 4:
                    ManageTwoLines(new Vector(coords[0], coords[1]), new Vector(coords[2], coords[3]), new Vector(coords[4], coords[5]), new Vector(coords[6], coords[7]));
                    break;
            }
        }

        private void ManageTwoLines(Vector T1, Vector T2, Vector T3, Vector T4)
        {
            // draw
            DrawLine(T1, T2);
            DrawLine(T3, T4);

            // calc
            double D = (T2.x - T1.x) * (T4.y - T3.y) - (T4.x - T3.x) * (T2.y - T1.y);
            double A = (T4.x - T3.x) * (T1.y - T3.y) - (T1.x - T3.x) * (T4.y - T3.y);
            double B = (T2.x - T1.x) * (T1.y - T3.y) - (T1.x - T3.x) * (T2.y - T1.y);

            if (Math.Abs(D) < eps)
            {
                DisplayText("The lines are parallel.\n");
                if (Math.Abs(A) < eps && Math.Abs(B) < eps)
                {
                    List<KeyValuePair<Vector, string>> pts = new List<KeyValuePair<Vector, string>>()
                    {
                        new KeyValuePair<Vector, string>(T1, "T1"),
                        new KeyValuePair<Vector, string>(T2, "T2"),
                        new KeyValuePair<Vector, string>(T3, "T3"),
                        new KeyValuePair<Vector, string>(T4, "T4")
                    };
                    pts.Sort((v1, v2) => v1.Key.x.CompareTo(v2.Key.x));

                    DrawLine(pts[1].Key, pts[2].Key, true);
                    DisplayText(string.Format("The lines match. The matching area is the line from {0}({1},{2}) to {3}({4},{5})",
                        pts[1].Value, pts[1].Key.x, pts[1].Key.y, pts[2].Value, pts[2].Key.x, pts[2].Key.y));
                }
                return;
            }

            double Ua = A / D;
            double Ub = B / D;

            if (Ua >= 0 && Ua <= 1 && Ub >= 0 && Ub <= 1)
            {
                double Xp = T1.x + Ua * (T2.x - T1.x);
                double Yp = T1.y + Ua * (T2.y - T1.y);

                List<KeyValuePair<Vector, string>> pts = new List<KeyValuePair<Vector, string>>()
                {
                    new KeyValuePair<Vector, string>(T1, "T1"),
                    new KeyValuePair<Vector, string>(T2, "T2"),
                    new KeyValuePair<Vector, string>(T3, "T3"),
                    new KeyValuePair<Vector, string>(T4, "T4")
                };
                if (!pts.Any(pt => pt.Key.Same(new Vector(Xp, Yp))))
                {
                    DrawDot(new Vector(Xp, Yp));
                    DisplayText(string.Format("The lines intersect in point Ti({0},{1}).", Xp, Yp));
                }
                else
                {
                    string dot = pts.First(pt => pt.Key.Same(new Vector(Xp, Yp))).Value;
                    DrawDot(new Vector(Xp, Yp));
                    DisplayText(string.Format("The lines are 'touching' in point {0}({1},{2}).", dot, Xp, Yp));
                }
            }
            else
            {
                DisplayText("The lines do not intersect.");
            }
        }

        private void ManagePointProjection(Vector T1, Vector T2, Vector T3)
        {
            // draw
            DrawLine(T2, T3);
            DrawDot(T1);

            // calc
            Vector v1 = new Vector(T3.x - T2.x, T3.y - T2.y);
            Vector v2 = new Vector(T1.x - T2.x, T1.y - T2.y);

            v1.Normalize();
            double len2 = v2.Length();
            //v2.Normalize();
            double sp = v1.ScalarProduct(v2);

            if (sp < 0 || sp >= len2)
            {
                if (EuclideanDistance(T1, T2) > EuclideanDistance(T1, T3))
                {
                    DrawLine(T1, T3);
                    DisplayText(String.Format("The rectangle projection of the point is not on the line. Closer edge of the line is T3. Distance to T3: {0}.", EuclideanDistance(T1, T3)));
                }
                else
                {
                    DrawLine(T1, T2);
                    DisplayText(String.Format("The rectangle projection of the point is not on the line. Closer edge of the line is T2. Distance to T2: {0}.", EuclideanDistance(T1, T2)));
                }
            } else
            {
                Vector Tp = new Vector(T2.x + v1.x * sp * len2, T2.y + v1.y * sp * len2);

                DrawLine(T1, Tp);
                DrawDot(Tp);
                DisplayText(String.Format("The rectangle projection of the point is in point Tp({0},{1}). Distance to the line: {2}", Tp.x, Tp.y, EuclideanDistance(Tp, T1)));
            }
        }

        private void ManageOneLine(Vector T1, Vector T2)
        {
            // draw
            DrawLine(T1, T2);

            // calc
            double distance = EuclideanDistance(T1, T2);
            DisplayText(String.Format("The Euclidean distance between points is {0}", distance));
        }

        private double EuclideanDistance(Vector T1, Vector T2)
        {
            return Math.Sqrt(Math.Pow(T1.x - T2.x, 2) + Math.Pow(T1.y - T2.y, 2));
        }

        private void DisplayText(string msg)
        {
            messageBlock.Text = msg;
        }

        private void DrawLine(Vector T1, Vector T2, bool red = false)
        {
            Line ln = new Line()
            {
                Stroke = red ? Brushes.Red : Brushes.Black,
                X1 = T1.x,
                Y1 = T1.y,
                X2 = T2.x,
                Y2 = T2.y,
                StrokeThickness = 3
            };
            cvs.Children.Add(ln);
        }

        /// <summary>
        /// Draws a poor man's dot by drawing a small elipse. C# wpf canvas doesn't support drawing dots (LOL!)
        /// </summary>
        /// <param name="d"></param>
        private void DrawDot(Vector d)
        {
            int dotSize = 10;

            Ellipse currentDot = new Ellipse();
            currentDot.Stroke = new SolidColorBrush(Colors.Cyan);
            currentDot.StrokeThickness = 2;
            Canvas.SetZIndex(currentDot, 2);
            currentDot.Height = dotSize;
            currentDot.Width = dotSize;
            currentDot.Fill = new SolidColorBrush(Colors.Cyan);
            currentDot.Margin = new Thickness(d.x - 5, d.y - 5, 0, 0); // Sets the position.
            cvs.Children.Add(currentDot);
        }
    }
}
