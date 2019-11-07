---
title: Install LogShark
---

In this section:

* TOC
{:toc}

----

Download LogShark
------------------
[![Download LogShark for Win](https://img.shields.io/badge/Download%20LogShark%20for%20Win-Version%204.1-blue.svg)](https://github.com/tableau/Logshark/releases/download/v4.1/LogShark.Win.4.1.1911.09672-public.zip)

[![Download LogShark for macOS](https://img.shields.io/badge/Download%20LogShark%20for%20macOS-Version%204.1-blue.svg)](https://github.com/tableau/Logshark/releases/download/v4.1/LogShark.Mac.4.1.1911.09672-public.zip)

Navigate to a location where you want to install LogShark and unzip the file there.


Running LogShark from any Directory (add to PATH)
-------------------
If you want to run LogShark from anywhere, you need to add the directory where you unzipped LogShark to your PATH system variable. Here are instructions on how to do it in Windows 10.

1. In Search, search for, and then select: **System**.
1. Click the **Advanced system settings** link.
1. Click **Environment Variables**. In the section System Variables, find the `PATH` environment variable and select it. Click **Edit**. If the PATH environment variable does not exist, click **New**.
1. In the **Edit System Variable** (or New System Variable) window, specify the value of the `PATH` environment variable. Click OK. Close all remaining windows by clicking OK.

**NOTE:** By default, LogShark saves the resulting workbooks in an `Output` folder in the directory from where it was ran. If you want to modify that, check <a href="logshark_configure">Configure and Customize LogShark</a>.
