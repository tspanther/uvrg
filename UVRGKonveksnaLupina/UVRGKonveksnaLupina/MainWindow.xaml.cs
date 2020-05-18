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

        private void setTestCases()
        {
            _testCases = new List<List<Vector>>();
            _testCases.Add(
                new List<Vector>()
                {
                    new Vector(200, 400),
                    new Vector(400, 200),
                    new Vector(600, 400),
                    new Vector(400, 600),
                    new Vector(400, 500),
                    new Vector(400, 300),
                    new Vector(250, 300),
                    new Vector(250, 320),
                    new Vector(550, 300),
                    new Vector(550, 320),
                    new Vector(550, 500),
                    new Vector(550, 480),
                    new Vector(250, 500),
                    new Vector(250, 480)
                }
                );
            _testCases.Add(
                new List<Vector>()
                {
                    new Vector(100, 400),
                    new Vector(250, 500),
                    new Vector(200, 600),
                    new Vector(300, 600),
                    new Vector(400, 700),
                    new Vector(500, 600),
                    new Vector(600, 600),
                    new Vector(570, 500),
                    new Vector(700, 400),
                    new Vector(570, 300),
                    new Vector(600, 200),
                    new Vector(550, 200),
                    new Vector(450, 150),
                    new Vector(400, 100),
                    new Vector(300, 220),
                    new Vector(200, 200),
                    new Vector(250, 300)
                }
            );
        }

        private const double eps = 0.000001;
        private const int standardnaDeviacija = 70;

        private List<Vector> _tocke;
        private List<Vector> _resitev;
        private List<List<Vector>> _testCases;

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
                    setTestCases();
                    _tocke = _testCases[Porazdelitev.SelectedIndex - 2];
                    PrikaziGeneriraneTocke();
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
                    ms = Jarvis();
                    break;
                case 1:
                    ms = Graham();
                    break;
                case 2:
                    ms = QuickHull();
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
            for (int i = 0; i < n; i++)
            {
                _tocke.Add(new Vector(rnd.Next(800), rnd.Next(800)));
            }
        }

        private void GenerirajTockeNormalno(int n)
        {
            _tocke = new List<Vector>(n);
            for (int i = 0; i < n; i++)
            {
                _tocke.Add(new Vector(NaslednjeNormalno(), NaslednjeNormalno()));
            }
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

        private long Jarvis()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            _resitev = new List<Vector>();

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
            while (true)
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
                if (izbran == E) break;
                _tocke.Remove(izbran);
                _resitev.Add(izbran);
                trenutna = izbran;
            }

            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private long Graham()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            _resitev = new List<Vector>();

            Vector avg = new Vector((_tocke[0].x + _tocke[1].x + _tocke[2].x) / 3, (_tocke[0].y + _tocke[1].y + _tocke[2].y) / 3);
            Vector E = _tocke[0];

            _tocke.ForEach(t => { if (t.y < E.y) E = t; });
            _tocke.Sort((a, b) => 
            {
                if (Vector.Compare(a, b, avg)) return 1;
                return -1;
            });
            
            int Pi1, Pi2, Pi3;
            Pi1 = _tocke.IndexOf(E);
            Pi2 = (Pi1 + 1) % _tocke.Count;
            Pi3 = (Pi2 + 1) % _tocke.Count;
            _resitev.Add(_tocke[Pi1]);
            _resitev.Add(_tocke[Pi2]);
            while (true)
            {
                double U = (_tocke[Pi2].x - _tocke[Pi1].x) * (_tocke[Pi3].y - _tocke[Pi1].y) - (_tocke[Pi3].x - _tocke[Pi1].x) * (_tocke[Pi2].y - _tocke[Pi1].y);
                if (U < 0)
                {
                    _resitev.Remove(_tocke[Pi2]);
                    _tocke.RemoveAt(Pi2);
                    if (Pi1 > Pi2) Pi1--; if (Pi3 > Pi2) Pi3--;
                    Pi2 = Pi1 % _tocke.Count; Pi1--; if (Pi1 == -1) Pi1 = _tocke.Count - 1;
                }
                else
                {
                    if (_tocke[Pi3] == E) break;
                    _resitev.Add(_tocke[Pi3]);
                    Pi1 = Pi2 % _tocke.Count; Pi2 = Pi3 % _tocke.Count; Pi3 = (Pi3 + 1) % _tocke.Count;
                }
            }
            
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private long QuickHull()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Vector avg = new Vector((_tocke[0].x + _tocke[1].x + _tocke[2].x) / 3, (_tocke[0].y + _tocke[1].y + _tocke[2].y) / 3); // za sortiranje resitve na koncu
            _resitev = new List<Vector>();

            Vector Ea = _tocke[0], Eb = _tocke[0];
            _tocke.ForEach(t =>
            {
                if (t.x < Ea.x) Ea = t;
                if (t.x > Eb.x) Eb = t;
            });
            Vector meja = new Vector(Ea, Eb);

            _resitev.Add(Ea); _resitev.Add(Eb);
            _tocke.Remove(Ea); _tocke.Remove(Eb);

            List<Vector> spodnja = new List<Vector>();
            List<Vector> zgornja = new List<Vector>();

            foreach (var p in _tocke)
            {
                double dEaEb = (p.x - Ea.x) * (Eb.y - Ea.y) - (p.y - Ea.y) * (Eb.x - Ea.x);
                if (dEaEb > 0)
                {
                    spodnja.Add(p);
                }
                else
                {
                    zgornja.Add(p);
                }
            }

            QH(Eb, Ea, spodnja);
            QH(Ea, Eb, zgornja);

            _resitev.Sort((a, b) =>
            {
                if (Vector.Compare(a, b, avg)) return 1;
                return -1;
            });

            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private void QH(Vector Ea, Vector Eb, List<Vector> mnozica)
        {
            if (mnozica.Count == 0) return;
            Vector meja = new Vector(Ea, Eb);

            double maxS = 0;
            Vector E = mnozica[0];
            foreach (var p in mnozica)
            {
                Vector tempP = new Vector(Ea, p);
                double S = Vector.ParalelogramS(meja, tempP);
                if (S > maxS)
                {
                    maxS = S;
                    E = p;
                }
            }
            mnozica.Remove(E);
            _resitev.Add(E);
            
            List<Vector> EEaToSolve = new List<Vector>();
            List<Vector> EbEToSolve = new List<Vector>();
            foreach (var p in mnozica)
            {
                double dEEa = (p.x - E.x) * (Ea.y - E.y) - (p.y - E.y) * (Ea.x - E.x);
                double dEbE = (p.x - Eb.x) * (E.y - Eb.y) - (p.y - Eb.y) * (E.x - Eb.x);
                if (dEEa > 0 || dEbE > 0)
                {
                    if (dEEa < 0) EbEToSolve.Add(p);
                    else EEaToSolve.Add(p);
                }
            }

            QH(Ea, E, EEaToSolve);
            QH(E, Eb, EbEToSolve);
        }

        #endregion

        #region UI

        private void ClearUI()
        {
            cvs.Children.Clear();
        }

        private void PrikaziGeneriraneTocke()
        {
            for (int i = 0; i < _tocke.Count && i < 100000; i++) // da lahko poganjam alg na več kot 100k (risanje pikic prepočasno)
            {
                NarisiPiko(_tocke[i]);
            }
        }

        private void PrikaziResitev()
        {
            for (int i = 0; i < _resitev.Count; i++)
            {
                NarisiCrto(_resitev[i], _resitev[(i + 1) % _resitev.Count]);
            }
        }

        private void NarisiCrto(Vector T1, Vector T2)
        {
            Line ln = new Line()
            {
                Stroke = Brushes.Red,
                X1 = T1.x,
                Y1 = T1.y,
                X2 = T2.x,
                Y2 = T2.y,
                StrokeThickness = 2
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
