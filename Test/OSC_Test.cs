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

namespace Nebulator.Test
{
    public class OSC_1 : TestSuite
    {
        public override void RunSuite()
        {
            UT_INFO("Other stuff, could be in another file.");

            string str1 = "123"; // Utils.RandStr(10);
            string str2 = str1.Clone() as string;
            UT_EQUAL(str1, str2);

            UT_EQUAL(str1, "Should fail");
            UT_INFO("Previous step should have failed.");

            UT_NOT_EQUAL(str1, "Should pass");

            UT_INFO("Tests various ...... functions.");

            // Cause an explosion.
            //UT_ASSERT(11, 22);
            //UT_INFO(Sub(0));
        }

        string Sub(int i)
        {
            int v = 100 / i;
            return v.ToString();
        }
    }
}
