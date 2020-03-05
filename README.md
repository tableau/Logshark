# LogShark
[![Community Supported](https://img.shields.io/badge/Support%20Level-Community%20Supported-457387.svg)](https://www.tableau.com/support-levels-it-and-developer-tools)

LogShark is a tool you can use to analyze and troubleshoot Tableau Server performance and activity. LogShark extracts data from Tableau Server and Tableau Desktop log files and builds workbooks that can help you identify and understand error conditions, performance bottlenecks, and background activity. LogShark works by running a set of targeted plugins that pull specific data out of the log files. LogShark builds a data source and provides Tableau workbooks, which you can then use to analyze the log files in Tableau.

LogShark can help you: 
  * Troubleshoot issue(s) that are recorded in the logs. 
  * Analyze system metrics from log data. 
  * Solve problems in Tableau without the fear of exposing sensitive corporate information. 
  * Validate Tableau Server application behavior against historical data when taking a new build or making a system change.

LogShark version 4 is a significant rewrite, and is substantially faster than before. It brings back the ability to write to a PostgreSQL database server, as well as a number of other improvements. See [releases page](https://github.com/tableau/Logshark/releases/latest) for a full list of updates.
  
![Sample Apache Workbook Screenshot](/assets/screenshot.png)

## Getting Started

There are 2 ways you can use LogShark.

### Self-Contained Application

Download and unzip the precompiled self-contained application from the following links:

[![Download LogShark for Win](https://img.shields.io/badge/Download%20LogShark%20for%20Win-Version%204.1-blue.svg)](https://github.com/tableau/Logshark/releases/download/v4.1/LogShark.Win.4.1.1911.09672-public.zip)

[![Download LogShark for macOS](https://img.shields.io/badge/Download%20LogShark%20for%20macOS-Version%204.1-blue.svg)](https://github.com/tableau/Logshark/releases/download/v4.1/LogShark.Mac.4.1.1911.09672-public.zip)

[![Setup LogShark](https://img.shields.io/badge/Setup%20LogShark-Installation%20and%20User%20Guide-lightgrey.svg)](https://tableau.github.io/Logshark/)

Note that LogShark is configured by the LogSharkConfig.json file in the Config directory. If you are replacing an existing copy of LogShark, be mindful of any changes made to this configuration file.

### Compile It Yourself

LogShark is a .NET Core 2.1 application. To compile it yourself, first clone or download the repository. Then run the following command:

Windows
```
dotnet publish LogShark -c Release -r win-x64 --self-contained true 
```

Mac
```
dotnet publish LogShark -c Release -r osx-x64 --self-contained true 
```

## Analysis

The best way to analyze results is to run LogShark on your own logset and explore the generated workbooks via Tableau. Beyond what is included, you can configure LogShark to output your own custom workbooks. See the [installation guide](https://tableau.github.io/Logshark/) for more details on how to do this.


## Support

LogShark is released as a [Community-Supported](https://www.tableau.com/support/itsupport) tool. It is intended to be a self-service tool and includes this user guide. Any bugs discovered should be filed in the LogShark [Git issue tracker](https://github.com/tableau/Logshark/issues).

## Contributing

Code contributions & improvements by the community are welcomed and encouraged! See the [LICENSE file](https://github.com/tableau/Logshark/blob/master/LICENSE) for current open-source licensing & use information.  Before we can accept pull requests from contributors, we do require a Contributor License Agreement.  See http://tableau.github.io for more details.
