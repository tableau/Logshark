﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Logshark.ConnectionModel")]
[assembly: AssemblyDescription("Core connection entities used by Logshark")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Tableau")]
[assembly: AssemblyProduct("Logshark")]
[assembly: AssemblyCopyright("Tableau")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("33ea5733-998c-4f71-ac5c-ad125268778e")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyFileVersion("2.1.0.0")]

// Required for log4net.
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

// Make internal classes visible to the unit test project.
[assembly: InternalsVisibleTo("Logshark.Tests")]