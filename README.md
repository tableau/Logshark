# Logshark

# What is Logshark?

Logshark is a standalone console application that you can use to analyze and troubleshoot issues with Tableau performance and activity. Logshark extracts data from Tableau Server and Desktop log files and builds workbooks that can help you identify and understand error conditions, performance bottlenecks, and background activity.

# How do I set up Logshark?

Logshark is deployed via a custom installer, which manages dependencies and also bundles MongoDB for ease of setup for new users. To get up and running, follow the instructions in the installation guide.

Logshark requires a 64-bit version of Windows in order to run.

# How do I analyze results from Logshark?

The best way to analyze results is to run the tool on your logset and explore the generated workbooks via Tableau! Beyond what’s included, you can configure Logshark to output your own custom workbooks. See the installation guide for more details on how to do this.

For the truly adventurous, Logshark features a plugin framework, so you can even build your own analysis plugin to leverage Logshark’s log parsing engine!

# What do I need to build Logshark from source?

The current development requirements are:

1. Windows operating system (64-bit)

2. Visual Studio 2015 or later.

3. WiX Toolset Visual Studio Extension v3.10.1 or later - Required if you wish to modify the installer projects. Available at http://www.wixtoolset.org

4. Configuration Section Designer Visual Studio Extension - Required if you wish to modify & regenerate the "LogsharkConfigSection" custom config section class. Available at http://csd.codeplex.com

5. NUnit Test Adapter 3 Extension - Required if you wish to run the unit tests.

# Is Logshark supported?

Logshark is made available AS-IS with no support. This is intended to be a self-service tool and includes a user guide. Any bugs discovered should be filed in the Logshark Git issue tracker.

# How can I contribute to Logshark?

Code contributions & improvements by the community are welcomed & encouraged! See the LICENSE file for current open-source licensing & use information. 
