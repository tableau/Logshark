---
title: Publish Logshark Results to Tableau Server
---
If you want to publish the workbooks to Tableau Server instead of the default `\Output` folder, you need to modify the `Logshark.config` file and use the `-p` or `--publishworkbooks` option when you run Logshark. Here is the syntax to use.

```
logshark <LogSetLocation> Options -p
```

In this section:

* TOC
{:toc}

-----------



### Update Config

1. You will need to update the `Logshark.config` file. Change the  `<TableauServer>` settings to match your Tableau Server configuration.

```xml
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

1.   The `site` attribute cannot be blank. If you are using the default site (for example, `http://localhost/#`), specify **Default** as the name (`site="Default"`).

1.   To publish workbooks, the user account you specify must exist on the Tableau Server (and the site) with Publisher permissions and the permissions to create projects. (*Site Administrator role will be the easiest option*).

1. If you don't want to store username and password in the config file, you can use command line to specify them. See full list of the available command parameters on [LogShark Command Options](/docs/logshark_cmds).

```
LogShark <LogSetLocation> <RunId> --publishworkbooks --username "myUserName" --password "myPassword"
```

### Publish Workbooks
2. Specify the `-p` or `--publishworkbooks` option when you run Logshark. 

```
logshark logs.zip --publishworkbooks --username "myUserName" --password "myPassword"
```

3.  Navigate to your Tableau Server. The URL for your workbooks would look like the following:  

    <code>http://<i>yourServer</i>/#/site/<i>yourSite</i>/projects   </code>

    The generated workbooks are organized in project folders. The name of a project is  *`Timestamp-MachineName-FileName`*, where *`DateTime`* is the time stamp that indicates when the logs were processed, *`MachineName`* is the name of the computer where Logshark was run, and *`FileName`* is the name of the archive file. The project contains all the workbooks for the archive file. If you want to replace *`MachineName-FileName`* with your own *`RunID`*, please see instructions on the [Configure and Customize Logshark](docs/logshark_configure.md) page. 

4.   Navigate to projects folder you are interested in and double-click the Tableau workbook you want to view. 
     For information about all the plugins and workbooks, see [Logshark Plugins and Generated Workbooks](logshark_plugins)

**NOTE:** To be able to publish workbooks to Tableau Server, the REST API option (`api.server.enabled`) must be enabled. See the [REST API Requirements](https://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_requ.htm%3FTocPath%3D_____3){:target="_blank"} article for more details.