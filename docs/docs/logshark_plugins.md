---
title: LogShark Plugins and Generated Workbooks
---

In this section:

* TOC
{:toc}


### LogShark Plugins Syntax 

The following table shows the list of available LogShark plugins and the names of the workbooks that the plugin generates. You can preview workbooks before running LogShark by navigating to the folder `<LogShark_install_location>\Workbooks` and opening up the workbooks in Tableau. 

To run a specific plugin, specify the name of the plugin with the LogShark **`--plugins`** option. To specify more than one plugin, list them separated by a semicolon, no spaces, and enclose the list in quotation marks (“ “).

If the `--plugins` arg is not specified, all plugins run by default, **except** any plugins listed in the `PluginsToExcludeFromDefaultSet` config setting. Out of the box, this excludes the `Replayer` plugin, which must be requested explicitly (`--plugins Replayer`).


**Usage:**

   <code>LogShark <i>LogSetLocation</i> <i>RunId</i></code>

   <code>LogShark <i>LogSetLocation</i> <i>RunId</i> --plugins <i>plugin</i></code>

   <code>LogShark <i>LogSetLocation</i> <i>RunId</i> --plugins "<i>plugin1</i> <i>plugin2</i> <i>plugin3</i>..."</code>



**Examples:**

```
     LogShark logs.zip --plugins Apache
     LogShark logs.zip --plugins "Apache;ServerTelemetry"
```
            
   
 

| Plugin name            | Workbook                                     | Description  
|------------------------|----------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Art | `Art.twbx` (server), `ArtDesktop.twbx` (desktop)  | Analyze VizQLServer Activity Resource Tracing information for performance details of view loads. Similar to ServerTelemetry plugin. See the [Art workbook guide]({{ site.baseurl }}/docs/logshark_art) for details. |
| Apache                 | `Apache.twbx`                                   | Collect and analyze workbook statistics on Tableau Server from the Apache (http) log files, including viz load times, view counts, errors, and warnings. |
| Backgrounder           | `Backgrounder.twbx`                             | Displays information about Tableau Server background tasks and jobs, such as subscriptions and extract refreshes. Data is taken from the backgrounder log files.                                                                                                                                                                |
| Bridge                 | `Bridge.twbx`                                   |Displays information for Bridge logs and jobs processed by Bridge client. Data is taken from every Bridge client to analyse the issues.                                                                                                                                                                                       |
| ClusterController      | `ClusterController.twbx`                        | Displays information about Tableau Server Cluster Controller events and errors, taken from the clustercontroller and zookeeper log files. Also includes some information about disk performance.                                                                                                                                  |
| Config                 | `Config.twbx`                                   | Displays the Tableau Server topology and configuration settings from the log files.  |
| DataServer             | `DataServer.twbx`                               | Displays information about Tableau Server DataServer process activity (Java and C++ logs) for published data sources, including query and connection ("protocol creation") performance. See the [DataServer workbook guide]({{ site.baseurl }}/docs/logshark_dataserver) for details. |
| Filestore              | `Filestore.twbx`                                | Displays information about Tableau Server File Store events and errors.                                                  |
| Hyper                | `Hyper.twbx`                                  | Displays information about Hyper activity, including extract generation and extract query details.                       |
| Netstat                | `Netstat.twbx`                                  | Displays information about transport-layer port reservations taken from the Netstat output files in a Server logset. The ziplogs must have been taken with the `–n` argument in order to contain Netstat data.                                                                                                                |
| Postgres               | `Postgres.twbx`                                 | Displays information about Tableau Server Repository events and errors, including application query details. |
| Prep                   | `Prep.twbx`                                     | Displays information about Tableau Prep flow runs and steps, taken from Tableau Prep log files. |
| Replayer               | *(none — no workbook generated)*                | Generates a replayable session script from Apache and VizqlServer log files, used to replay/simulate browser session traffic against a Tableau Server. Not part of the default plugin set — must be requested explicitly with `--plugins Replayer`. |
| ResourceManager        | `ResourceManager.twbx`                          | Shows information harvested from the Server Resource Manager log events. Workbook includes metrics on CPU utilization, memory utilization, and process recycling events. |
| SearchServer           | `SearchServer.twbx`                             | Displays information about Tableau Server Search & Browser service, including search indexing events.                                        |
| ServerTelemetry        | `ServerTelemetry.twbx`                          | Displays Tableau Server view load performance based on VizqlServer telemetry events and metrics (native JSON logs). Similar to the Art plugin. |
| Tabadmin               | `Tabadmin.twbx`                                 | Displays Tableau Server admin (tabadmin) activities from the log files, including Tableau Server starts, stops, backup, and error history.                                                                                                                                                                                      |
| TabadminController     | `TabadminController.twbx`                       | Displays Tableau Services Manager (TSM) job timelines, errors/warnings, admin user activity, and maintenance logs, taken from the tabadmincontroller, tabadminagent, and process control log files. See the [TabadminController workbook guide]({{ site.baseurl }}/docs/logshark_tabadmincontroller) for details. |
| Vizportal              | `Vizportal.twbx`                                | Displays information about Tableau Server Application Server events, such as authentication or API issues.                                                                                                              |
| VizqlDesktop           | `VizqlDesktop.twbx`                             | Collect and analyze events from Tableau Desktop vizql log files, including Vizql events, query activity, and errors.     |
