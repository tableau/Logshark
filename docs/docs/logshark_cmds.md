---
title: LogShark Command Options
---

Full usage can be viewed at any time by invoking `LogShark --help`.  The only required argument is the log set location, which can be either an archive (zip file), a directory, or a logset hash. 

**LogShark command syntax**

```xml
...
   LogShark <i>LogSetLocation</i> <i>RunId</i> [<i>Options</i>]
...
```
`<LogSetLocation>` - Location of the logs to process (zip file or unzipped folder)

`<RunId>` - Unique identifier to use for output of this run (i.e. SaturdayOutageLogs). If not specified 


| **Options** | Description  |
|-------------|--------------|
 --force-run-id |LogShark prefixes RunId with timestamp even if RunId provided by user. This flag prevents timestamps from being added to user-supplied RunId. In this case you are responsible for providing unique RunId for each run |
| --password | Tableau server password |
| --pg-db-conn-string | Connection string for output database for postgres writer |
| --pg-db-host | Output database hostname for postgres writer |
| --pg-db-name | Output database name for postgres writer |
| --pg-db-pass | Output database password for postgres writer |
| --pg-db-user | Output database username for postgres writer |
| `-plugins all, --plugins <plugin1>;<plugin2>...` | (Default: All) List of plugins to run, to specify more than one plugin, list them separated by semicolon, no spaces. Or "All" to run all applicable plugins. See LogShark plugins and generated workbooks |
| --site | Tableau server site name |
| --url | Tableau server url | 
| --username | Tableau server username | 
| --workbookname <string> | Custom workbook name to append to the end of each workbook generated. | 
| -?, -h, --help | Show help information | 
| -a, --append-to <APPEND_TO>  | Append this run results to the results from specified run id. Implementation varies by output writer. See [Visualize Historic Trends of Your Logs](add link here) for more info | 
| -c, --config <CONFIG>  | Specify alternative config file to use. By default `<LogShark_Install_location.\Config\LogSharkConfig.json` is used | 
| -l, --listplugins | Lists the LogShark plugins available for use with `--plugins` parameter |
| -p, --publishworkbooks | (Default: False) Publish resulting workbooks to Tableau Server | 
| `-w, --writer <WRITER>` | Select type of output writer to use (i.e. "csv", "postgres, "sql", etc) | 
{: .custom-class #custom-id}
 

