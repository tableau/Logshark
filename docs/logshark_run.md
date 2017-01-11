---
title: Run Logshark and View the Results
layout: docs
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

### View the generated Tableau workbooks:


-   You can view all workbooks (or other plugin-generated content) by navigating to the `\Output` folder in the location where you installed Logshark. 

-   You can also click **Start** &gt; **All Programs** &gt; **Logshark** &gt; **Logshark** **Output**


-   If you want to publish the workbooks to Tableau Server, you can use use the `-p` option when you run Logshark. The workbooks are published on the Tableau Server you specify in the `Logshark.config` file. See [Edit the Tableau Server connection information in Logshark.config](logshark_install#edit-the-tableau-server-connection-information-in-logshark.config). The URL for your workbooks would look like the following:  
<code>http://<i>yourServer</i>/#/site/<i>yourSite</i>/projects   </code>

    The format of a project is  *`HostName_DateTime_FileName`*, where  *`HostName`* is the name of the computer where Logshark was run and *`FileName`* is the name of the archive file. The project contains all the workbooks for the archive file.

   

    ![]({{ site.baseurl }}/assets/SampleScreenshot.png)
