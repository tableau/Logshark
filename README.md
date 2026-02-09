# LogShark
[![Community Supported](https://img.shields.io/badge/Support%20Level-Community%20Supported-457387.svg)](https://www.tableau.com/support-levels-it-and-developer-tools)

LogShark is a tool for analyzing and troubleshooting Tableau Server and Tableau Desktop. LogShark extracts data from log files and builds workbooks that can help you identify and understand error conditions, performance bottlenecks, and background activity. LogShark works by running a set of targeted plugins that pull specific data out of the log files, building data sources, and generating Tableau workbooks which can be used for analysis.

LogShark can help you: 
  * Troubleshoot issues that are recorded in the logs. 
  * Analyze system metrics from log data. 
  * Solve problems in Tableau without exposing sensitive corporate information. 
  * Validate Tableau Server application behavior against historical data when taking a new build or making a system change.

See the [releases page](https://github.com/tableau/Logshark/releases/latest) for a full list of updates.
  
![Sample Apache Workbook Screenshot](/docs/assets/SampleScreenshot.png)

## Getting Started

There are 3 ways you can use LogShark.

### Self-Contained Application

Download and unzip the precompiled self-contained application from the following links:

[![Download LogShark for Win](https://img.shields.io/badge/Download%20LogShark%20for%20Win-Version%204.2.5-blue.svg)](https://github.com/tableau/Logshark/releases/download/v4.2.3/LogShark.Win.4.2.5.zip)

[![Setup LogShark](https://img.shields.io/badge/Setup%20LogShark-Installation%20and%20User%20Guide-brightgreen.svg)](https://tableau.github.io/Logshark/)

Note that LogShark is configured by the LogSharkConfig.json file in the Config directory. If you are replacing an existing copy of LogShark, be mindful of any changes made to this configuration file.

### Compile It Yourself

LogShark is a .NET Core 3.1 application. To compile it yourself:
1. Make sure you have .NET Core 3.1 SDK installed 
2. Clone or download the repository
3. Run the following command from the directory where `LogShark.sln` file is. Make sure to replace `<insert_version>` with the actual version, i.e. `4.2.1`:

Windows
```
dotnet publish LogShark -c Release -r win-x64 /p:Version=<insert_version> --self-contained true 
```

Linux
```
dotnet publish LogShark -c Release -r linux-x64 /p:Version=<insert_version> --self-contained true 
```

### Build and Run It Using Docker

Below are instructions on how to build and run LogShark using Docker on your own machine.

#### To build Docker image

1. Install Docker Desktop (if you don’t have it already)
2. Clone or download LogShark source code from this repository
3. Build LogShark container image by running the following command from the directory where `LogShark.sln` file is

```
docker build -f LogShark/Dockerfile -t logshark .
```

* Note the `.` at the end of the command - it is required
* `-t` parameter specifies Docker image name and tag. Use whatever makes sense for your environment, or leave `logshark` there.

#### To process a log set

Use docker run command to run LogShark in a container. For example:

```
docker run -v ~/TestLogSets/logs_clean_tsm.zip:/app/logs.zip -v ~/TestLogSets/LogSharkDocker/Output:/app/Output -v ~/TestLogSets/LogSharkDocker/ProdConfig.json:/app/Config/LogSharkConfig.json logshark:latest logs.zip --plugins "Apache;Config" -p
```

Let’s break down this command part by part:

* `-v` parameter maps a file or directory on local machine to a file/directly within container. This way LogShark in container can read files from the local machine (log set to process and config) and save output so it is available even after container is done and destroyed.
    * `-v ~/TestLogSets/logs_clean_tsm.zip:/app/logs.zip`  maps `~/TestLogSets/logs_clean_tsm.zip` file on host OS to `/app/logs.zip` file in container.
    * `-v ~/TestLogSets/LogSharkDocker/Output:/app/Output` maps Output directory of LogShark within container to a directory on host OS.
    * `-v ~/TestLogSets/LogSharkDocker/ProdConfig.json:/app/Config/LogSharkConfig.json` maps json config on host OS to a default LogShark configuration file within container (and hides the original file included with LogShark). This way there is no need to explicitly tell LogShark what config file to use.
* `logshark:latest` this is the name/tag of the container image to use. If you used a different name/tag while building the Docker image, use it here.
* `logs.zip --plugins "Apache;Config" -p` the rest of the line is passed to LogShark directly as arguments. See full list of available arguments [here](https://tableau.github.io/Logshark/docs/logshark_cmds).
    * The first parameter is the name of the log set to process and it is required. It points to a file we mapped with the first `-v` statement

#### OS Compatibility

* Above instructions are for macOS and Linux host OS running Linux containers.
* Windows host OS running Linux containers: Same instructions apply, but path format is different for mapping local files and dirs when running the container. I.e. `-v ~/TestLogSets/logs_clean_tsm.zip:/app/logs.zip` becomes `-v C:\tmp\logs_clean_tsm.zip:/app/logs.zip`
* Windows host OS running Windows containers: LogShark can run on Windows so this should be doable, but we do not use/test/support this scenario currently.



## Analysis

The best way to analyze results is to run LogShark on your own logset and explore the generated workbooks via Tableau. Beyond what is included, you can configure LogShark to output your own custom workbooks. See the [installation guide](https://tableau.github.io/Logshark/) for more details on how to do this.


## Support

LogShark is released as a [Community-Supported](https://www.tableau.com/support/itsupport) tool. It is intended to be a self-service tool and includes this user guide. Any bugs discovered should be filed in the LogShark [Git issue tracker](https://github.com/tableau/Logshark/issues).

## Contributing

Code contributions & improvements by the community are welcomed and encouraged! See the [LICENSE file](https://github.com/tableau/Logshark/blob/master/LICENSE) for current open-source licensing & use information.  Before we can accept pull requests from contributors, we do require a Contributor License Agreement.  See http://tableau.github.io for more details.
