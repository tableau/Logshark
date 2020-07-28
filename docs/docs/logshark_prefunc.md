---
title: Get your Computer Set Up for LogShark
---

Before you install and run LogShark on your computer, you'll need to make sure your system meets the requirements. This section will walk you through them:

* TOC
{:toc}



System Requirements
-------------------
**For Windows**:
-   A computer running a 64-bit version of Windows (2008 R2 or later).

-   An account with local administrator permissions on the computer where you will be installing LogShark.

-   Hyper API for C++. If Hyper requirements are not met on the machine, LogShark will fail. The simplest way to meet Hyper requirements is to install Tableau Desktop on your machine. 
    - If you don't have Tableau Desktop installed, you will need Visual C++ Redistributables. Download x64 version of "Visual Studio 2015, 2017 and 2019" package from [https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads){:target="_blank"}

-   For best performance, use a computer with the latest hardware and software available. The ability of LogShark to process log files improves as the performance of the computer's CPU, Memory, and Disk I/O increases.

-   Tableau Desktop version 10.5 (or later) to view workbooks. You can download Tableau from: [http://www.tableau.com/products/desktop](http://www.tableau.com/products/desktop){:target="_blank"}

**For macOS:** 
- LogShark runs on macOS versions 10.12 "Sierra", 10.13 "High Sierra, and 10.14 "Mojave". 
- Please note that 10.15 "Catalina" implemented new security features that are currently interfering with LogShark.


Tableau Log Requirements
--------------------------------

The archive log files must be from Tableau Server or Tableau Desktop version 9.0 or later. LogShark requires that the Tableau Server log files that you process are compressed (zipped) files, also known as *archive* files or *snapshots*.

### Server Logs
**USING TSM**
You can create these archive files using Tableau Services Manager (TSM) web interface or TSM CLI `tsm maintenance ziplogs` command on the Tableau Server. For more information about gathering Tableau Server log files using TSM, see [Archive Log Files](https://onlinehelp.tableau.com/current/server/en-us/logs_archive.htm){:target="_blank"}.

**USING TABADMIN**
You can create these archive files using the `tabadmin ziplogs` command on the Tableau Server, or by creating a snapshot from the Status or Maintenance menu within Tableau Server. For more information about gathering Tableau Server log files using `tabadmin`, see [Archive Log Files](http://onlinehelp.tableau.com/v2018.1/server/en-us/logs_create.htm){:target="_blank"}.

### Desktop Logs
For Tableau Desktop, the log files are located in the `My Tableau Repository` directory. The default location is <code>\Users\<i>username</i>\Documents\My Tableau Repository\Logs</code>. You can also find the location using Tableau. Start Tableau Desktop and click **File &gt; Repository** **Location**.

After you locate the log files, you can copy them to another location to process, or specify the path to their current location when you run LogShark.
