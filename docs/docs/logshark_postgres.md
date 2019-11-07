---
title: Visualize Historic Trends of Your Logs
---
In the previous version LogShark we are bringning back the ability to  visualize historic trends view of your logs. You can either write LogShark's output to a PostgreSQL database server or append data from a new log set to an existing LogShark output.

In this section:

* TOC
{:toc}

-----------

## Write Output to PostgreSQL
To write LogShark's output to PostgreSQL database you need to provide the connection string and ensure the chosen user has the necessary permissions. LogShark will handle creating the database, tables, columns, and inserting all the extracted data from your Tableau logs.

### System Requirements
You will need:

- System Requirements for [LogShark](\docs\LogShark_install.md)
- PostgreSQL v9.6
  - This feature has been tested to be compatible with PostgreSQL v9.6. Additional testing is underway.
  - Download and install PostgreSQL on your machine

## Required permissions for the user 

`CREATE`

- Database: LogShark will create the database if it doesn’t exist. This permission is not necessary if the database already exists.
- Schema: If the specified schema doesn’t exist in the database, LogShark will create it.
Tables: LogShark will create the tables necessary to store the data extracted from your logs.

`SELECT`

- pg_catalog: Queried to determine if the database exists.
- information_schema: Queried to determine if the necessary columns exist for LogShark to store the data extracted from your logs.

`ALTER`

- Table: LogShark will add the columns necessary to store the data extracted you’re your logs.

`INSERT`

- LogShark needs to be able to insert the data it extracted from your Tableau logs!

### Connection Setup
To direct LogShark to write to PostgreSQL database, you need to update settings. You can do it in config file or command line. 

#### Config File
To update config settings, navigate to config file `<LogShark_install_location>\Config\LogSharkConfig.json` and update the following fields inside the `PostgresWriterDatabase` group. Note that each field above supersedes values from previous fields. For example, a value supplied in the `DatabaseName` field will override the database name supplied in the `ConnectionString` field.


```xml
"PostgresWriterDatabase": {   
   "Host": "localhost",
   "DatabaseName": "myDataBase",
   "Username": "myUserName",
   "Password": "myPassword",
   "ConnectionString": "",
   "ServiceDatabaseName": "",
   "BatchSize": 100,
   "ConnectionTimeoutSeconds": 30
   ```


We recommend to use the `ConnectionString` field, as LogShark will just use the supplied value verbatim. However, if you want to supply the values piecemeal, feel free to use the fields above.

```xml
"ConnectionString": "User ID=myUserName;Password=myPassword;Host=localhost;Port=5432;Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;",
```

#### Command Line
If you don't want to store username and password in config file, you can use command line to specify them. See full list of available command parameters below.

```
LogShark <LogSetLocation> <RunId> --writer postgres --pg-db-user "myUserName" --pg-db-pass "myPassword"
```

If either a `ConnectionString`, `Username`, or a `Password` are not provided, then LogShark assumes you want to use Integrated Security. 


#### Write Output to PostgreSQL
1.  Open a Command Prompt window (Windows) or Terminal (mac)
1.  To see the LogShark command options and syntax, enter LogShark --help
1.  To process a logset, just run LogShark and specify the path to the log file and any other option you wish to set. 

Here is a syntax reference for invoking PostgreSQL command in LogShark.

```
LogShark <LogSetLocation> <RunId> --writer postgres
```

#### PostgreSQL Commands
Each of the fields for the configuration may be supplied as a command line argument. This can be beneficial if you wish to avoid storing user credentials as plain text inside LogSharkConfig.json. You may mix and match supplying connection information between the config file and command line arguments (for example, supply a connection string with a placeholder password inside the config file, and supply the actual password as a command line argument). For each field, any value supplied as a command line argument supersedes the value supplied in the config file. 


| Command | Description|
|---------|------------|
| -w,--writer <WRITER>  | Select type of output writer to use (i.e. "csv", "postgres", "sql", etc) |
|--pg-db-conn-string | Connection string for output database for postgres writer | 
| --pg-db-host  | Output database hostname for postgres writer | 
| --pg-db-name  | Output database name for postgres writer | 
| --pg-db-user  | Output database username for postgres writer | 
|  --pg-db-pass | Output database password for postgres writer | 


#### Results
The data from the run is saved in the PostgreSQL database specified in the config or command line paramaters. 

All workbooks are saved in an `\<LogShark_run_location>\Output\<RunID>\woorkbooks` folder the directory from where LogShark is run. If the folder doesn't exist, LogShark creates it. The workbooks in that folder are connected to the Postgres database you specified when you ran LogShark. When you open the workbook, you will be asked to provide your Postgres credentials.

#### Updates
LogShark is an active project, so it’s possible that different versions may have different output schema. Despite this, LogShark will never remove data, columns, tables, schema, or databases. LogShark only ever creates the schema necessary to store data for its current execution. This means it’s possible that one version may create a table and/or column which is unused in subsequent versions. These extra table/columns do not impact LogShark’s ability to extract data. LogShark simply ignores unused schema.

----

### Append Results to a Previous Run
Another way to see historic trends in the same viz is to use an `append` command to append data from a new log set to an existing LogShark output. The section below describes how to do it.


#### Append command reference
Here is a syntax reference for invoking the append command in LogShark.

```
LogShark <LogSetLocation> <RunId> --append-to <RunId_Of_The_Run_To_Append_To>
```


#### How it works
The way LogShark normally works is it creates a new output folder and a new empty extract files at the beginning of the run and then pushes data into those empty extracts. When `--append-to` parameter is specified, LogShark does the following instead:

1.  Create new empty folder for run output
1.  Locate output folder for the run id specified by `--append-to` parameter
1.  Copy existing extracts into the new output folder
Open existing extracts in an append mode (if extract is missing – i.e. more plugins requested than before – new empty extract will be created)
1.  From this point on LogShark proceeds as usual – reading data from logs and pushing results into hyper extracts

#### Things to note
- Results from the original run are copied into the new output folder before run. This means that “output” folder of the original run stays unchanged.
- LogShark does not look into previous results in any way, nor does it have filtering capabilities for output. This means that if original and appended log sets have overlapping data – resulting extracts will have duplicate rows.
- Few unknowns we haven't had a chance to test yet:
  - It is very likely that if we have to change data model for any specific data set (plugin) in the future - it will be impossible to append to the respective extract(s) after upgrading to the new version of the LogShark. Adding new data sets/plugins should not be a problem though.
  - Extract files can get quite large over time. We had 22GB extract from one log set and Hyper did not seem to mind, but there might be a certain size threshold after which Hyper and/or OS start to act up

####  Adding several runs 
LogShark appends one time at-a-time. It is possible to combine more than two runs, however you would need to “chain” runs in the right order, i.e. first process log set A, then append log set B to the results of A, then append log set C to the results of B, etc. Here are the steps:

1. Run LogShark on the first log set as usual
1. Run LogShark on the second log set while adding `--append-to <run_id_of_the_first_run>` parameter
1. Run LogShark on the third log set while adding `--append-to <run_id_of_the_second_run>` parameter.