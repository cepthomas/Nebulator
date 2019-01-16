using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using PNUT;
using Nebulator.Synth;
using Nebulator.Visualizer;


namespace Nebulator.Test
{
    public class SYNTH_Vis : TestSuite
    {
        public override void RunSuite()
        {
            VisualizerForm v = new VisualizerForm();

            int which = 2;

            switch(which)
            {
                case 1:
                    #region ADSR
                    {
                        DataSeries seradsr = new DataSeries() { Name = "ADSR" };
                        ADSR adsr = new ADSR() { AttackTime = 0.5, DecayTime = 0.3, SustainLevel = 0.6, ReleaseTime = 0.4, Gain = 0.5 };
                        for (int i = 0; i < 100000; i++)
                        {
                            double dd = adsr.Next(1);
                            if (i % 1000 == 0)
                            {
                                seradsr.AddPoint(i, dd);
                            }

                            if (i == 5000)
                            {
                                adsr.Key(true);
                            }

                            if (i == 60000)
                            {
                                adsr.Key(false);
                            }
                        }
                        v.AllSeries.Add(seradsr);
                    }
                    break;
                    #endregion

                case 2:
                    #region Osc
                    {
                        SinOsc osc1 = new SinOsc() { Freq = 400, Gain = 0.5 };
                        SinOsc osc2 = new SinOsc() { Freq = 500, Gain = 0.5 };
                        Mix mix1 = new Mix() { Gain = 0.5 };

                        DataSeries serosc1 = new DataSeries() { Name = "OSC1" };
                        DataSeries serosc2 = new DataSeries() { Name = "OSC2" };
                        DataSeries sermix = new DataSeries() { Name = "MIX" };

                        for (int i = 0; i < 500; i++) // = 11.3 msec  400Hz = 2.5 msec 
                        {
                            double d1 = osc1.Next(1);
                            double d2 = osc2.Next(1);
                            double dm = mix1.Next(d1, d2);

                            serosc1.AddPoint(i, d1);
                            serosc2.AddPoint(i, d2);
                            sermix.AddPoint(i, dm);
                        }

                        v.AllSeries.Add(serosc1);
                        v.AllSeries.Add(serosc2);
                        v.AllSeries.Add(sermix);
                    }
                    break;
                    #endregion

                case 3:
                    #region Osc + ADSR
                    {
                        SinOsc osc = new SinOsc() { Freq = 400, Gain = 0.5 };
                        ADSR adsr = new ADSR() { AttackTime = 0.5, DecayTime = 0.3, SustainLevel = 0.6, ReleaseTime = 0.4, Gain = 0.5 };

                        DataSeries serosc = new DataSeries() { Name = "OSC" };
                        DataSeries seradsr = new DataSeries() { Name = "ADSR" };

                        for (int i = 0; i < 100000; i++)
                        {
                            double d1 = osc.Next(1);
                            double da = adsr.Next(d1);

                            if (i % 100 == 0)
                            {
                                serosc.AddPoint(i, d1);
                                seradsr.AddPoint(i, da);
                            }

                            if (i == 5000)
                            {
                                adsr.Key(true);
                            }

                            if (i == 60000)
                            {
                                adsr.Key(false);
                            }
                        }

                        //v.AllSeries.Add(serosc);
                        v.AllSeries.Add(seradsr);
                        v.ChartType = ChartType.Line;
                    }
                    break;
                    #endregion
            }

            new System.Threading.Thread(() => v.ShowDialog()).Start();
        }
    }

    public class SYNTH_Basic : TestSuite
    {
        public override void RunSuite()
        {
            VisualizerForm v = new VisualizerForm();

            SynthOutput sout = new SynthOutput();
            bool ok = sout.Init("SYNTH:ASIO4ALL v2");

            UT_TRUE(ok);

            if (ok)
            {
                MySynth ssynth = new MySynth() { Gain = 0.8 };
                sout.Synth = ssynth;
                ssynth.pan.Location = -1;

                sout.Start();

                for (int i = 0; i < 60; i++)
                {
                    //Console.WriteLine($"+++{i}");

                    //ssynth.lpf.Freq += 50;
                    //ssynth.osc1.Width += .01;
                    //ssynth.osc1.Freq += 1;
                    ssynth.pan.Location += .02;

                    if (i == 0)
                    {
                        ssynth.adsr.Key(true);
                        Console.WriteLine($"KEY ON");
                    }

                    if (i == 30)
                    {
                        //ssynth.lpf.Freq = 2500;
                    }

                    if (i == 40)
                    {
                        ssynth.adsr.Key(false);
                        Console.WriteLine($"KEY OFF");
                    }

                    System.Threading.Thread.Sleep(100);
                }

                sout.Stop();

                // Vis it
                //v.AllSeries.Add(ssynth.ser);
                //new System.Threading.Thread(() => v.ShowDialog()).Start();
            }
        }

        public class MySynth : UGen2
        {
            public DataSeries ser = new DataSeries();
            int serind = 0;

            public PulseOsc osc1 = new PulseOsc() { Freq = 400, Width = 0.1, Gain = 0.5 };
            public PulseOsc osc2 = new PulseOsc() { Freq = 401, Width = 0.1, Gain = 0.5 };
            //public SinOsc osc1 = new SinOsc() { Freq = 400, Gain = 0.5 };
            //public SinOsc osc2 = new SinOsc() { Freq = 500, Gain = 0.5 };
            public Mix mix1 = new Mix() { Gain = 0.5 };
            public ADSR adsr = new ADSR() { AttackTime = 0.2, DecayTime = 0.2, SustainLevel = 0.7, ReleaseTime = 0.5, Gain = 0.5 };
            public LPF lpf = new LPF() { Freq = 600, Gain = 0.8 };
            public Pan pan = new Pan() { Location = 0.0, Gain = 0.8 };

            public override Sample Next(double _)
            {
                //double dd1 = osc1.Next(0);
                //double dd2 = osc2.Next(0);
                //double dd3 = mix1.Next(dd1, dd2);
                //double dd4 = adsr.Next(dd3);
                //Sample dout = new Sample() { Left = dd1, Right = dd1 };

                double dd = adsr.Next(mix1.Next(osc1.Next(0), osc2.Next(0)));
                dd = lpf.Next(dd);
                //Sample dout = new Sample() { Left = dd, Right = dd };
                Sample dout = pan.Next(dd);

                //if (serind++ % 100 == 0)
                if (serind++ < 10000)
                {
                    //ser.AddPoint(serind, dd1);
                    ser.AddPoint(serind, dout.Left);
                }

                return dout;
            }

            public override void Note(double notenum, double amplitude)
            {
                adsr.Key(amplitude > 0);
                //and/or pass along to the noisemakers.
                //vcr1.Note(notenum, amplitude);
            }
        }
    }

    public class SYNTH_Full : TestSuite
    {
        public override void RunSuite()
        {
            MySynth synth = new MySynth() { Gain = 0.5 };

            SynthOutput sout = new SynthOutput();
            bool ok = sout.Init("SYNTH:ASIO4ALL v2");

            UT_TRUE(ok);

            if (ok)
            {
                sout.Synth = synth;
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
            public LPF lpf = new LPF() { Gain = 0.5 };
            ADSR adsrF = new ADSR() { AttackTime = 1.5, DecayTime = 0.3, SustainLevel = 0.4, ReleaseTime = 0.8, Gain = 0.5 };
            public Pan pan = new Pan() { Gain = 0.5 };

            public override Sample Next(double _)
            {
                lpf.Freq = 200 + 500 * adsrF.Next(1);
                Sample dout = pan.Next(lpf.Next(vcr1.Next(0)));
                return dout;
            }

            public override void Note(double notenum, double amplitude)
            {
                adsrF.Key(amplitude > 0);
                // Pass along to the noisemakers.
                vcr1.Note(notenum, amplitude);
            }
        }

        // A single voice in the generator.
        public class MyVoice : UGen
        {
            // 2 oscs slightly detuned and synced
            SinOsc osc1 = new SinOsc() { Freq = 400, Gain = 0.5 };
            SinOsc osc2 = new SinOsc() { Freq = 500, Gain = 0.5 };
            Mix mix1 = new Mix() { Gain = 0.5 };
            ADSR adsr = new ADSR() { AttackTime = 0.5, DecayTime = 0.3, SustainLevel = 0.4, ReleaseTime = 0.8, Gain = 0.5 };

            public MyVoice()
            {
            }

            public override void Note(double notenum, double amplitude)
            {
                osc1.Note(notenum, amplitude);
                osc2.Note(notenum * 1.05, amplitude);
                adsr.Key(amplitude > 0);
            }

            public override double Next(double _)
            {
                double dout = adsr.Next(mix1.Next(osc1.Next(0), osc2.Next(0)));
                return dout;
            }
        }
    }
}
