---
title: Install and Configure Logshark
layout: docs
---

If you have not done so already, you need to run the Logshark Setup program. If you need to uninstall or re-install Logshark, you should follow the [Uninstall Logshark](#uninstall-logshark) instructions.


In this section:

* TOC
{:toc}



-------------------------------------------------------------------------------------

### Run the Logshark Setup program


1.  From the directory where you downloaded Logshark, run the Logshark Setup program, double-click the file `Setup_Logshark_v1.0.exe`.

2.  Click **Install** to use the default configuration. This adds Logshark to the PATH environment variable.

-   Click **Options** if you need to change the default installation directory (from `C:\Program Files\Logshark`).

**NOTE:** The archive logs will need to be unzipped or copied to the drive where you have installed Logshark. If your `C:\` drive is low on space, you should install Logshark in a different location.





---------------------------------------------------------------------------------------

### Configure Logshark 


Logshark uses a configuration file to point to the databases that are used for parsing and storing log data. The configuration file also lets you publish the Tableau workbooks to a specific Tableau Server. This configuration is located at *\<install_directory>*`\Config\Logshark.config`.

Some of these configuration settings can be overridden at the command line. Other settings can only be set in the configuration file. For example, if you want to specify which Tableau Server to use when publishing workbooks, you need to set that option in the configuration file.

**NOTE:** To be able to publish workbooks to Tableau Server, the REST API option (`api.server.enabled`) must be enabled. See the [REST API Requirements](https://onlinehelp.tableau.com/current/api/rest_api/en-us/help.htm#REST/rest_api_requ.htm%3FTocPath%3D_____3) article for more details.

-----

#### Edit the PostgreSQL connection information in Logshark.config


1.  In a text editor, open the configuration file: *\<install_directory>*`\Config\Logshark.config` file. In the Logshark.config file, change the `<PostgresConnection>` settings to match your PostgreSQL setup.

2.  Set the Server `address` attribute to the name of the computer that is running PostgreSQL. For example, if you have installed PostgreSQL on your local computer, use **localhost** as the address.

3.  Set the `port` attribute to the port your server uses if it is different from the default. The default port is **5432**.

4.  For the `user`, set both the `username` and `password` to **logshark** to match the role/user and password you added when you [Configure PostgreSQL for Logshark](logshark_postgres#configure-postgresql-for-logshark).


 
    ```xml 
    ...
    <PostgresConnection>
       <Server address="localhost" port="5432"/>
       <User username="logshark" password="logshark"/>
     </PostgresConnection>
     ...
    ```


5.  Save the file.

    **NOTE:** You only need to edit the MongoDB connection information if you plan to use your own MongoDB installation to store the log data (recommended if logs are greater than 2GB). In most cases, you want to use the MongoDB instance that Logshark provides (using the `--startlocalmongo` command line option, or by setting `LocalMongoOptions useAlways="true"` in the config). For more information, see [Use your own MongoDB instance](logshark_mongo#use-your-own-mongodb-instance).



----

#### Edit the Tableau Server connection information in Logshark.config


If you want to publish the workbooks that Logshark generates on Tableau Server, change the `<TableauConnection>` settings in the Logshark.config file to match your Tableau Server configuration.

When you edit the Tableau Server connection, follow these guidelines:

-   The Server `address` attribute should just contain the hostname or IP address of the computer (for example, *mytableauserver.tableau.com*), and should not be prefixed with the protocol (*http or https*).

-   When using a non-standard port for your Tableau Server, ensure the `port` attribute is set correctly (by default, HTTP uses port **80** and HTTPS uses port **443**).

-   The `site` attribute cannot be blank. If you are using the default site (for example, `http://localhost/#`), specify **Default** as the name (`site="Default"`).

-   To publish workbooks, the user account you specify must exist on the Tableau Server (and the site) with Publisher permissions and the permissions to create projects. (*Site Administrator role will be the easiest option*).

    ```xml
    ...
    <TableauConnection protocol="http">
        <Server address="myTableauServer" port="80" site="MySite"/>
        <User username="myUser" password="myUserPassword"/>
    </TableauConnection>
    ...
    ```