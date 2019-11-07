---
title: Install Logshark
---

In this section:

* TOC
{:toc}

----

Download LogShark
------------------
 [![Download Logshark](https://img.shields.io/badge/Download%20Logshark-Version%203.0.2-blue.svg)](https://github.com/tableau/Logshark/releases/download/3.0.2/Setup_Logshark_v3.0.2.exe)

Navigate to a location where you want to install LogShark and unzip the file there.


Running LogShark from any Directory (add to PATH)
-------------------
If you want to run LogShark from anywhere, you need to add the directory where you unzipped LogShark to your PATH system variable. Here are instructions on how to do it in Windows 10.

1. In Search, search for and then select: **System**.
1. Click the **Advanced system settings** link.
1. Click **Environment Variables**. In the section System Variables, find the `PATH` environment variable and select it. Click **Edit**. If the PATH environment variable does not exist, click **New**.
1. In the **Edit System Variable** (or New System Variable) window, specify the value of the `PATH` environment variable. Click OK. Close all remaining windows by clicking OK.

**NOTE:** By default LogShark saves the resulting workbooks in an `Output` folder in the directory from where it was ran. If you want to modify that, check [Configure and Customize Logshark](/docs/logshark_configure.md).
