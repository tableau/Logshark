---
title: Install and Configure PostgreSQL
---

Use the following instructions to manually install PostgreSQL on your computer. Logshark needs to have its own PostgreSQL instance to store data. If you are using PostgreSQL on a remote computer, that is, not on the local computer running Logshark, you need to do a few additional steps to allow the remote connection. By default, PostgreSQL does not allow any remote connections.


In this section:

* TOC
{:toc}



### Download and Install PostgreSQL


-   Download PostgreSQL
    [http://www.enterprisedb.com/products-services-training/pgdownload](http://www.enterprisedb.com/products-services-training/pgdownload){:target="_blank"}

-   Run the installer and configure the following options into the setup wizard:

    -   For the (**postgres**) superuser password: *you choose*

    -   Port: **5432**

    -   Locale: English, United States

    -   If prompted to launch StackBuilder, choose **No**.

### Configure PostgreSQL for Logshark


The following instructions describe how to configure your PostgreSQL installation for Logshark.

<!-- ### Create a Login Role for Logshark -->

1.  Open pgAdmin (III or 4), the PostgreSQL administration tool.

2.  Connect to the PostgreSQL server using the superuser (**postgres**) login account.

3.  Create a Login Role for Logshark. Right click on **Login Roles** (or Login/Group Role) and choose **New Login Role**

    ![]({{ site.baseurl }}/assets/postgres-login-role.png)
   

4.  In the New Login Role dialog box, enter the following values:

    Role name: **logshark**

    Password: **password** (on the Definition tab).  You should set this to a custom password, but please note that you will need to update the corresponding password in Logshark.config when you [Install and Configure Logshark](logshark_install).

    Role privileges: Select the **Can create databases** and **Can login** options.

5.  (Optional) If you installed PostgreSQL on a remote computer (not on the computer running Logshark), you need to add a pg\_hba.conf entry to enable a connection. See instructions for this at [https://www.postgresql.org/docs/current/static/auth-pg-hba-conf.html
    ](https://www.postgresql.org/docs/current/static/auth-pg-hba-conf.html){:target="_blank"}


  
