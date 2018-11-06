---
title: Logshark Plugins and Generated Workbooks
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
| Apache                 | `Apache.twbx`                                   | Collect and analyze workbook statistics on Tableau Server from the Apache (http) log files, including viz load times, view counts, errors, and warnings.                                                                                                                                                                        |
| Backgrounder           | `Backgrounder.twbx`                             | Displays information about Tableau Server background tasks and jobs, such as subscriptions and extract refreshes. Data is taken from the backgrounder log files.                                                                                                                                                                |
| ClusterController      | `ClusterController.twbx`                        | Displays information about Tableau Server Cluster Controller events and errors, taken from the clustercontroller and zookeeper log files. Also includes some information about disk performance.                                                                                                                                  |
| Config                 | `Config.twbx`                                   | Displays the Tableau Server topology and configuration settings from the log files.                                                                                                                                                                                                                                             |
| CustomWorkbooks        | Any custom user workbooks in CustomWorkbooks | Allows a user to output their own custom workbooks as a part of a Logshark run. These workbooks are loaded from the CustomWorkbooks\\ folder. Plugin dependencies for each workbook can specified in CustomWorkbookConfig.xml. For more information, see [Adding your own custom workbooks](logshark_customwbk). |
| Filestore              | `Filestore.twbx`                                | Displays information about Tableau Server File Store events and errors.                                                                                                              |
| Hyper                | `Hyper.twbx`                                  | Displays information about Hyper activity, including extract generation and extract query details.                                                                                           |
| Netstat                | `Netstat.twbx`                                  | Displays information about transport-layer port reservations taken from the Netstat output files in a Server logset. The ziplogs must have been taken with the `–n` argument in order to contain Netstat data.                                                                                                                |
| Postgres               | `Postgres.twbx`                                 | Displays information about Tableau Server Repository events and errors, including application query details.                                                                                                             |
| ReplayCreation        | NA                          | Optional silent plugin that can play back real traffic, it replays Tableau Server single or multi-user sessions. Replay reconstructs the URL access and interactions on the viz using Tableau Server logs. Instructions and installation guide can be found on [Tableau Community](https://community.tableau.com/docs/DOC-11048).                                                                                                                                                        |
| ResourceManager        | `ResourceManager.twbx`                          | Shows information harvested from the Server Resource Manager log events. Workbook includes metrics on CPU utilization, memory utilization, and process recycling events.                                                                                                                                                        |
| SearchServer           | `SearchServer.twbx`                             | Displays information about Tableau Server Search & Browser service, including search indexing events.                                                                                                              |
| Tabadmin               | `Tabadmin.twbx`                                 | Displays Tableau Server admin (tabadmin) activities from the log files, including Tableau Server starts, stops, backup, and error history.                                                                                                                                                                                      |
| VizPortal              | `Vizportal.twbx`                                | Displays information about Tableau Server Application Server events, such as authentication or API issues.                                                                                                              |
| VizqlDesktop           | `VizqlDesktop.twbx`                             | Collect and analyze events from Tableau Desktop vizql log files, including Vizql events, query activity, and errors.                                                                                                                                                                                                            |
| VizqlServer            | `VizqlServer.twbx`                              | Collects high-level session summary information from Tableau vizqlserver log files, including error data.                                                                                                                                                                                                                       |
| VizqlServerPerformance | `VizqlServerPerformance.twbx`                   | Collect and analyze all events from Tableau Server vizqlserver log files, including detailed performance information.                                                                                                                                                                                                           |

