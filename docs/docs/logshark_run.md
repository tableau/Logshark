---
title: Run LogShark
---


After you have installed, the rest is easy. You just point LogShark to the log files and view the results in Tableau workbooks.

In this section:

* TOC
{:toc}

-----------

### Run LogShark to process the log files


1. Open a Command Prompt window (Windows) or Terminal (mac) as administrator.

1. To see the LogShark command options and syntax, run `LogShark --help`

1. Navigate to the directory where you want to output the results

1. To process a logset, just run LogShark.exe and specify the path to the Tableau archive and any other option you wish to set.

```
    LogShark <LogSetLocation> <RunId> [Options]
```

Where *`LogSetLocation`* represents a zipped archive file (`logs.zip`), directory, or hash value from a previous run. LogShark supports both absolute and relative paths. **true?**

**Examples:**

Here are various examples of how to run LogShark. 

```
LogShark D:\logs.zip                                                  | Runs LogShark on logs.zip and outputs locally.
LogShark C:\logs\logset --plugins "Backgrounder;ClusterController"    | Runs specified plugins on existing unzipped log directory.
LogShark logs.zip -p                                                  | Runs LogShark and publishes to Tableau Server.
LogShark logs.zip -c CustomLogSharkConfig.json                        | Runs LogShark with a custom config file.
```

For more information, see the [LogShark command options](LogShark_cmds).

-----------------

### View the generated workbooks

1.  All workbooks or other plugin-generated content is saved in a `\<LogShark_run_location>\Output\workbooks` folder in the directory from where LogShark is run. If the folder doesn't exist, LogShark creates it.

    When LogShark processes the log files it creates a new folder for the results in the `\Output` folder. The name of the folder is *`DateTime_HostName_FileName`*, where  *`HostName`* is the name of the computer where LogShark was run. *`DateTime`* is the time stamp that indicates when the logs were processed, and *`FileName`* is the name of the archive file. The folder contains all the workbooks for the plugins that were run in that instance.

2.  Navigate to results folder you are interested in and double-click the Tableau workbook you want to view. 

    For example, if you open the `Apache.twbx` workbook, you can view the viz load statistics generated from the Apache log files.     


    ![]({{ site.baseurl }}/assets/SampleScreenshot.png)

   For information about all the plugins and workbooks, see [LogShark Plugins and Generated Workbooks](LogShark_plugins)