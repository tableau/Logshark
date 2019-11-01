---
title: LogShark Installation and User Guide
---
*Analyze your Tableau log files – in Tableau!*

LogShark is a tool you can use to analyze and troubleshoot Tableau performance and activity. LogShark extracts data from Tableau Server and Tableau Desktop log files and builds workbooks that can help you identify and understand error conditions, performance bottlenecks, and background activity.

LogShark works by running a set of targeted plugins that pull specific data out of the log files. LogShark builds a data source and provides Tableau workbooks which you can then use to analyze the log files in Tableau.

**NOTE**: This version of LogShark is significantly faster and brings back the ability to write to a PostgreSQL database server, as well as a number of other improvements. See [releases page](https://github.com/tableau/Logshark/releases/latest) for a full list of updates.

This installation and user guide will walk you through the steps to get started, how to run LogShark and view the results, and a command dictionary.

<!--
[Second page]({{ site.baseurl }}/second-page).
-->

<!--
In this section:

* TOC
{:toc}

-->

**Ready to get set up?**

- [Get your Computer Set Up For LogShark](docs/logshark_prefunc.md)
    - [If you are running macOS Catalina](docs/logshark_catalina.md)
- [Install LogShark](docs/logshark_install.md)

**Ready to roll?**

- [Run LogShark](docs/logshark_run.md)
- [Configure and Customize](docs/logshark_configure.md)
- [Publish to Server](docs/logshark_server.md)
- [View Historic Trends (Output to Postgres)](docs/logshark_postgres.md)

**Reference**
- [LogShark Command Options](docs/logshark_cmds.md)
- [LogShark Plugins and Generated Workbooks](docs/logshark_plugins.md)



### LogShark and Tableau Technical Support
 
LogShark is released as a [Community-Supported](https://www.tableau.com/support/itsupport) tool. It is intended to be a self-service tool and includes this user guide. Any bugs discovered should be filed in the [LogShark Git Issue tracker](https://github.com/tableau/Logshark/issues).
