using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

namespace Nebulator.Common
{
    /// <summary>
    /// Info about one assembly.
    /// </summary>
    public class AssemblyInfoTool
    {
        /// <summary>
        /// The filename if pertinent.
        /// </summary>
        public string FileName { get; private set; } = "";

        /// <summary>
        /// The assembly name.
        /// </summary>
        public string AssemblyName { get; private set; } = "";

        /// <summary>
        /// The target version.
        /// </summary>
        public string Target { get; private set; } = "";

        /// <summary>
        /// The .NET format version number.
        /// </summary>
        public string Version { get; private set; } = "";

        /// <summary>
        /// List of assemblies referenced by this one.
        /// </summary>
        public List<AssemblyInfoTool> ReferencedAssemblies { get; private set; } = new List<AssemblyInfoTool>();

        /// <summary>
        /// The types defined in this assembly.
        /// </summary>
        public List<string> TypeNames { get; private set; } = new List<string>();

        /// <summary>
        /// All referenced assemblies to avoid duplicates.
        /// </summary>
        HashSet<string> _refAssemblies = new HashSet<string>();

        /// <summary>
        /// Process an individual file by loading it and processing the assembly.
        /// </summary>
        /// <param name="fn">The filename</param>
        /// <returns>Error string or empty if ok.</returns>
        public string ProcessFile(string fn)
        {
            string ret = "";

            try
            {
                // Gather info.
                FileName = fn;

                // Try to load the assembly.
                Assembly assy = Assembly.LoadFrom(fn);
                ret = ProcessAssembly(assy).ToString();
            }
            catch (BadImageFormatException)
            {
                ret = "Assume it is a non-.NET dll";
            }
            catch (Exception e)
            {
                // Other errors.
                ret = "*** " + e.Message;
            }

            return ret;
        }

        /// <summary>
        /// Process one loaded assembly.
        /// </summary>
        /// <param name="assy">The assembly</param>
        /// <returns>Error string or empty if ok.</returns>
        public bool ProcessAssembly(Assembly assy)
        {
            bool ret = false;

            try
            {
                AssemblyName asmName = assy.GetName();
                object[] list = assy.GetCustomAttributes(true);

                var attributes = list.OfType<TargetFrameworkAttribute>();

                if (attributes != null && attributes.Count() > 0)
                {
                    Target = attributes.First().FrameworkName;
                    AssemblyName = asmName.Name;
                    Version = asmName.Version.ToString();

                    foreach (AssemblyName refassy in assy.GetReferencedAssemblies())
                    {
                        // Check to see if we have this one already.
                        string srefassy = refassy.Name + " " + refassy.Version.ToString();
                        if (!_refAssemblies.Contains(srefassy))
                        {
                            _refAssemblies.Add(srefassy);

                            AssemblyInfoTool refai = new AssemblyInfoTool()
                            {
                                AssemblyName = refassy.Name,
                                FileName = "",
                                Version = refassy.Version.ToString()
                            };
                            ReferencedAssemblies.Add(refai);
                        }
                    }

                    foreach (Module Module in assy.GetModules())
                    {
                        Type[] TypesArray = Module.FindTypes(null, null);

                        foreach (Type t in TypesArray)
                        {
                            TypeNames.Add(t.Name);
                        }
                    }

                    ret = true;
                }
            }
            catch (Exception)
            {
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// Convert contents to a string for plain text output.
        /// </summary>
        /// <returns>Formatted string</returns>
        public string Format()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Assembly ");
            if (FileName.Length > 0)
            {
                sb.AppendLine("  File " + FileName);
            }
            sb.AppendLine("  Name " + AssemblyName);
            sb.AppendLine("  Version " + Version);
            sb.AppendLine("  Target " + Target);

            foreach (AssemblyInfoTool refassy in ReferencedAssemblies)
            {
                sb.AppendLine("  RefAssembly " + refassy.AssemblyName + " " + refassy.Version.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Utility method to turn assembly information into a string suitable for logging.
        /// Typically call with: GetAssemblyInfo(Assembly.GetExecutingAssembly())
        /// </summary>
        /// <param name="assy">The assembly to document.</param>
        /// <returns>String representation of assembly information.</returns>
        public static string GetAssemblyInfo(Assembly assy)
        {
            string ret = "";

            try
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("{0}:{1}", assy.GetName().Name, assy.GetName().Version);

                // Referenced assemblies.
                foreach (AssemblyName refassy in assy.GetReferencedAssemblies())
                {
                    sb.AppendFormat(" {0}:{1}", refassy.Name, refassy.Version);
                }

                ret = sb.ToString();
            }
            catch (Exception e)
            {
                ret = "*** " + e.Message;
            }

            return ret;
        }
    }
}
