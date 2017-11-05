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
    //[TestCase("LWUT_1", "Tester for the utility functions.")]
    public class LWUT_1 : TestCase
    {
        public override void RunCase()
        {
            UT_INFO("It may be multiple lines.");

            UT_STEP(1, "Tests various string functions, including UTF8 support.");

            string zTest1 = RandStr(100);
            string zTest2 = zTest1.Clone() as string;
            UT_CHECK_STR_EQUAL(zTest1, zTest2);

            UT_STEP_END();

            UT_STEP(2, "Tests various time functions.");

            UT_STEP_END();
        }
    }

    //[TestCase("LWUT_2", "Tester for other stuff.")]
    public class LWUT_2 : TestCase
    {
        public override void RunCase()
        {
            UT_INFO("Here I am.");

            UT_STEP(1, "Tests blabla.");

            string zTest1 = RandStr(100);
            string zTest2 = zTest1.Clone() as string;
            UT_CHECK_STR_EQUAL(zTest1, zTest2);

            UT_CHECK_STR_EQUAL(zTest1, "zTest2");

            UT_STEP_END();

            UT_STEP(2, "Tests various time functions.");

            UT_STEP_END();
        }
    }

}



/*
/////////////////////////////////////////////////////////////////////////////
UT_CASE(LWUT_3, "Test Other Functions")
{
	//RET_STAT RetStat = RET_STAT_NO_ERR;
	int iTest1 = 321;
	int iTest2 = 987;
	const char* zTest1 = "round and round";
	const char* zTest2 = "the mulberry bush";
	double dTest1 = 1.500F;
	double dTest2 = 1.600F;
	double dTol = 0.001F;

	UT_STEP(1, "The remaining tests for CMN_UnitTester.h are lumped into one test step.");

	UT_INSP("Test UT_INSP. Visually inspect that this appears in the output.");

	UT_INSP_1("Test UT_INSP_1. Visually inspect that this appears in the output with parm 321.", iTest1);

	UT_INSP("Substep should fail on UT_CHECK.")
	UT_CHECK(iTest2 < iTest1);

	// Substep should pass on UT_CHECK.
	UT_CHECK(iTest2 == 987);

	UT_INSP("Substep should fail on UT_CHECK_STR_EQUAL.");
	UT_CHECK_STR_EQUAL(zTest1, zTest2);

	// Substep should pass on UT_CHECK_STR_EQUAL.
	UT_CHECK_STR_EQUAL(zTest2, "the mulberry bush");

	UT_INSP("Substep should fail on UT_CHECK_NOT_EQUAL.");
	UT_CHECK_NOT_EQUAL(iTest1, 321);

	// Substep should pass on UT_CHECK_NOT_EQUAL.
	UT_CHECK_NOT_EQUAL(iTest2, iTest1);

	UT_INSP("Substep should fail on UT_CHECK_LESS_OR_EQUAL.");
	UT_CHECK_LESS_OR_EQUAL(iTest2, iTest1);

	// Substep should pass on UT_CHECK_LESS_OR_EQUAL.
	UT_CHECK_LESS_OR_EQUAL(iTest1, 321);

	// Substep should pass on UT_CHECK_LESS_OR_EQUAL.
	UT_CHECK_LESS_OR_EQUAL(iTest1, iTest2);

	UT_INSP("Substep should fail on UT_CHECK_GREATER.")
	UT_CHECK_GREATER(iTest1, iTest2);

	// Substep should pass on UT_CHECK_GREATER.
	UT_CHECK_GREATER(iTest2, iTest1);

	UT_INSP("Substep should fail on UT_CHECK_GREATER_OR_EQUAL.")
	UT_CHECK_GREATER_OR_EQUAL(iTest1, iTest2);

	// Substep should pass on UT_CHECK_GREATER_OR_EQUAL.
	UT_CHECK_GREATER_OR_EQUAL(iTest2, 987);

	// Substep should pass on UT_CHECK_GREATER_OR_EQUAL.
	UT_CHECK_GREATER_OR_EQUAL(iTest2, iTest1);

	// Substep should pass on UT_CHECK_CLOSE.
	UT_CHECK_CLOSE(dTest1, dTest2, dTest2 - dTest1 + dTol);

	UT_INSP("Substep should fail on UT_CHECK_CLOSE.");
	UT_CHECK_CLOSE(dTest1, dTest2, dTest2 - dTest1 - dTol);

	UT_STEP_END();
}
*/
