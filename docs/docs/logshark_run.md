---
title: Run Logshark and View the Results
---


After you set up PostgreSQL database (and MongoDB, if necessary) and have installed and configured Logshark, the rest is easy. You just point Logshark to the log files and view the results in Tableau workbooks.

In this section:

* TOC
{:toc}

-----------

### Run Logshark to process the log files


1.  Open a Command Prompt window as administrator.

2.  Run `Setup_Logshark_v1.0.exe` and specify the path to the Tableau archive and any other option you wish to set. Logshark uses the following syntax:
  
    <code>logshark <i>Target</i> [<i>Options</i>]</code>


    Where *`Target`* represents a zipped archive file (`logs.zip`), directory, or hash value from a previous run. Logshark supports both absolute and relative paths.
    One option that you will likely use is `--startlocalmongo`, unless you are using your own MongoDB instance in place of the one Logshark provides.
    To see all the Logshark command options and syntax, use the `--help` option.
 
    ```   
    logshark --help
    ```



**Examples:**

The following example runs Logshark on the archive file, `logs.zip` and uses the local MongoDB that Logshark provides. Results are available in the `\Output` folder in the location where you installed Logshark.

```
logshark C:\Logs\logs.zip --startlocalmongo
```

The following command directs Logshark to process logs on a file share, and uses the `-p` option to publish the generated workbooks in the default Tableau Server location.

```
logshark \\workgroup\Files\Home\Shared\logs.zip -p --startlocalmongo

```
For more information, see the [Logshark command options](logshark_cmds).




-----------------

### View the generated Tableau workbooks (Desktop)

By default, all workbooks (or other plugin-generated content) are placed in the `\Output` folder in the location where you installed Logshark. 

1.  Navigate to the `\Output` folder (for example, `C:\Program Files\Logshark\Output`). 
    
    Or click **Start** &gt; **All Programs** &gt; **Logshark** &gt; **Logshark** **Output**

    When Logshark processes the log files it creates a new folder for the results in the `\Output` folder. The name of the folder is *`HostName_DateTime_FileName`*, where  *`HostName`* is the name of the computer where Logshark was run. *`DateTime`* is the time stamp that indicates when the logs were processed, and *`FileName`* is the name of the archive file. The folder contains all the workbooks for the plugins that were run in that instance.

2.  Navigate to results folder you are interested in and double-click the Tableau workbook you want to view. 

    For example, if you open the `Apache.twb` workbook, you can view the viz load statistics generated from the Apache log files.     


    ![]({{ site.baseurl }}/assets/SampleScreenshot.png)

   For information about all the plugins and workbooks, see [Logshark Plugins and Generated Workbooks](logshark_plugins)

### Publish and view results on Tableau Server

If you want to publish the workbooks to Tableau Server instead of the default `\Output` folder, you need to modify the `Logshark.config` file and use the `-p` option when you run Logshark. 

1. Modify the Logshark.config file. Change the  `<TableauConnection>` settings to match your Tableau Server configuration.   For information about setting up a Tableau Server, see [Edit the Tableau Server connection information in the Logshark configuration file](logshark_install#edit-the-tableau-server-connection-information-in-the-logshark-configuration-file). 

2. Specify the `-p` or `--publishworkbooks` option when you run Logshark. When you use this option, the workbooks are published on the Tableau Server identified in the `Logshark.config` file.

3.  Navigate to your Tableau Server. 
    The URL for your workbooks would look like the following:  

    <code>http://<i>yourServer</i>/#/site/<i>yourSite</i>/projects   </code>

    The generated workbooks are organized in project folders. The name of a project is  *`HostName_DateTime_FileName`*, where  *`HostName`* is the name of the computer where Logshark was run. *`DateTime`* is the time stamp that indicates when the logs were processed, and *`FileName`* is the name of the archive file. The project contains all the workbooks for the archive file.

4.   Navigate to projects folder you are interested in and double-click the Tableau workbook you want to view. 
     For information about all the plugins and workbooks, see [Logshark Plugins and Generated Workbooks](logshark_plugins)
