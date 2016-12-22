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
  
    `logshark [target] [options]` 


    Where `[target]` represents a zipped archive file (`logs.zip`), directory, or hash value from a previous run. Logshark supports both absolute and relative paths.
    One option that you will likely use is `--startlocalmongo`, unless you are using your own MongoDB instance in place of the one Logshark provides.
    To see all the Logshark command options and syntax, use the `--help` option.
    
    `logshark --help`




**Examples:**

The following example runs Logshark on the archive file, `logs.zip` and uses the local MongoDB that Logshark provides. Results are available in the *\<Logshark_install_location>*`\Output` directory.

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


-   All workbooks (or other plugin-generated content) are saved in the following directory:

    *\<Logshark_install_location>*`\Output`

    You can also click **Start** &gt; **All Programs** &gt; **Logshark** &gt; **Logshark** **Output**

-   If you want to publish the workbooks to Tableau Server using the `-p` option, the workbooks are published on the Tableau Server you specify in the `Logshark.config` file. See [Edit the Tableau Server connection information in Logshark.config](logshark_install#edit-the-tableau-server-connection-information-in-logshark.config). The URL for your workbooks would look like the following: `http://<yourServer/#/site/<yourSite>/projects`.
    The format of a project is *\<Host_Name>*`_DATETIME_`*\<Filename>*, where *&lt;Host\_Name&gt;* is the name of the computer where Logshark was run and *\<Filename>* is the name of the archive file. The project contains all the workbooks for the archive file.


    ![]({{ site.baseurl }}/assets/SampleScreenShot.png)