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



/* binary looks like:
/muse/elements/alpha_relative\0\0\0
,ffff\0\0\0
\xff\xc0\0\0
\xff\xc0\0\0
\xff\xc0\0\0
\xff\xc0\0\0
*/


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

    public class OSC_Address : TestSuite
    {
        public override void RunSuite()
        {
            Address a = new Address("TODOX");



        }
    }

    public class OSC_Message : TestSuite
    {
        public override void RunSuite()
        {
            Message m = new Message("TODOX");

        }
    }

    public class OSC_Bundle : TestSuite
    {
        public override void RunSuite()
        {
            DateTime dt = new DateTime(2005, 5, 9, 15, 47, 39, 123);
            TimeTag tt = new TimeTag(dt); // specific constructor
            TimeTag ttImmediate = new TimeTag(); // default constructor

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
