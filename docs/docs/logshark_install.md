---
title: Install Logshark
---

In this section:

* TOC
{:toc}



-------------------------------------------------------------------------------------

### Download LogShark
 [![Download Logshark](https://img.shields.io/badge/Download%20Logshark-Version%203.0.2-blue.svg)](https://github.com/tableau/Logshark/releases/download/3.0.2/Setup_Logshark_v3.0.2.exe)

Navigate to a location where you want to install LogShark and unzip the file there.


### Running LogShark from any Directory
If you want to run LogShark from anywhere, you need to add the directory where you unzipped LogShark to your PATH system variable. Here are instructions on how to do it in Windows 10.

1. In Search, search for and then select: **System**.
1. Click the **Advanced system settings** link.
1. Click **Environment Variables**. In the section System Variables, find the `PATH` environment variable and select it. Click **Edit**. If the PATH environment variable does not exist, click **New**.
1. In the **Edit System Variable** (or New System Variable) window, specify the value of the `PATH` environment variable. Click OK. Close all remaining windows by clicking OK.

**NOTE:** The archive logs will need to be unzipped or copied to the drive where you have installed Logshark. If your `C:\` drive is low on space, you should install Logshark in a different location. :question: **TRUE?**


### Configure Logshark 


Logshark uses a configuration file to point to the databases that are used for parsing and storing log data. The configuration file also lets you publish the Tableau workbooks to a specific Tableau Server. The path to the configuration file is <code><i>installation-folder</i>\Config\Logshark.config</code>.

Some of these configuration settings can be overridden at the command line. Other settings can only be set in the configuration file. For example, if you want to specify which Tableau Server to use when publishing workbooks, you need to set that option in the configuration file.

**NOTE:** To be able to publish workbooks to Tableau Server, the REST API option (`api.server.enabled`) must be enabled. See the [REST API Requirements](https://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_requ.htm%3FTocPath%3D_____3){:target="_blank"} article for more details.
