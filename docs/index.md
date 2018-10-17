---
title: Logshark Installation and User Guide
---
*Analyze your Tableau log files – in Tableau!*

Logshark is a tool you can use to analyze and troubleshoot Tableau performance and activity. Logshark extracts data from Tableau Server and Tableau Desktop log files and builds workbooks that can help you identify and understand error conditions, performance bottlenecks, and background activity. Logshark works by running a set of targeted plugins that pull specific data out of the log files. Logshark builds a data source and provides Tableau workbooks which you can then use to analyze the log files in Tableau.

This installation and user guide will walk you through the steps to get started, how to run Logshark and view the results, and a command dictionary. 

<!--
[Second page]({{ site.baseurl }}/second-page).
-->

<!--
In this section:

* TOC
{:toc}

-->

**Ready to get set up?**

- [Get your Computer Set Up for Logshark](docs/logshark_prefunc)
- [Install Logshark](docs/logshark_install)

**Ready to roll?**

- [Run Logshark and View the Results](docs/logshark_run)


NOTE: In the latest release of Logshark, the run outputs are saved as hyper extracts instead of using PostgreSQL. Not only does Logshark no longer needs a dedicated PostgreSQL database, but it also runs significantly faster. The results of the Logshark run are now saved as hyper extracts embedded with the workbook (.twbx), which enables the vizzes to load much faster.

### Logshark and Tableau Technical Support
 
Logshark is made available AS-IS with **no** support from Tableau Technical Support. This is intended to be a self-service tool and includes this user guide. Any bugs discovered should be filed in the [Logshark Git Issue tracker](https://github.com/tableau/Logshark/issues){:target="_blank"}.

