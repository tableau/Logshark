---
title: Install and Configure Logshark
---

If you have not done so already, you need to run the Logshark Setup program. If you need to uninstall or re-install Logshark, you should follow the [Uninstall Logshark](logshark_remove) instructions.


In this section:

* TOC
{:toc}



-------------------------------------------------------------------------------------

### Run the Logshark Setup program
 [![Download Logshark](https://img.shields.io/badge/Download%20Logshark-Version%203.0-blue.svg)](https://github.com/tableau/Logshark/releases/download/v2.1/Setup_Logshark_v2.1.exe)

1.  From the directory where you downloaded Logshark, run the Logshark Setup program, double-click the file `Setup_Logshark_v3.0.exe`.
   
2.  Click **Install** to use the default configuration. This adds Logshark to the PATH environment variable.

-   Click **Options** if you need to change the default installation directory (from `C:\Program Files\Logshark`).

**NOTE:** The archive logs will need to be unzipped or copied to the drive where you have installed Logshark. If your `C:\` drive is low on space, you should install Logshark in a different location.


---------------------------------------------------------------------------------------

### Configure Logshark 


Logshark uses a configuration file to point to the databases that are used for parsing and storing log data. The configuration file also lets you publish the Tableau workbooks to a specific Tableau Server. The path to the configuration file is <code><i>installation-folder</i>\Config\Logshark.config</code>.

Some of these configuration settings can be overridden at the command line. Other settings can only be set in the configuration file. For example, if you want to specify which Tableau Server to use when publishing workbooks, you need to set that option in the configuration file.

**NOTE:** To be able to publish workbooks to Tableau Server, the REST API option (`api.server.enabled`) must be enabled. See the [REST API Requirements](https://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_requ.htm%3FTocPath%3D_____3){:target="_blank"} article for more details.


----

#### Edit the Tableau Server connection information in the Logshark configuration file


If you want to publish the workbooks that Logshark generates on Tableau Server, change the `<TableauConnection>` settings in the Logshark.config file to match your Tableau Server configuration.

When you edit the Tableau Server connection, follow these guidelines:

-   The Server `address` attribute should just contain the hostname or IP address of the computer (for example, *mytableauserver.tableau.com*), and should not be prefixed with the protocol (*http or https*).

-   When using a non-standard port for your Tableau Server, ensure the `port` attribute is set correctly (by default, HTTP uses port **80** and HTTPS uses port **443**).

-   The `site` attribute cannot be blank. If you are using the default site (for example, `http://localhost/#`), specify **Default** as the name (`site="Default"`).

-   To publish workbooks, the user account you specify must exist on the Tableau Server (and the site) with Publisher permissions and the permissions to create projects. (*Site Administrator role will be the easiest option*).

    ```xml
    ...
    <TableauConnection protocol="http" publishingTimeoutSeconds="300">
        <Server address="myTableauServer" port="80" site="MySite"/>
        <User username="myUser" password="myUserPassword"/>
    </TableauConnection>
    ...
    ```

-----

#### Edit the MondoDB connection information in the Logshark configuration file

**NOTE:** You only need to edit the MongoDB connection information if you plan to use your own MongoDB installation to store the log data (recommended if logs are **greater than 2GB**). In most cases, you want to use the MongoDB instance that Logshark provides (using the `--startlocalmongo` command line option, or by setting `LocalMongoOptions useAlways="true"` in the config). For more information, see [Use your own MongoDB instance](logshark_mongo).
