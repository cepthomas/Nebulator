using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using PNUT;
//using Nebulator.Controls;
using Nebulator.Synth;


namespace Nebulator.Test
{
    public class SYNTH_Vis : TestSuite
    {
        public override void RunSuite()
        {
            Controls.Visualizer v = new Controls.Visualizer
            {
                XMin = 0,
                XMax = 10,
                YMin = 2,
                YMax = 110,
                DotSize = 4,
                LineSize = 1,
                ChartType = Controls.Visualizer.ChartTypes.ScatterLine,
                Dock = DockStyle.Fill
            };

            // Make some data.
            Random rand = new Random();
            for (int y = 5; y < 100; y+=7)
            {
                Controls.Visualizer.Series ser = new Controls.Visualizer.Series();
                for(int x = 1; x < 10; x++)
                {
                    double dr = rand.NextDouble() * 3;
                    ser.Points.Add((x, y - (float)dr));
                }
                ser.Name = $"Series {y}";
                v.AllSeries.Add(ser);
            }

            Form f = new Form()
            {
                Text = "Visualizer",
                Size = new Size(900, 600),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(20, 20),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };
            f.Controls.Add(v);

            new System.Threading.Thread(() => f.ShowDialog()).Start();

            //System.Threading.Thread.Sleep(1000);
        }
    }

    public class SYNTH_Basic : TestSuite //TODON1 debug
    {
        public override void RunSuite()
        {
            SynthOutput sout = new SynthOutput();
            bool ok = sout.Init("SYNTH:ASIO4ALL v2");

            UT_TRUE(ok);

            if (ok)
            {
                sout.SynthUGen = new Simple();
                sout.Start();

                double val = 0.3;
                while (val < 0.9)
                {
                    //synth.lpf.Freq = val;
                    System.Threading.Thread.Sleep(200);
                    val += 0.1; 
                }

                sout.Stop();
            }
        }

        public class Simple : UGen2
        {
            SinOsc osc1 = new SinOsc() { Freq = 400, Gain = 0.5 };
            SinOsc osc2 = new SinOsc() { Freq = 410, Gain = 0.5 };
            Mix mix1 = new Mix();
            ADSR adsr = new ADSR() { AttackTime = 0.5, DecayTime = 0.3, SustainLevel = 0.4, ReleaseTime = 0.8, Gain = 0.5 };
            Pan pan = new Pan();

            public Simple()
            {
                adsr.KeyOn();
            }

            public override Sample Next(double _)
            {
                double dd1 = osc1.Next(0);
                double dd2 = osc2.Next(0);
                double dd3 = mix1.Next(dd1, dd2);
                double dd4 = adsr.Next(dd3);
                //Sample dout = new Sample() { Left = dd1, Right = dd2 };

                double dd = adsr.Next(mix1.Next(osc1.Next(0), osc2.Next(0)));
                Sample dout = new Sample() { Left = dd, Right = dd };

                return dout;
            }

            public override void Note(double notenum, double ampl)
            {
                //adsrF.KeyOn();
                // Pass along to the noisemakers.
                //vcr1.Note(notenum, ampl);
            }
        }
    }

    public class SYNTH_Full : TestSuite
    {
        public override void RunSuite()
        {
            MySynth synth = new MySynth();

            SynthOutput sout = new SynthOutput();
            bool ok = sout.Init("SYNTH:ASIO4ALL v2");

            UT_TRUE(ok);

            if (ok)
            {
                sout.SynthUGen = synth;
                sout.Start();

                double val = 0.3;
                while (val < 0.9)
                {
                    synth.lpf.Freq = val;
                    System.Threading.Thread.Sleep(200);
                    val += 0.1; 
                }

                sout.Stop();
            }
        }

        // The overall synthesizer.
        public class MySynth : UGen2
        {
            public Voicer vcr1 = new Voicer(typeof(MyVoice), 5, 0.2);
            public LPF lpf = new LPF();
            ADSR adsrF = new ADSR() { AttackTime = 0.5, DecayTime = 0.3, SustainLevel = 0.4, ReleaseTime = 0.8, Gain = 0.5 };
            public Pan pan = new Pan();

            public override Sample Next(double _)
            {
                lpf.Freq = 200 + 500 * adsrF.Next(1);
                Sample dout = pan.Next(lpf.Next(vcr1.Next(0)));
                return dout;
            }

            public override void Note(double notenum, double ampl)
            {
                adsrF.KeyOn();
                // Pass along to the noisemakers.
                vcr1.Note(notenum, ampl);
            }
        }

        // A single voice in the generator.
        public class MyVoice : UGen
        {
            // 2 oscs slightly detuned and synced
            SinOsc osc1 = new SinOsc();
            SinOsc osc2 = new SinOsc();
            Mix mix1 = new Mix();
            ADSR adsr = new ADSR() { AttackTime = 0.5, DecayTime = 0.3, SustainLevel = 0.4, ReleaseTime = 0.8, Gain = 0.5 };

            public MyVoice()
            {
            }

            public override void Note(double notenum, double amplitude)
            {
                osc1.Note(notenum, amplitude);
                osc2.Note(notenum * 1.05, amplitude);
                adsr.KeyOn();
            }

            public override double Next(double _)
            {
                double dout = adsr.Next(mix1.Next(osc1.Next(0), osc2.Next(0)));
                return dout;
            }
        }
    }
}
