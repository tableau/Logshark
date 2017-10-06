# Logshark
[![Community Supported](https://img.shields.io/badge/Support%20Level-Community%20Supported-457387.svg)](https://www.tableau.com/support-levels-it-and-developer-tools)

Logshark is a command line utility that you can run against Tableau Server logs to generate a set of workbooks that provide insights into system performance, content usage, and error conditions.

Some common use cases for Logshark include: 
  * Troubleshooting issue(s) that are recorded in the logs. 
  * Analyzing system metrics from log data. 
  * Self-solving problems in Tableau without the fear of exposing sensitive corporate information. 
  * Regularly validating Tableau Server application behavior against historical data when taking a new build or making a system change.
  
![Sample Apache Workbook Screenshot](/Logshark.CLI/Resources/SampleScreenshot.png)

# How do I set up Logshark?

Logshark is deployed via a custom installer, which you can download from the [releases page](https://github.com/tableau/Logshark/releases/latest).  The installer manages dependencies and also bundles MongoDB for ease of setup for new users.  You will need to set up a Postgres database.  To get up and running, download the installer and then follow the instructions in the [installation guide](https://tableau.github.io/Logshark/) to configure it.

Logshark requires a 64-bit version of Windows in order to run, and must be run as an account with administrator privileges.

NOTE: If you are upgrading from a previous version of Logshark, the installer will handle most of the upgrade work for you, but during the upgrade your Logshark.config file will be overwritten.  If there are settings from this config you wish to preserve, please make a backup.

# How do I analyze results from Logshark?

The best way to analyze results is to run Logshark on your own logset and explore the generated workbooks via Tableau! Beyond what is included, you can configure Logshark to output your own custom workbooks. See the [installation guide](https://tableau.github.io/Logshark/) for more details on how to do this.

For the truly adventurous, Logshark features a plugin framework, so you can even build your own analysis plugin to leverage Logshark’s log parsing engine!

# What do I need to build Logshark from source?

The current development requirements are:

1. Windows operating system. (64-bit)
2. Visual Studio 2015 or later.
3. WiX Toolset Visual Studio Extension v3.10.1 or later - Required if you wish to to modify the installer projects.
  * Available at http://www.wixtoolset.org
4. Configuration Section Designer Visual Studio Extension - Required if you wish to modify & regenerate the "LogsharkConfigSection" custom config section class.
  * Available at http://csd.codeplex.com

It is recommended that you install the Logshark Workbook Creation Plugin Project Template extension by running the "Logshark Workbook Creation Plugin Project Template.vsix" file found in the root directory.  This adds a "Logshark Workbook Creation Plugin" project type to Visual Studio which you can use to easily get up and running developing a new plugin.

Note that you do not need to build Logshark from source to use it; a pre-built installer is available on the [releases page](https://github.com/tableau/Logshark/releases/latest).

# Is Logshark supported?

Logshark is Community Supported. This is intended to be a self-service tool and includes a user guide. Any bugs discovered should be filed in the Logshark [Git issue tracker](https://github.com/tableau/Logshark/issues).

# How can I contribute to Logshark?

Code contributions & improvements by the community are welcomed and encouraged! See the [LICENSE file](https://github.com/tableau/Logshark/blob/master/LICENSE) for current open-source licensing & use information.  Before we can accept pull requests from contributors, we do require a Contributor License Agreement.  See http://tableau.github.io/ for more details.
