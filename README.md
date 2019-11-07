# LogShark
[![Community Supported](https://img.shields.io/badge/Support%20Level-Community%20Supported-457387.svg)](https://www.tableau.com/support-levels-it-and-developer-tools)

LogShark is a tool you can use to analyze and troubleshoot Tableau Server performance and activity. LogShark extracts data from Tableau Server and Tableau Desktop log files and builds workbooks that can help you identify and understand error conditions, performance bottlenecks, and background activity. LogShark works by running a set of targeted plugins that pull specific data out of the log files. LogShark builds a data source and provides Tableau workbooks which you can then use to analyze the log files in Tableau.

Some common use cases for LogShark include: 
  * Troubleshooting issue(s) that are recorded in the logs. 
  * Analyzing system metrics from log data. 
  * Self-solving problems in Tableau without the fear of exposing sensitive corporate information. 
  * Regularly validating Tableau Server application behavior against historical data when taking a new build or making a system change.
  
![Sample Apache Workbook Screenshot](/Logshark.CLI/Resources/SampleScreenshot.png)

# How do I set up LogShark?

[![Download LogShark for Win](https://img.shields.io/badge/Download%20LogShark%20for%20Win-Version%204.1-blue.svg)](https://github.com/tableau/Logshark/releases/download/v4.1/LogShark.Win.4.1.1911.09672-public.zip)

[![Download LogShark for macOS](https://img.shields.io/badge/Download%20LogShark%20for%20macOS-Version%204.1-blue.svg)](https://github.com/tableau/Logshark/releases/download/v4.1/LogShark.Mac.4.1.1911.09672-public.zip)

[![Setup LogShark](https://img.shields.io/badge/Setup%20LogShark-Installation%20and%20User%20Guide-lightgrey.svg)](https://tableau.github.io/Logshark/)

No installer is needed for this version, as LogShark is provided as a self-contained application. Simply download a zip file with LogShark (see link above), navigate to a location where you want to install it, and unzip the file there.

This version of LogShark is significantly ***FASTER*** and brings back the ability to write to a PostgreSQL database server, as well as a number of other improvements. See [releases page](https://github.com/tableau/Logshark/releases/latest) for a full list of updates.

# System Requirements

**For Windows**: 
-  LogShark requires a 64-bit version of Windows in order to run, and must be run as an account with administrator privileges. 
- Hyper API for C++. If Hyper requirements are not met on the machine, LogShark will fail. The simpest way to meet Hyper requirements is to install Tableau Desktop on your machine.
  - If you don't have Tableau Desktop installed, you will need Visual C++ Redistributables. Download x64 version of "Visual Studio 2015, 2017 and 2019" package from https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads.

**For macOS:** 
- LogShark runs on macOS versions 10.12 "Sierra", 10.13 "High Sierra, and 10.14 "Mojave".
  - Currently LogShark doesn't support macOS 10.15 "Catalina".

NOTE: If you are copying over existing LogShark folder, make a backup copy of your config file to preserve any valuable settings you have previously set for LogShark.

# How do I analyze results from LogShark?

The best way to analyze results is to run LogShark on your own logset and explore the generated workbooks via Tableau. Beyond what is included, you can configure LogShark to output your own custom workbooks. See the [installation guide](https://tableau.github.io/Logshark/) for more details on how to do this.

# What do I need to build LogShark from source? 

Instructions on how to build LogShark from the source code are coming soon. 

Please note that current source code is for LogShark 3.0. The source code for LogShark 4.1 will be released soon.


# Is LogShark supported?

LogShark is released as a [Community-Supported](https://www.tableau.com/support/itsupport) tool. It is intended to be a self-service tool and includes this user guide. Any bugs discovered should be filed in the LogShark [Git issue tracker](https://github.com/tableau/Logshark/issues).

# How can I contribute to LogShark?

Code contributions & improvements by the community are welcomed and encouraged! See the [LICENSE file](https://github.com/tableau/Logshark/blob/master/LICENSE) for current open-source licensing & use information.  Before we can accept pull requests from contributors, we do require a Contributor License Agreement.  See http://tableau.github.io for more details.
