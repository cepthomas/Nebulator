using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PNUT;
using Nebulator.OSC;


namespace Nebulator.Test
{
    public class OSC_TimeTag : TestSuite
    {
        public override void RunSuite()
        {
            // Make some test objects.
            DateTime dt1 = new DateTime(2005,  5,  9, 15, 47, 39, 123);
            DateTime dt2 = new DateTime(2022, 11, 24,  7, 29,  6, 801);

            TimeTag ttImmediate = new TimeTag(); // default constructor
            TimeTag tt1 = new TimeTag(dt1); // specific constructor
            TimeTag tt2 = new TimeTag(dt2);
            TimeTag tt1raw = new TimeTag(tt1.Raw); // constructor from raw
            TimeTag tt2copy = new TimeTag(new TimeTag(dt2)); // copy constructor

            // Check them all.
            UT_EQUAL(ttImmediate.ToString(), "When:Immediate");
            UT_EQUAL(tt1.ToString(), "When:2005-05-09 15:47:39.000 Seconds:3324642459 Fraction:528280977");
            UT_EQUAL(tt2.ToString(), "When:2022-11-24 07:29:06.000 Seconds:3878263746 Fraction:3440268803");

            UT_TRUE(tt1.Equals(tt1));
            UT_FALSE(ttImmediate.Equals(tt2));
            UT_FALSE(tt1raw.Equals(tt1));

            UT_TRUE(tt1 == tt1raw);
            UT_TRUE(tt2 == tt2copy);
            UT_TRUE(tt1 != tt2);
            UT_FALSE(tt1 != tt1raw);
            UT_FALSE(tt2 != tt2copy);
            UT_FALSE(tt1 == tt2);

            UT_TRUE(tt2 >= tt1);
            UT_TRUE(tt2 > tt1);
            UT_FALSE(tt1 >= tt2);
            UT_FALSE(tt1 > tt2);

            UT_TRUE(tt1 <= tt2);
            UT_TRUE(tt1 < tt2);
            UT_FALSE(tt2 <= tt1);
            UT_FALSE(tt2 < tt1);
        }
    }

    public class OSC_Message : TestSuite
    {
        public override void RunSuite()
        {
            Message m1 = new Message(@"/foo/bar");

            m1.Data.Add(919);
            m1.Data.Add("some text");
            m1.Data.Add(83.743f); // float, not double!
            m1.Data.Add(new List<byte>() { 11, 28, 205, 68, 137, 251 });

            List<byte> packed = m1.Pack();

            //var vs = packed.Dump("|");
            //UT_INFO(vs);

            UT_FALSE(packed == null);
            UT_EQUAL(packed.Count(), 52);

            Message m2 = Message.Unpack(packed.ToArray());

            UT_FALSE(m2 == null);
            UT_EQUAL(m2.Address, m1.Address);
            UT_EQUAL(m2.Data.Count, m1.Data.Count);
            UT_EQUAL(m2.Data[0].ToString(), m1.Data[0].ToString());
            UT_EQUAL(m2.Data[1].ToString(), m1.Data[1].ToString());
            UT_EQUAL(m2.Data[2].ToString(), m1.Data[2].ToString());
            UT_EQUAL(m2.Data[3].ToString(), m1.Data[3].ToString());
        }
    }

    public class OSC_Bundle : TestSuite
    {
        public override void RunSuite()
        {
            DateTime dt = new DateTime(2005, 5, 9, 15, 47, 39, 123);
            TimeTag tt = new TimeTag(dt);
            TimeTag ttImmediate = new TimeTag();

            Bundle b = new Bundle(tt);

        }
    }

    public class OSC_Output : TestSuite
    {
        public override void RunSuite()
        {


        }
    }

    public class OSC_Input : TestSuite
    {
        public override void RunSuite()
        {


        }
    }


}
