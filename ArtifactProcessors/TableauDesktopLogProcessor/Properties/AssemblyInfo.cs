﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("TableauDesktopLogProcessor")]
[assembly: AssemblyDescription("Library for processing Tableau Desktop log files")]
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
[assembly: Guid("d6d4516c-0cc6-40b3-9002-b84f81e2c90b")]

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

// Make internal classes visible to the unit test project.
[assembly: InternalsVisibleTo("Logshark.Tests")]