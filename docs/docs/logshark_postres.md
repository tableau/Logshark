---
title: Visualize Historic Trends of Your Logs
---
In the previous version LogShark could append multiple log sets to visualize historic trends view of your logs on the same viz. When we switched from Postgres to Hyper extracts, LogShark lost that functionality. We’ve received multiple request to implement this feature in LogShark both internally and externally. Now, you can either write LogShark's output to a PostgreSQL database or append data from a new log set to an existing LogShark output. 

In this section:

* TOC
{:toc}

-----------

## Write Output to PostgreSQL
LogShark can write its output to a PostgreSQL database server. Simply provide the connection string, ensure the chosen user has the necessary permissions, and LogShark will handle creating the database, tables, columns, and inserting all the extracted data from your Tableau logs.

### System Requirements
You will need:

- System Requirements for [LogShark](\docs\logshark_install.md)
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

Connection Setup
To direct LogShark to write to PostgreSQL database, you need to update settings. You can do it in config file or command line. 

Config File
To update config settings, navigate to config file <LogShark_install_location>\Config\LogSharkConfig.json and update the following fields inside the PostgresWriterDatabase group. Note that each field above supersedes values from previous fields. For example, a value supplied in the DatabaseName field will override the database name supplied in the ConnectionString field.


Config
"PostgresWriterDatabase": {   
   "Host": "localhost",
   "DatabaseName": "myDataBase",
   "Username": "myUserName",
   "Password": "myPassword",
   "ConnectionString": "",
   "ServiceDatabaseName": "",
   "BatchSize": 100,
   "ConnectionTimeoutSeconds": 30


We recommend to use the ConnectionString field, as LogShark will just use the supplied value verbatim. However, if you want to supply the values piecemeal, feel free to use the fields above.

Config ConnectionString
"ConnectionString": "User ID=myUserName;Password=myPassword;Host=localhost;Port=5432;Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;",

Command Line
If you don't want to store username and password in config file, you can use command line to specify them. See full list of available command parameters below.

Command Line
LogShark <LogSetLocation> <RunId> --writer postgres --pg-db-user "myUserName" --pg-db-pass "myPassword"

If either a ConnectionString, Username, or a Password are not provided, then LogShark assumes you want to use Integrated Security. 



Write Output to PostgreSQL
Open a Command Prompt window (Windows) or Terminal (mac)
To see the LogShark command options and syntax, enter LogShark --help
To process a logset, just run LogShark and specify the path to the log file and any other option you wish to set. 

Here is a syntax reference for invoking PostgreSQL command in LogShark.

Postgres
LogShark <LogSetLocation> <RunId> --writer postgres
PostgreSQL Commands
Each of the fields for the configuration may be supplied as a command line argument. This can be beneficial if you wish to avoid storing user credentials as plain text inside LogSharkConfig.json. You may mix and match supplying connection information between the config file and command line arguments (for example, supply a connection string with a placeholder password inside the config file, and supply the actual password as a command line argument). For each field, any value supplied as a command line argument supersedes the value supplied in the config file. 



-w|--writer <WRITER>
Select type of output writer to use (i.e. "csv", "postgres", "sql", etc)

--pg-db-conn-string
Connection string for output database for postgres writer

--pg-db-host
Output database hostname for postgres writer

--pg-db-name
Output database name for postgres writer

--pg-db-user 
Output database username for postgres writer

--pg-db-pass
Output database password for postgres writer



### Update Configurations


1. You will need to update the `Logshark.config` file. Change the  `<TableauServer>` settings to match your Tableau Server configuration.

```
  "TableauServer": {
    "Url": "<EnterServerUrlHere>",
    "Site": "<EnterSiteNameHere>",
    "Username": "<EnterUsernameHere>",
    "Password": "<EnterPasswordHere>",
    "Timeout": 240,
    "GroupsToProvideWithDefaultPermissions": [ "All Users" ],
    "ParentProject": {
      "Id": "",
      "Name": ""
    }
```

1. The Server `Url` attribute should just contain the hostname or IP address of the computer (for example, *myTableauServer.tableau.com*), and should not be prefixed with the protocol (*http or https*).

1.   When using a non-standard port for your Tableau Server, ensure the `port` attribute is set correctly (by default, HTTP uses port **80** and HTTPS uses port **443**). **do we need this?**

1.   The `site` attribute cannot be blank. If you are using the default site (for example, `http://localhost/#`), specify **Default** as the name (`site="Default"`).

1.   To publish workbooks, the user account you specify must exist on the Tableau Server (and the site) with Publisher permissions and the permissions to create projects. (*Site Administrator role will be the easiest option*).

1. If you don't want to store username and password in the config file, you can use command line to specify them. See full list of the available command parameters on [LogShark Command Options](/docs/logshark_cmds).

```
Command Line
LogShark <LogSetLocation> <RunId> --publishworkbooks --username "myUserName" --password "myPassword"
```

2. Specify the `-p` or `--publishworkbooks` option when you run Logshark. 

```
logshark logs.zip --publishworkbooks --username "myUserName" --password "myPassword"
```

3.  Navigate to your Tableau Server. The URL for your workbooks would look like the following:  

    <code>http://<i>yourServer</i>/#/site/<i>yourSite</i>/projects   </code>

    The generated workbooks are organized in project folders. The name of a project is  *`HostName_DateTime_FileName`*, where  *`HostName`* is the name of the computer where Logshark was run. *`DateTime`* is the time stamp that indicates when the logs were processed, and *`FileName`* is the name of the archive file. The project contains all the workbooks for the archive file. **Need to check and update**

4.   Navigate to projects folder you are interested in and double-click the Tableau workbook you want to view. 
     For information about all the plugins and workbooks, see [Logshark Plugins and Generated Workbooks](logshark_plugins)



    ```xml
    ...
    <TableauConnection protocol="http" publishingTimeoutSeconds="300">
        <Server address="myTableauServer" port="80" site="MySite"/>
        <User username="myUser" password="myUserPassword"/>
    </TableauConnection>
    ...
    ```