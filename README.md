# LogShark
[![Community Supported](https://img.shields.io/badge/Support%20Level-Community%20Supported-457387.svg)](https://www.tableau.com/support-levels-it-and-developer-tools)

LogShark is a command line utility that you can use to analyze and troubleshoot Tableau performance and activity. LogShark extracts data from Tableau Server and Desktop logs and builds a set of Tableau workbooks that provide insights into the system performance, content usage, and error conditions.

Some common use cases for LogShark include: 
  * Troubleshooting issue(s) that are recorded in the logs. 
  * Analyzing system metrics from log data. 
  * Self-solving problems in Tableau without the fear of exposing sensitive corporate information. 
  * Regularly validating Tableau Server application behavior against historical data when taking a new build or making a system change.
  
![Sample Apache Workbook Screenshot](/Logshark.CLI/Resources/SampleScreenshot.png)

# How do I set up LogShark?

[![Download LogShark](https://img.shields.io/badge/Download%20Logshark-Version%203.0.1-blue.svg)](https://github.com/tableau/Logshark/releases/download/3.0.1/Setup_Logshark_v3.0.1.exe)

[![Setup LogShark](https://img.shields.io/badge/Setup%20Logshark-Installation%20and%20User%20Guide-lightgrey.svg)](https://tableau.github.io/Logshark/)

No installer is needed for this version, as LogShark is provided as a self-contained application. Simply download a zip file with LogShark (see link above), navigate to a location where you want to install it, and unzip the file there.

This version of LogShark is significantly ***FASTER*** and brings back the ability to write to a PostgreSQL database server, as well as a number of other improvements. See [release page](**add a link**) for a full list of updates.

# System Requirements

**For Windows**: -   LogShark requires a 64-bit version of Windows in order to run, and must be run as an account with administrator privileges. 
-   Hyper API for C++. If Hyper requirements are not met on the machine, LogShark will fail. The simpest way to meet Hyper requirements is to install Tableau Desktop on your machine.
  If you don't have Tableau Desktop installed, you will need Visual C++ Redistributables. Download x64 version of "Visual Studio 2015, 2017 and 2019" package from https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads.

**For macOS:** OS version must be 10.12 "Sierra" or later
- If you are running 10.15 "Catalina", you will need to follow the following [resolution steps](/docs/logshark_catalina.md).

NOTE: If you are copying over existing LogShark folder, make a backup copy of your config file to preserve any valuable settings you have previously set for LogShark.

# How do I analyze results from LogShark?

The best way to analyze results is to run LogShark on your own logset and explore the generated workbooks via Tableau. Beyond what is included, you can configure LogShark to output your own custom workbooks. See the [installation guide](https://tableau.github.io/Logshark/) for more details on how to do this.

For the truly adventurous, LogShark features a plugin framework, so you can even build your own analysis plugin to leverage LogShark's log parsing engine! _ **Remove? Similar to internal documentation**:question:

# What do I need to build LogShark from source? 

(**Remove this since we are not publishing the source code for now or just keep it?**):question:

The current development requirements are:

1. Windows operating system. (64-bit)
2. Visual Studio 2015 or later.
3. WiX Toolset Visual Studio Extension v3.10.1 or later - Required if you wish to to modify the installer projects.
  * Available at http://www.wixtoolset.org
4. Configuration Section Designer Visual Studio Extension - Required if you wish to modify & regenerate the "LogsharkConfigSection" custom config section class.
  * Available at http://csd.codeplex.com
5. Download [hyperd.exe](https://github.com/tableau/Logshark/releases/download/v3.0/hyperd.exe) and [hyperd_sse2.exe](https://github.com/tableau/Logshark/releases/download/v3.0/hyperd_sse2.exe) and place them in .\Tableau.ExtractApi\lib\SDK\hyper\ (**Is this relevant or is the link different**)

It is recommended that you install the LogShark Workbook Creation Plugin Project Template extension by running the "LogShark Workbook Creation Plugin Project Template.vsix" file found in the root directory.  This adds a "LogShark Workbook Creation Plugin" project type to Visual Studio which you can use to easily get up and running developing a new plugin.

Note that you do not need to build LogShark from source to use it; a zipped self-contained application is available here [releases page](https://github.com/tableau/Logshark/releases/latest). **check the link**

# Is LogShark supported?

LogShark is released as a [Community-Supported](https://www.tableau.com/support/itsupport) tool. It is intended to be a self-service tool and includes this user guide. Any bugs discovered should be filed in the LogShark [Git issue tracker](https://github.com/tableau/Logshark/issues).

# How can I contribute to LogShark?

Code contributions & improvements by the community are welcomed and encouraged! See the [LICENSE file](https://github.com/tableau/Logshark/blob/master/LICENSE) for current open-source licensing & use information.  Before we can accept pull requests from contributors, we do require a Contributor License Agreement.  See http://tableau.github.io for more details.
