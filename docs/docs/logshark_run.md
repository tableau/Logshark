---
title: Run Logshark
---


After you have installed, the rest is easy. You just point Logshark to the log files and view the results in Tableau workbooks.

In this section:

* TOC
{:toc}

-----------

### Run Logshark to process the log files


1. Open a Command Prompt window (Windows) or Terminal (mac) as administrator.

1. To see the LogShark command options and syntax, run `logshark --help`

1. Navigate to the directory where you want to output the results

1. To process a logset, just run LogShark.exe and specify the path to the Tableau archive and any other option you wish to set.

```
    logshark <LogSetLocation> <RunId> [Options]
```

Where *`LogSetLocation`* represents a zipped archive file (`logs.zip`), directory, or hash value from a previous run. Logshark supports both absolute and relative paths. **true?**

**Examples:**

Here are varios examples of how to run LogShark. 

```
logshark D:\logs.zip                                                  | Runs logshark on logs.zip and outputs locally.
logshark C:\logs\logset --plugins "Backgrounder;ClusterController"    | Runs specified plugins on existing unzipped log directory.
logshark logs.zip -p                                                  | Runs logshark and publishes to Tableau Server.
logshark logs.zip -c CustomLogSharkConfig.json                        | Runs logshark with a custom config file.
```

For more information, see the [Logshark command options](logshark_cmds).

-----------------

### View the generated workbooks

1.  All workbooks or other plugin-generated content is saved in a `\<LogShark_run_location>\Output\workbooks` folder in the directory from where Logshark is run. If the folder doesn't exist, LogShark creates it.

    When Logshark processes the log files it creates a new folder for the results in the `\Output` folder. The name of the folder is *`DateTime_HostName_FileName`*, where  *`HostName`* is the name of the computer where Logshark was run. *`DateTime`* is the time stamp that indicates when the logs were processed, and *`FileName`* is the name of the archive file. The folder contains all the workbooks for the plugins that were run in that instance.

2.  Navigate to results folder you are interested in and double-click the Tableau workbook you want to view. 

    For example, if you open the `Apache.twbx` workbook, you can view the viz load statistics generated from the Apache log files.     


    ![]({{ site.baseurl }}/assets/SampleScreenshot.png)

   For information about all the plugins and workbooks, see [Logshark Plugins and Generated Workbooks](logshark_plugins)