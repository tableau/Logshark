---
title: LogShark Plugins and Generated Workbooks
---

In this section:

* TOC
{:toc}


### LogShark Plugins Syntax 

The following table shows the list of available LogShark plugins and the names of the workbooks that the plugin generates. You can preview workbooks before running LogShark by navigating to the folder `<LogShark_install_location>\Workbooks` and opening up the workbooks in Tableau. 

To run a specific plugin, specify the name of the plugin with the LogShark **`--plugins`** option. To specify more than one plugin, list them separated by a semicolon, no spaces, and enclose the list in quotation marks (“ “).


**Usage:**

   <code>LogShark <i>LogSetLocation</i> <i>RunId</i></code>

   <code>LogShark <i>LogSetLocation</i> <i>RunId</i> --plugins <i>plugin</i></code>

   <code>LogShark <i>LogSetLocation</i> <i>RunId</i> --plugins "<i>plugin1</i> <i>plugin2</i> <i>plugin3</i>..."</code>



**Examples:**

```
     LogShark logs.zip --plugins Apache
     LogShark logs.zip --plugins "Apache;VizqlServer"
            
   
 

| Plugin name            | Workbook                                     | Description  
|------------------------|----------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Art | `Art.twbx`  | Analyze VizQLServer Activity Resource Tracing information for performance details of view loads. Similar to ServerTelemetry plugin. |

| Apache                 | `Apache.twbx`                                   | Collect and analyze workbook statistics on Tableau Server from the Apache (http) log files, including viz load times, view counts, errors, and warnings.                                                                                                                                                                        |
| Backgrounder           | `Backgrounder.twbx`                             | Displays information about Tableau Server background tasks and jobs, such as subscriptions and extract refreshes. Data is taken from the backgrounder log files.                                                                                                                                                                |
| ClusterController      | `ClusterController.twbx`                        | Displays information about Tableau Server Cluster Controller events and errors, taken from the clustercontroller and zookeeper log files. Also includes some information about disk performance.                                                                                                                                  |
| Config                 | `Config.twbx`                                   | Displays the Tableau Server topology and configuration settings from the log files.

| Filestore              | `Filestore.twbx`                                | Displays information about Tableau Server File Store events and errors.                                                                                                              |
| Hyper                | `Hyper.twbx`                                  | Displays information about Hyper activity, including extract generation and extract query details.                                                                                           |
| Netstat                | `Netstat.twbx`                                  | Displays information about transport-layer port reservations taken from the Netstat output files in a Server logset. The ziplogs must have been taken with the `–n` argument in order to contain Netstat data.                                                                                                                |
| Postgres               | `Postgres.twbx`                                 | Displays information about Tableau Server Repository events and errors, including application query details.

| ResourceManager        | `ResourceManager.twbx`                          | Shows information harvested from the Server Resource Manager log events. Workbook includes metrics on CPU utilization, memory utilization, and process recycling events.                                                                                                                                                        |
| SearchServer           | `SearchServer.twbx`                             | Displays information about Tableau Server Search & Browser service, including search indexing events.                                                                                                              |
| Tabadmin               | `Tabadmin.twbx`                                 | Displays Tableau Server admin (tabadmin) activities from the log files, including Tableau Server starts, stops, backup, and error history.                                                                                                                                                                                      |
| VizPortal              | `Vizportal.twbx`                                | Displays information about Tableau Server Application Server events, such as authentication or API issues.                                                                                                              |
| VizqlDesktop           | `VizqlDesktop.twbx`                             | Collect and analyze events from Tableau Desktop vizql log files, including Vizql events, query activity, and errors.     |                                                                                       