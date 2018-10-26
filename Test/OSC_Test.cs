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
    public class OSC_1 : TestSuite
    {
        public override void RunSuite()
        {
            Message m = new Message("TODOX");

            var mp = m.Pack();

            //UT_EQUAL(str1, "Should fail");
            //UT_INFO("Previous step should have failed.");

            //UT_NOT_EQUAL(str1, "Should pass");

            //UT_INFO("Tests various ...... functions.");
        }
    }
}
