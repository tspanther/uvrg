using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace UVRGKonveksnaLupina
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private const double eps = 0.000001;
        private const int standardnaDeviacija = 70;

        private List<Vector> _tocke;
        private List<KeyValuePair<Vector, Vector>> _resitevTriangulacijaSpirala;
        private List<KeyValuePair<Vector, Vector>> _resitevTriangulacijaTrak;

        private Random rnd = new Random();
        private class Vector
        {
            public double x;
            public double y;

            public Vector(double _x, double _y)
            {
                x = _x; y = _y;
            }

            public Vector(Vector krajevni1, Vector krajevni2)
            {
                x = krajevni2.x - krajevni1.x;
                y = krajevni2.y - krajevni1.y;
            }

            public static double Area(Vector p0, Vector p1, Vector p2)
            {
                return 0.5 * (-p1.y * p2.x + p0.y * (-p1.x + p2.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y);
            }

            public bool IsInsideTriangle(Vector p0, Vector p1, Vector p2)
            {
                double S = Vector.Area(p0, p1, p2);
                double s = 1 / (2 * S) * (p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * this.x + (p0.x - p2.x) * this.y);
                double t = 1 / (2 * S) * (p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * this.x + (p1.x - p0.x) * this.y);
                return s > 0 && s < 1 && t > 0 && t < 1 && (s + t) > 0 && (s + t) < 1;
            }

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

            public static double ParalelogramS(Vector v1, Vector v2)
            {
                return Math.Abs((v1.x * v2.y) - (v1.y * v2.x));
            }

            public bool Same(Vector v)
            {
                return Math.Abs(v.x - x) < eps && Math.Abs(v.y - y) < eps;
            }

            public double Angle(Vector v)
            {
                return Math.Acos(ScalarProduct(v) / (Length() * v.Length()));
            }

            public static bool Compare(Vector a, Vector b, Vector center)
            {
                var bminsc = new Vector(b, center);
                var aminusc = new Vector(a, center);

                double bb = Math.Atan2(bminsc.y, bminsc.x);
                double aa = Math.Atan2(aminusc.y, aminusc.x);

                return aa > bb;
            }

            public static bool Intersecting(KeyValuePair<Vector, Vector> a, KeyValuePair<Vector, Vector> b)
            {
                var T1 = a.Key; var T2 = a.Value; var T3 = b.Key; var T4 = b.Value;
                if (T1.Same(T3) || T1.Same(T4) || T2.Same(T3) || T2.Same(T4)) // touching
                {
                    return false;
                }
                // calc
                double D = (T2.x - T1.x) * (T4.y - T3.y) - (T4.x - T3.x) * (T2.y - T1.y);
                double A = (T4.x - T3.x) * (T1.y - T3.y) - (T1.x - T3.x) * (T4.y - T3.y);
                double B = (T2.x - T1.x) * (T1.y - T3.y) - (T1.x - T3.x) * (T2.y - T1.y);

                if (Math.Abs(D) < eps) // parallel
                {
                    return false;
                }

                double Ua = A / D;
                double Ub = B / D;

                bool inter = Ua >= 0 && Ua <= 1 && Ub >= 0 && Ub <= 1;
                return inter;
            }
        }

        #region Events

        private void Generiraj_Click(object sender, RoutedEventArgs e)
        {
            ClearUI();

            int N = 0;
            if (!int.TryParse(stTock.Text, out N))
            {
                messageBlock.Text += "Število točk ni številka.\n";
                return;
            }

            switch (Porazdelitev.SelectedIndex)
            {
                case -1:
                    messageBlock.Text += "Porazdelitev točk ni izbrana.\n";
                    return;
                    break;
                case 0:
                    GenerirajTockeNormalno(N);
                    PrikaziGeneriraneTocke();
                    break;
                case 1:
                    GenerirajTockeEnakomerno(N);
                    PrikaziGeneriraneTocke();
                    break;
                default:
                    break;
            }
        }

        private void Izracunaj_Click(object sender, RoutedEventArgs e)
        {
            if (_tocke == null || _tocke.Count < 3)
            {
                messageBlock.Text += "Najprej generiraj tocke.\n";
                return;
            }

            long ms = 0;
            switch (Algoritem.SelectedIndex)
            {
                case -1:
                    messageBlock.Text += "Algoritem ni izbran.\n";
                    return;
                    break;
                case 0:
                    ms = MUT();
                    break;
                case 1:
                    ms = HamiltonovaTriangulacija();
                    break;
            }
            messageBlock.Text += "Time elapsed: " + ms + "ms\n";

            PrikaziResitev();
        }

        #endregion

        #region Generiranje Točk

        private void GenerirajTockeEnakomerno(int n)
        {
            _tocke = new List<Vector>(n);
            var povezave = new List<KeyValuePair<Vector, Vector>>(); // za preverjanje kolinearnosti
            for (int i = 0; i < n; i++)
            {
                var p = new Vector(rnd.Next(800), rnd.Next(800));
                bool dodaj = true;
                foreach (var pov in povezave)
                {
                    if (/*Collinear(pov, p)*/false)
                    {
                        i--;
                        dodaj = false;
                        break;
                    }
                }
                if (dodaj)
                {
                    _tocke.Add(p);
                    foreach (var t in _tocke)
                    {
                        povezave.Add(new KeyValuePair<Vector, Vector>(t, p));
                    }
                }
            }
        }

        private void GenerirajTockeNormalno(int n)
        {
            _tocke = new List<Vector>(n);
            var povezave = new List<KeyValuePair<Vector, Vector>>(); // za preverjanje kolinearnosti
            for (int i = 0; i < n; i++)
            {
                var p = new Vector(NaslednjeNormalno(), NaslednjeNormalno());
                bool dodaj = true;
                foreach (var pov in povezave)
                {
                    if (/*Collinear(pov, p)*/false)
                    {
                        i--;
                        dodaj = false;
                        break;
                    }
                }
                if (dodaj)
                {
                    _tocke.Add(p);
                    foreach (var t in _tocke)
                    {
                        povezave.Add(new KeyValuePair<Vector, Vector>(t, p));
                    }
                }
            }
        }

        private bool Collinear(KeyValuePair<Vector, Vector> pov, Vector p)
        {
            var AB = new Vector(pov.Key, p).Length();
            var BC = new Vector(pov.Value, p).Length();
            var AC = new Vector(pov.Value, pov.Key).Length();
            return Math.Abs(AC - (AB + BC)) < 1; // tocke naj bodo v trikotniku, kjer je najdaljsa daljica vsaj za 1 krajsa od vsote krajsih daljic (trikotnik ima vsaj minimalno ploscino)
        }

        /// <summary>
        /// Box-Mullerjeva transformacija. Iz parov naključno generiranih števil med 0 in 1 dobimo števila, ki so normalno razporejena v intervalu 0 do 1.
        /// Ideja s spletne strani http://mathworld.wolfram.com/Box-MullerTransformation.html
        /// </summary>
        /// <param name="n"></param>
        private double NaslednjeNormalno()
        {
            double u1 = 1.0 - rnd.NextDouble(); // uniform(0,1] random doubles
            double u2 = 1.0 - rnd.NextDouble();
            double enotskaNormalnaRazporeditev = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
            double randNormal = cvs.ActualHeight / 2 + standardnaDeviacija * (0.5 - enotskaNormalnaRazporeditev);

            return randNormal;
        }

        #endregion

        #region Algoritmi

        private long MUT()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var povezave = new List<KeyValuePair<Vector, Vector>>();
            for (int i = 0; i < _tocke.Count; i++)
            {
                for (int j = i + 1; j < _tocke.Count; j++)
                {
                    povezave.Add(new KeyValuePair<Vector, Vector>(_tocke[i], _tocke[j]));
                }
            }
            povezave.Sort((a, b) => { return (new Vector(a.Key, a.Value).Length() - new Vector(b.Key, b.Value).Length() > 0) ? 1 : -1; });

            _resitevTriangulacijaSpirala = new List<KeyValuePair<Vector, Vector>>();
            _resitevTriangulacijaTrak = new List<KeyValuePair<Vector, Vector>>();
            foreach (var e in povezave)
            {
                bool dodaj = true;
                foreach(var res in _resitevTriangulacijaSpirala)
                {
                    if (Vector.Intersecting(res, e))
                    {
                        dodaj = false;
                        break;
                    }
                }
                if (dodaj)
                {
                    _resitevTriangulacijaSpirala.Add(e);
                }
            }

            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private long HamiltonovaTriangulacija()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            _resitevTriangulacijaSpirala = new List<KeyValuePair<Vector, Vector>>();
            _resitevTriangulacijaTrak = new List<KeyValuePair<Vector, Vector>>();

            List<Vector> spiralaTocke = JarviSpiral();
            int indZacetkaNotranje = (int)spiralaTocke[spiralaTocke.Count - 1].x;
            spiralaTocke.RemoveAt(spiralaTocke.Count - 1);
            var spiralaDaljice = new List<KeyValuePair<Vector, Vector>>();
            for (int i = 0; i < spiralaTocke.Count - 1; i++)
            {
                spiralaDaljice.Add(new KeyValuePair<Vector, Vector>(spiralaTocke[i], spiralaTocke[i + 1]));
            }
            _resitevTriangulacijaSpirala.AddRange(spiralaDaljice);

            int c = 0;
            int b = indZacetkaNotranje;
            int a = b - 1;
            int temp;

            while((a != c) && (c != b))
            {
                if (c > spiralaTocke.Count -1)
                {
                    c = spiralaTocke.Count - 1;
                }
                if (b > spiralaTocke.Count - 1)
                {
                    b = spiralaTocke.Count - 1;
                }
                if (b > spiralaTocke.Count - 1)
                {
                    b = spiralaTocke.Count - 1;
                }

                bool vsebujeTockoSpirale = false;
                for (int i = 0; i < spiralaTocke.Count; i++)
                {
                    if (i != a && i != b && i != c)
                    {
                        if (spiralaTocke[i].IsInsideTriangle(spiralaTocke[a], spiralaTocke[b], spiralaTocke[c]))
                        {
                            vsebujeTockoSpirale = true;
                            break;
                        }
                    }
                }
                if(vsebujeTockoSpirale)
                {
                    temp = a;
                    a = b;
                    c = b + 1;
                    b = temp;
                    continue;
                }

                bool sekaDaljicoSpirale = false;
                List<KeyValuePair<Vector, Vector>> daljici = new List<KeyValuePair<Vector, Vector>>();
                if (Math.Abs(a-b) !=1)
                {
                    daljici.Add(new KeyValuePair<Vector, Vector>(spiralaTocke[a], spiralaTocke[b]));
                }
                if (Math.Abs(c - a) != 1)
                {
                    daljici.Add(new KeyValuePair<Vector, Vector>(spiralaTocke[c], spiralaTocke[a]));
                }
                if (Math.Abs(c - b) != 1)
                {
                    daljici.Add(new KeyValuePair<Vector, Vector>(spiralaTocke[c], spiralaTocke[b]));
                }
                foreach(var d in daljici)
                {
                    if (!sekaDaljicoSpirale)
                    {
                        foreach (var ds in spiralaDaljice)
                        {
                            if (Vector.Intersecting(d, ds))
                            {
                                sekaDaljicoSpirale = true;
                                break;
                            }
                        }
                    }
                }
                if (sekaDaljicoSpirale)
                {
                    temp = a;
                    a = b;
                    c = b + 1;
                    b = temp;
                    continue;
                }

                // uspeh
                _resitevTriangulacijaTrak.AddRange(daljici);
                temp = c;
                c = b + 1;
                a = b;
                b = temp;
            }

            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private List<Vector> JarviSpiral()
        {
            var _resitev = new List<Vector>();

            // iskanje ekstrema
            Vector E = _tocke[0];
            for (int i = 0; i < _tocke.Count; i++)
            {
                if (_tocke[i].y < E.y) E = _tocke[i];
            }
            _resitev.Add(E);

            // prva naslednja
            double minKot = Math.PI + 1;
            double dMinKot = double.MaxValue;
            Vector izbran = null;
            Vector os = new Vector(100, 0); // zacnemo z vodoravno osjo
            foreach (var t in _tocke)
            {
                if (t == E) continue; // X\{E}
                Vector primerjaj = new Vector(E, t);
                double kot = os.Angle(primerjaj);
                if (kot < minKot || Math.Abs(kot - minKot) < eps)
                {
                    double d = primerjaj.Length();
                    if (Math.Abs(kot - minKot) < eps && d > dMinKot)
                    {
                        continue;
                    }
                    izbran = t;
                    dMinKot = d;
                    minKot = kot;
                }
            }
            _tocke.Remove(izbran);
            _resitev.Add(izbran);
            Vector trenutna = izbran;

            // ostale
            int ind = -1;
            while (_tocke.Count > 0)
            {
                minKot = Math.PI + 1;
                dMinKot = double.MaxValue;
                os = new Vector(_resitev[_resitev.Count - 2], _resitev[_resitev.Count - 1]);
                foreach (var t in _tocke)
                {
                    Vector primerjaj = new Vector(trenutna, t);
                    double kot = os.Angle(primerjaj);
                    if (kot < minKot || Math.Abs(kot - minKot) < eps)
                    {
                        double d = primerjaj.Length();
                        if (Math.Abs(kot - minKot) < eps && d > dMinKot)
                        {
                            continue;
                        }
                        izbran = t;
                        dMinKot = d;
                        minKot = kot;
                    }
                }
                if (izbran == E && ind ==-1)
                {
                    ind = _resitev.Count;
                }
                else
                {
                    _resitev.Add(izbran);
                    trenutna = izbran;
                }
                _tocke.Remove(izbran);
            }
            _resitev.Add(new Vector(ind, -1));
            return _resitev;
        }

        #endregion

        #region UI

        private void ClearUI()
        {
            cvs.Children.Clear();
        }

        private void PrikaziGeneriraneTocke()
        {
            foreach (var i in _tocke)
            {
                NarisiPiko(i);
            }
        }

        private void PrikaziResitev()
        {
            foreach(var r in _resitevTriangulacijaSpirala)
            {
                NarisiCrto(r.Key, r.Value, Brushes.Red, 2);
            }
            foreach(var r in _resitevTriangulacijaTrak)
            {
                NarisiCrto(r.Key, r.Value, Brushes.Green, 1);
            }
        }

        private void NarisiCrto(Vector T1, Vector T2, SolidColorBrush scb, int thiccness)
        {
            Line ln = new Line()
            {
                Stroke = scb,
                X1 = T1.x,
                Y1 = T1.y,
                X2 = T2.x,
                Y2 = T2.y,
                StrokeThickness = thiccness
            };
            cvs.Children.Add(ln);
        }

        private void NarisiPiko(Vector d)
        {
            int dotSize = 3;

            Ellipse currentDot = new Ellipse();
            currentDot.Stroke = new SolidColorBrush(Colors.Black);
            currentDot.StrokeThickness = 1.5;
            Canvas.SetZIndex(currentDot, 1);
            currentDot.Height = dotSize;
            currentDot.Width = dotSize;
            currentDot.Fill = new SolidColorBrush(Colors.Black);
            currentDot.Margin = new Thickness(d.x - dotSize / 2, d.y - dotSize / 2, 0, 0); // Sets the position.
            cvs.Children.Add(currentDot);
        }

        #endregion
    }
}
