---
title: Use your own MongoDB Instance
---

If you have log files that are larger than 2 GB, you should install and set up your own MongoDB Community server. Download it from [https://www.mongodb.com/download-center\#community.
](https://www.mongodb.com/download-center){:target="_blank"}

If you install your own MongoDB, you must configure LogShark to use your MongoDB instance by editing the LogShark configuration file. The file is located in your LogShark installation directory in `\config\LogShark.config`.

The LogShark.config file contains the `<MongoConnection>` element that sets the options for the MongoDB that LogShark uses to store the BSON documents extracted from the archive (zipped logs).

```xml
  <MongoConnection poolSize="200" timeout="60" insertionRetries="3"> 
    <Server address="LogShark-mongo-prod" port="27000"/> 
    <User username="LogShark" password="password"/> 
  </MongoConnection> 
```


If you want to use a different MongoDB, change the setting for server address and port to match the new database. 
For example: 


`<Server address="myMongodb_server" port="27017"/>`

Set the user name and password, if necessary, or leave blank:

`<User username="" password=""/>`

The MongoConnection element has attributes that describe the connection properties. These attributes are described in the following table:

| MongoConnection Attribute | Description 
|---------------------------|----------------------------------------------------------------
|poolSize | Sets the maximum connection pool size for concurrent connections to MongoDB.  Most people will never come close to reaching the default limit of 200. If you experience problems with having too many concurrent connections open against the MongoDB, you might want to try lowering this value. The default is `poolSize="200"`.
|timeout  | Specifies the maximum time to wait (in seconds) when the client is establishing a new connection to MongoDB.  In most cases, you won't want to change this, as the default setting of 60 seconds should allow plenty of time. Default is `timeout="60"`.
|insertionRetries | Specifies the number of times a failed insertion to MongoDB will be retried.  Retries are costly operations, so the number of attempts probably shouldn't ever be set higher than the default (`insertionRetries="3"`).
