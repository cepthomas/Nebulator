using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using PNUT;
using Nebulator.Common;
using Nebulator.Synth;


namespace Nebulator.Test
{
    public class SYNTH_Vis : TestSuite
    {
        public override void RunSuite()
        {
            Visualizer v = new Visualizer();

            List<Color> colors = new List<Color>() { Color.Firebrick, Color.CornflowerBlue, Color.MediumSeaGreen, Color.MediumOrchid, Color.DarkOrange, Color.DarkGoldenrod, Color.DarkSlateGray, Color.Khaki, Color.PaleVioletRed };
            // Unnamed sets from http://colorbrewer2.org qualitative:
            // COLORS_DARK: Color.FromArgb(27, 158, 119), Color.FromArgb(217, 95, 2), Color.FromArgb(117, 112, 179), Color.FromArgb(231, 41, 138), Color.FromArgb(102, 166, 30), Color.FromArgb(230, 171, 2), Color.FromArgb(166, 118, 29), Color.FromArgb(102, 102, 102) <summary>Color definitions.</summary>
            // COLORS_LIGHT: Color.FromArgb(141, 211, 199), Color.FromArgb(255, 255, 179), Color.FromArgb(190, 186, 218), Color.FromArgb(251, 128, 114), Color.FromArgb(128, 177, 211), Color.FromArgb(253, 180, 98), Color.FromArgb(179, 222, 105), Color.FromArgb(252, 205, 229)


            // Give some data.
            for (int i = 2; i < 7; i++)
            {
                Visualizer.Series ser = new Visualizer.Series();
                for(int j = 5; j < 100; j += 7)
                {
                    ser.Points.Add((i, j + i));
                }

                ser.Name = "opopp";
                ser.Color = colors[i];
                
                v.AllSeries.Add(ser);
            }

            v.XMin = 0;
            v.XMax = 10;
            v.YMin = 2;
            v.YMax = 110;
            v.DotSize = 4;
            v.LineSize = 1;
            v.ChartType = Visualizer.ChartTypes.ScatterLine;

            new System.Threading.Thread(() => v.ShowDialog()).Start();

            System.Threading.Thread.Sleep(1000);
        }
    }

    public class SYNTH_Basic : TestSuite
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
