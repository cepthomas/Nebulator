using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebulator.Test
{
    class Attributes // TODO delete if not used.
    {
    }

    // /// <summary>Test case info.</summary>
    // [AttributeUsage(AttributeTargets.Class)]
    // public class TestCaseAttribute : Attribute
    // {
    //     public virtual Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    //     public TestCaseAttribute(string id, string desc)
    //     {
    //         Properties.Add("Id", id);
    //         Properties.Add("Desc", desc);
    //     }
    // }



    // ///////// Old below ////////

    // // This is a set of attributes that can be used for decorating NUnit tests in order to 
    // // insert information that is needed for test reports.
    // // Beginning with NUnit 2.5, a property attribute is able to contain multiple name/value pairs. 
    // // This capability is not exposed publicly but may be used by derived property classes.

    // #region Assembly level properties
    // /// <summary></summary>
    // [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    // public class TestReportPropertyAttribute : Attribute // PropertyAttribute
    // {
    //     public virtual Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    //     /// <summary>
    //     /// Add dynamic information into the test output file at the top level.
    //     /// </summary>
    //     /// <param name="name">The name of the property.</param>
    //     /// <param name="value">The value of the property.</param>
    //     /// <param name="order">Force the order in the output row.</param>
    //     public TestReportPropertyAttribute(string name, string value, int order)
    //     {
    //         // Prepend with a number to force the order we want. The xsl will remove it.
    //         string sorder = order.ToString() + "_";
    //         Properties.Add(sorder + name, value);
    //     }

    //     /// <summary>
    //     /// Add the engineer name etc and referenced assemblies information to the test
    //     /// output file at the top level.
    //     /// Only instantiate this once.
    //     /// </summary>
    //     /// <param name="assyUnderTest">The name of the thing being tested.</param>
    //     /// <param name="desc">Description of the thing being tested.</param>
    //     /// <param name="files">List of files being tested.</param>
    //     public TestReportPropertyAttribute(string assyUnderTest, string desc, string files)
    //     {
    //         StringBuilder sb = new StringBuilder();
    //         List<AssemblyName> all = new List<AssemblyName>();

    //         string assyUnderTestVersion = "???";

    //         foreach (Assembly assy in AppDomain.CurrentDomain.GetAssemblies())
    //         {
    //             all.Add(assy.GetName());
    //             all.AddRange(assy.GetReferencedAssemblies());
    //         }

    //         // Format output.

    //         // Collect all referenced assemblies to avoid duplicates.
    //         HashSet<string> refAssemblies = new HashSet<string>();
    //         HashSet<string> allAssemblies = new HashSet<string>();

    //         foreach (AssemblyName an in all)
    //         {
    //             string san = an.Name;
    //             allAssemblies.Add(san);

    //             // Hide some noisy ones.
    //             if (!(san.Contains("mscorlib") || san.Contains("nunit") || san.Contains("System") || san.Contains("log4net") || san.Contains("NLog")))
    //             {
    //                 // Is this the one being tested?
    //                 if (san == assyUnderTest)
    //                 {
    //                     assyUnderTestVersion = string.Format("{0}.{1}.{2}", an.Version.Major, an.Version.Minor, an.Version.Build);
    //                 }
    //                 else
    //                 {
    //                     // Check to see if we have this one already.
    //                     string srefassy = string.Format("{0}:{1}", san, an.Version);
    //                     if (!refAssemblies.Contains(srefassy))
    //                     {
    //                         refAssemblies.Add(srefassy);
    //                         sb.Append(srefassy + " ");
    //                     }
    //                 }
    //             }
    //         }

    //         // Add them to the test report properties.
    //         Properties.Add("10_Project Name", "IDAS");
    //         Properties.Add("20_Component Name", assyUnderTest);
    //         Properties.Add("30_Engineer Name", Environment.UserName);
    //         Properties.Add("40_SW Version", assyUnderTestVersion);
    //         Properties.Add("50_Component/Test Description", desc);
    //         Properties.Add("60_Source Code File List", files);
    //         Properties.Add("70_Date Completed", DateTime.Now.ToString());
    //         Properties.Add("80_Software Engineer Signature", "");
    //         Properties.Add("85_Software Team Leader Signature", "N/A");
    //         Properties.Add("90_Legend", "CE:Code Execution CVV:Code Visual Verification P:Pass F:Fail I:Incomplete");

    //         //// Debug stuff.
    //         //string sref = "";
    //         //foreach(string s in allAssemblies)
    //         //{
    //         //    sref += s + ", ";
    //         //}
    //         //Properties.Add("95_All Assemblies", sref);
    //     }
    // }
    // #endregion


    // #region Method properties
    // /// <summary>Test expected results go here.</summary>
    // [AttributeUsage(AttributeTargets.Method)]
    // public class RequirementAttribute : Attribute
    // {
    //     public virtual Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    //     public RequirementAttribute(string text)
    //     {
    //         Properties.Add("rqmt", text);
    //     }
    // }

    // public enum TestMethod { CE, CVV }

    // [AttributeUsage(AttributeTargets.Method)]
    // public class TestMethodAttribute : Attribute
    // {
    //     public virtual Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    //     public TestMethodAttribute(TestMethod value)
    //     {
    //         Properties.Add("tmethod", value.ToString());
    //     }
    // }

    // [AttributeUsage(AttributeTargets.Method)]
    // public class NotesAttribute : Attribute
    // {
    //     public virtual Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    //     public NotesAttribute(string value)
    //     {
    //         //Properties.Add("notes", value);
    //     }
    // }

    // [AttributeUsage(AttributeTargets.Method)]
    // public class ExpectedResultAttribute : Attribute
    // {
    //     public virtual Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    //     public ExpectedResultAttribute(string value)
    //     {
    //         Properties.Add("expected", value);
    //     }
    // }
    // #endregion


    ///// <summary>Helper for diagnostics.</summary>
    //public class TestAttributesUtils
    //{
    //    static public string Dump(Type tp)
    //    {
    //        StringBuilder sb = new StringBuilder();

    //        // Using reflection.
    //        foreach (MethodInfo mi in tp.GetMethods())
    //        {
    //            // Output the properties.
    //            foreach (object attr in mi.GetCustomAttributes(typeof(PropertyAttribute), false))
    //            {
    //                PropertyAttribute t = attr as PropertyAttribute;
    //                foreach (object o in t.Properties.Keys)
    //                {
    //                    sb.AppendFormat("{0}:{1}{2}", o, t.Properties[o], Environment.NewLine);
    //                }
    //            }
    //        }

    //        return sb.ToString();
    //    }
    //}




    //public static void MainXXX()
    //{
    //    // Call function to get and display the attribute.
    //    GetAttribute(typeof(MainApp));
    //}

    //public static void GetAttribute(Type t)
    //{
    //    // Get instance of the attribute.
    //    TestReportPropertyAttribute MyAttribute = (TestReportPropertyAttribute)Attribute.GetCustomAttribute(t, typeof(TestReportPropertyAttribute));

    //    if (MyAttribute == null)
    //    {
    //        Console.WriteLine("The attribute was not found.");
    //    }
    //    else
    //    {
    //        //  In cases where multiple instances of the same attribute are applied to the same scope, you can use Attribute.GetCustomAttributes to place all instances of an attribute into an array. 


    //        // Get the Name value.
    //        Console.WriteLine("The Name Attribute is: {0}.", MyAttribute.Name);
    //        // Get the Level value.
    //        Console.WriteLine("The Level Attribute is: {0}.", MyAttribute.Level);
    //        // Get the Reviewed value.
    //        Console.WriteLine("The Reviewed Attribute is: {0}.", MyAttribute.Reviewed);
    //    }
    //}

}
