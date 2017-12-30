using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace Nebulator.Test
{
    public class SUT_1 : TestCase
    {
        public override void RunCase()
        {
            int int1 = 321;
            int int2 = 987;
            string str1 = "round and round";
            string str2 = "the mulberry bush";
            double dbl1 = 1.500F;   
            double dbl2 = 1.600F;
            double dblTol = 0.001F;

            UT_STEP(1, "Core function tests.");

            UT_INSPECT("Test UT_INSPECT. Visually inspect that this appears in the output.");

            UT_INSPECT("Test UT_INSPECT with parms.", int1, dbl2);

            UT_INSPECT("Should fail on UT_CHECK.");
            UT_CHECK(int2 < int1, "Boolean test");

            // Should pass on UT_CHECK.
            UT_CHECK(int2 == 987, "Boolean test");

            UT_INSPECT("Should fail on UT_STR_EQUAL.");
            UT_STR_EQUAL(str1, str2);

            // Should pass on UT_STR_EQUAL.
            UT_STR_EQUAL(str2, "the mulberry bush");

            UT_STR_EMPTY("");

            UT_INSPECT("Should fail on UT_NOT_EQUAL.");
            UT_NOT_EQUAL(int1, 321);

            // Should pass on UT_NOT_EQUAL.
            UT_NOT_EQUAL(int2, int1);

            UT_INSPECT("Should fail on UT_LESS_OR_EQUAL.");
            UT_LESS_OR_EQUAL(int2, int1);

            // Should pass on UT_LESS_OR_EQUAL.
            UT_LESS_OR_EQUAL(int1, 321);

            // Should pass on UT_LESS_OR_EQUAL.
            UT_LESS_OR_EQUAL(int1, int2);

            UT_INSPECT("Should fail on UT_GREATER.");
            UT_GREATER(int1, int2);

            // Should pass on UT_GREATER.
            UT_GREATER(int2, int1);

            UT_INSPECT("Should fail on UT_GREATER_OR_EQUAL.");
            UT_GREATER_OR_EQUAL(int1, int2);

            // Should pass on UT_GREATER_OR_EQUAL.
            UT_GREATER_OR_EQUAL(int2, 987);

            // Should pass on UT_GREATER_OR_EQUAL.
            UT_GREATER_OR_EQUAL(int2, int1);

            // Should pass on UT_CLOSE.
            UT_CLOSE(dbl1, dbl2, dbl2 - dbl1 + dblTol);

            UT_INSPECT("Should fail on UT_CLOSE.");
            UT_CLOSE(dbl1, dbl2, dbl2 - dbl1 - dblTol);
        }
    }

    public class SUT_2 : TestCase
    {
        public override void RunCase()
        {
            UT_INFO("It may be multiple lines.");

            UT_STEP(1, "Tests various string functions, including UTF8 support.");

            string str1 = RandStr(10);
            string str2 = str1.Clone() as string;
            UT_STR_EQUAL(str1, str2);

            UT_STR_EQUAL(str1, "Should fail");
            UT_INSPECT("Previous step should have failed.");

            UT_STR_NOT_EQUAL(str1, "Should pass");

            UT_STEP(2, "Tests various ...... functions.");
        }
    }

    public class SUT_3 : TestCase
    {
        public override void RunCase()
        {
            UT_INFO("Other stuff.");

            UT_STEP(1, "Tests various string functions, including UTF8 support.");

            string str1 = RandStr(10);
            string str2 = str1.Clone() as string;
            UT_STR_EQUAL(str1, str2);

            UT_STR_EQUAL(str1, "Should fail");
            UT_INSPECT("Previous step should have failed.");

            UT_STR_NOT_EQUAL(str1, "Should pass");

            UT_STEP(2, "Tests various ...... functions.");

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
