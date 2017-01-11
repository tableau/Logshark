---
title: Logshark Plugins and Generated Workbooks
layout: docs
---

In this section:

* TOC
{:toc}


### Logshark Plugins Syntax 

The following table shows the list of available Logshark plugins and the names of the workbooks that the plugins generate. Specify the name of the plugin with the Logshark **--plugins** option. To specify more than one plugin, use spaces to separate the plugins and enclose the list in quotation marks (" ").

**Usage:**

   <code>logshark <i>Target</i> --plugins <i>plugin</i></code>

   <code>logshark <i>Target</i> --plugins "<i>plugin1</i> <i>plugin2</i> <i>plugin3</i>..."</code>



**Examples:**


     
     logshark logs.zip --plugins Apache
     
     
     logshark logs.zip --plugins "Apache VizqlServer"
            
   
 

| Plugin name            | Workbook                                     | Description  
|------------------------|----------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Apache                 | `Apache.twb`                                   | Collect and analyze workbook statistics on Tableau Server from the Apache (http) log files, including viz load times, view counts, errors, and warnings.                                                                                                                                                                        |
| Backgrounder           | `Backgrounder.twb`                             | Displays information about Tableau Server background tasks and jobs, such as subscriptions and extract refreshes. Data is taken from the backgrounder log files.                                                                                                                                                                |
| ClusterController      | `ClusterController.twb`                        | Displays information about Tableau Server Cluster Controller events and errors, taken from the clustercontroller and zookeeper log files. Also includes some information about disk performance.                                                                                                                                  |
| Config                 | `Config.twb`                                   | Displays the Tableau Server topology and configuration settings from the log files.                                                                                                                                                                                                                                             |
| CustomWorkbooks        | Any custom user workbooks in CustomWorkbooks | Allows a user to output their own custom workbooks as a part of a Logshark run. These workbooks are loaded from the CustomWorkbooks\\ folder. Plugin dependencies for each workbook can specified in CustomWorkbookConfig.xml. For more information, see [Adding your own custom workbooks](logshark_customwbk). |
| Netstat                | `Netstat.twb`                                  | Displays information about transport-layer port reservations taken from the Netstat output files in a Server logset. The ziplogs must have been taken with the `–n` argument in order to contain Netstat data.                                                                                                                |
| ResourceManager        | `ResourceManager.twb`                          | Shows information harvested from the Server Resource Manager log events. Workbook includes metrics on CPU utilization, memory utilization, and process recycling events.                                                                                                                                                        |
| Tabadmin               | `Tabadmin.twb`                                 | Displays Tableau Server admin (tabadmin) activities from the log files, including Tableau Server starts, stops, backup, and error history.                                                                                                                                                                                      |
| VizqlDesktop           | `VizqlDesktop.twb`                             | Collect and analyze events from Tableau Desktop vizql log files, including Vizql events, query activity, and errors.                                                                                                                                                                                                            |
| VizqlServer            | `VizqlServer.twb`                              | Collects high-level session summary information from Tableau vizqlserver log files, including error data.                                                                                                                                                                                                                       |
| VizqlServerPerformance | `VizqlServerPerformance.twb`                   | Collect and analyze all events from Tableau Server vizqlserver log files, including detailed performance information.                                                                                                                                                                                                           |

--------

### Appending Logshark-generated Data to the Same Workbook


When you run Logshark using the default command options, Logshark generates a new PostgreSQL database for each run. If you want to append data to the same database, so that the data will all be available in the same workbook, you can specify the name of the database on the command line, using the <code>--dbname <i>database</i></code> option. You can use this option to set a custom name for the database where the plugin output is stored (the data source for the plugin workbook).


**Example**

```
logshark C:\Logs\logs.zip --plugins Apache --dbname myApacheData
```

If the `–-dbname` option is not specified, a new database will be generated.