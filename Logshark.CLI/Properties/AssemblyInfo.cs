using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Logshark")]
[assembly: AssemblyDescription("Command line harness for Logshark core engine.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Tableau")]
[assembly: AssemblyProduct("Logshark.CLI")]
[assembly: AssemblyCopyright("Tableau")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f0123db8-a320-4b48-b86e-123c56d3ed9a")]

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