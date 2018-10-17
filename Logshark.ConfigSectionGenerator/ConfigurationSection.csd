<?xml version="1.0" encoding="utf-8"?>
<configurationSectionModel xmlns:dm0="http://schemas.microsoft.com/VisualStudio/2008/DslTools/Core" dslVersion="1.0.0.0" Id="d0ed9acb-0435-4532-afdd-b5115bc4d562" namespace="logshark" xmlSchemaNamespace="logshark" xmlns="http://schemas.microsoft.com/dsltools/ConfigurationSectionDesigner">
  <typeDefinitions>
    <externalType name="String" namespace="System" />
    <externalType name="Boolean" namespace="System" />
    <externalType name="Int32" namespace="System" />
    <externalType name="Int64" namespace="System" />
    <externalType name="Single" namespace="System" />
    <externalType name="Double" namespace="System" />
    <externalType name="DateTime" namespace="System" />
    <externalType name="TimeSpan" namespace="System" />
  </typeDefinitions>
  <configurationElements>
    <configurationSection name="LogsharkConfig" namespace="Logshark.ConfigSection" codeGenOptions="Singleton, XmlnsProperty" xmlSectionName="Config">
      <elementProperties>
        <elementProperty name="PostgresConnection" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="PostgresConnection" isReadOnly="false" documentation="Settings pertaining to the Postgres database.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/PostgresConnection" />
          </type>
        </elementProperty>
        <elementProperty name="MongoConnection" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="MongoConnection" isReadOnly="false" documentation="Settings pertaining to the MongoDB database.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/MongoConnection" />
          </type>
        </elementProperty>
        <elementProperty name="RunOptions" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="RunOptions" isReadOnly="false" documentation="Settings pertaining to LogShark runtime options">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/RunOptions" />
          </type>
        </elementProperty>
        <elementProperty name="TableauConnection" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="TableauConnection" isReadOnly="false" documentation="Settings pertaining to the Tableau Server connection.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/TableauServerConnection" />
          </type>
        </elementProperty>
        <elementProperty name="ArtifactProcessorOptions" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="ArtifactProcessorOptions" isReadOnly="false" documentation="Options related to artifact processors.">
          <type>
            <configurationElementCollectionMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/ArtifactProcessorOptions" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationSection>
    <configurationElement name="MongoConnection" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="PoolSize" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="poolSize" isReadOnly="false" documentation="The MongoDB max connection pool size.">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="Timeout" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="timeout" isReadOnly="false" documentation="The MongoDB connection timeout, in seconds.">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="InsertionRetries" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="insertionRetries" isReadOnly="false" documentation="The number of times a failed insert should be retried." defaultValue="3">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <elementProperties>
        <elementProperty name="Servers" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="Servers" isReadOnly="false" documentation="Configuration information about the MongoDB server endpoints.">
          <type>
            <configurationElementCollectionMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/MongoServers" />
          </type>
        </elementProperty>
        <elementProperty name="User" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="User" isReadOnly="false" documentation="Settings related to the Mongo user.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/MongoUser" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationElement>
    <configurationElement name="PostgresConnection" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="CommandTimeout" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="commandTimeout" isReadOnly="false" documentation="The Postgres command timeout, in seconds" defaultValue="120">
          <validator>
            <integerValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-negative Integer" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="TcpKeepalive" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="tcpKeepalive" isReadOnly="false" documentation="The number of seconds of connection inactivity before a TCP keepalive query is sent. " defaultValue="0">
          <validator>
            <integerValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-negative Integer" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="WriteBufferSize" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="writeBufferSize" isReadOnly="false" documentation="The size of the Postgres write buffer, in bytes." defaultValue="16384">
          <validator>
            <integerValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-negative Integer" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <elementProperties>
        <elementProperty name="Server" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="Server" isReadOnly="false" documentation="Information about the Postgres server.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/PostgresServer" />
          </type>
        </elementProperty>
        <elementProperty name="User" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="User" isReadOnly="false" documentation="Settings related to the Postgres user.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/PostgresUser" />
          </type>
        </elementProperty>
        <elementProperty name="Database" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="Database" isReadOnly="false" documentation="Information about the Postgres database instance.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/PostgresDatabase" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationElement>
    <configurationElement name="MongoServer" namespace="Logshark.Config" documentation="Information about a single MongoDB server endpoint.">
      <attributeProperties>
        <attributeProperty name="Server" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="address" isReadOnly="false" documentation="The hostname or IP address of the MongoDB server." defaultValue="&quot;localhost&quot;">
          <validator>
            <stringValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-empty String" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Port" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="port" isReadOnly="false" documentation="The port of the MongoDB server.">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="PostgresServer" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="Server" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="address" isReadOnly="false" documentation="The hostname or IP address of the Postgres server." defaultValue="&quot;unspecified&quot;">
          <validator>
            <stringValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-empty String" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Port" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="port" isReadOnly="false" documentation="The port of the Postgres server." defaultValue="5432">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="TuningOptions" namespace="Logshark.Config">
      <elementProperties>
        <elementProperty name="FilePartitioner" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="FilePartitioner" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/FilePartitioner" />
          </type>
        </elementProperty>
        <elementProperty name="FileProcessor" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="FileProcessor" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/FileProcessor" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationElement>
    <configurationElement name="FilePartitioner" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="MaxFileSizeMB" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="maxFileSizeMb" isReadOnly="false" documentation="The maximum size a file can be without being partitioned, in megabytes." defaultValue="250">
          <validator>
            <integerValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Positive Integer" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="ConcurrencyLimit" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="concurrencyLimit" isReadOnly="false" documentation="The maximum number of files that can be partitioned concurrently." defaultValue="4">
          <validator>
            <integerValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Positive Integer" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="PostgresUser" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="Username" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="username" isReadOnly="false" documentation="The username of the Postgres user.">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Password" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="password" isReadOnly="false" documentation="The password for the Postgres user.">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="MongoUser" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="Username" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="username" isReadOnly="false" documentation="The username of the Mongo user.">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Password" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="password" isReadOnly="false" documentation="The password for the Mongo user.">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="PostgresDatabase" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="Name" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="name" isReadOnly="false" documentation="The name of the Postgres database instance." defaultValue="&quot;postgres&quot;">
          <validator>
            <stringValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-empty String" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="TableauServerConnection" namespace="Logshark.Config" documentation="Information about the Tableau Server instance where results will be published.">
      <attributeProperties>
        <attributeProperty name="Protocol" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="protocol" isReadOnly="false" documentation="The communication protocol to use, i.e. &quot;http&quot; or &quot;https&quot;." defaultValue="&quot;http&quot;">
          <validator>
            <stringValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/HttpProtocolValidator" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="PublishingTimeoutSeconds" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="publishingTimeoutSeconds" isReadOnly="false" documentation="The number of seconds to wait for a response when publishing a workbook to Tableau Server." defaultValue="600">
          <validator>
            <integerValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Positive Integer" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <elementProperties>
        <elementProperty name="Server" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="Server" isReadOnly="false">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/TableauServer" />
          </type>
        </elementProperty>
        <elementProperty name="User" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="User" isReadOnly="false" documentation="Settings for the Tableau Server user used to publish results.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/TableauUser" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationElement>
    <configurationElement name="TableauServer" namespace="Logshark.Config" documentation="Information about the Tableau Server endpoint.">
      <attributeProperties>
        <attributeProperty name="Server" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="address" isReadOnly="false" documentation="The hostname or IP address of Tableau Server." defaultValue="&quot;localhost&quot;">
          <validator>
            <stringValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-empty String" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Port" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="port" isReadOnly="false" documentation="The port that Tableau Server is running on." defaultValue="80">
          <validator>
            <integerValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Valid Port" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
        <attributeProperty name="Site" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="site" isReadOnly="false" documentation="The name of the site to publish to." defaultValue="&quot;Default&quot;">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="TableauUser" namespace="Logshark.Config" documentation="Information about the Tableau Server user account to publish as.">
      <attributeProperties>
        <attributeProperty name="Username" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="username" isReadOnly="false" documentation="The username of the user to publish as." defaultValue="&quot;admin&quot;">
          <validator>
            <stringValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-empty String" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
        <attributeProperty name="Password" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="password" isReadOnly="false" documentation="The password of this user account." defaultValue="&quot;password&quot;">
          <validator>
            <stringValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-empty String" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="ArtifactProcessorConfigNode" namespace="Logshark.Config" documentation="Options related to running an artifact processor.">
      <attributeProperties>
        <attributeProperty name="Name" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="name" isReadOnly="false" documentation="The type name of the artifact processor." defaultValue="&quot;Default&quot;">
          <validator>
            <stringValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Non-empty String" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
      <elementProperties>
        <elementProperty name="DefaultPlugins" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="DefaultPlugins" isReadOnly="false" documentation="The default set of plugins to run on every Logshark execution using the parent artifact processor.">
          <type>
            <configurationElementCollectionMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/DefaultPlugins" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationElement>
    <configurationElementCollection name="DefaultPlugins" namespace="Logshark.Config" documentation="The default set of plugins to run on every Logshark execution which uses the parent artifact processor." xmlItemName="Plugin" codeGenOptions="Indexer, AddMethod, RemoveMethod, GetItemMethods">
      <itemType>
        <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Plugin" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="Plugin" namespace="Logshark.Config" documentation="Information about a Logshark plugin.">
      <attributeProperties>
        <attributeProperty name="Name" isRequired="true" isKey="true" isDefaultCollection="false" xmlName="name" isReadOnly="false" documentation="The name of the plugin.">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElementCollection name="MongoServers" namespace="Logshark.Config" xmlItemName="Server" codeGenOptions="Indexer, AddMethod, RemoveMethod, GetItemMethods">
      <itemType>
        <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/MongoServer" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="FileProcessor" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="ConcurrencyLimitPerCore" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="concurrencyLimitPerCore" isReadOnly="false" documentation="Determines the degree of concurrency for file processing.  This number is multiplied by the number of logical processors on the system." defaultValue="1">
          <validator>
            <integerValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Positive Integer" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="LocalMongoOptions" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="UseAlways" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="useAlways" isReadOnly="false" documentation="Indicates whether a local MongoDB instance should be used for each run." defaultValue="false">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Boolean" />
          </type>
        </attributeProperty>
        <attributeProperty name="PurgeOnStartup" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="purgeOnStartup" isReadOnly="false" documentation="Indicates whether the local Mongo DB should be purged whenever it is started." defaultValue="true">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Boolean" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="RunOptions" namespace="Logshark.Config" documentation="Encapsulates various runtime application options.">
      <elementProperties>
        <elementProperty name="Tuning" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="Tuning" isReadOnly="false" documentation="Settings pertaining to performance tuning.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/TuningOptions" />
          </type>
        </elementProperty>
        <elementProperty name="LocalMongo" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="LocalMongo" isReadOnly="false" documentation="Settings pertaining to using a local MongoDB instance.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/LocalMongoOptions" />
          </type>
        </elementProperty>
        <elementProperty name="DataRetention" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="DataRetention" isReadOnly="false" documentation="Settings pertaining to output data retention.">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/DataRetentionOptions" />
          </type>
        </elementProperty>
        <elementProperty name="TempFolder" isRequired="false" isKey="false" isDefaultCollection="false" xmlName="TempFolder" isReadOnly="false" documentation="Temp folder to use while extracting data from log set">
          <type>
            <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/TempFolderOptions" />
          </type>
        </elementProperty>
      </elementProperties>
    </configurationElement>
    <configurationElementCollection name="ArtifactProcessorOptions" namespace="Logshark.Config" documentation="Options pertaining to artifact processors." xmlItemName="ArtifactProcessor" codeGenOptions="Indexer, AddMethod, RemoveMethod, GetItemMethods">
      <itemType>
        <configurationElementMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/ArtifactProcessorConfigNode" />
      </itemType>
    </configurationElementCollection>
    <configurationElement name="DataRetentionOptions" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="MaxRuns" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="maxRuns" isReadOnly="false" documentation="The maximum number of runs that will be retained." defaultValue="10">
          <validator>
            <integerValidatorMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Positive Integer" />
          </validator>
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/Int32" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
    <configurationElement name="TempFolderOptions" namespace="Logshark.Config">
      <attributeProperties>
        <attributeProperty name="Path" isRequired="true" isKey="false" isDefaultCollection="false" xmlName="path" isReadOnly="false" documentation="Full path to the temp folder">
          <type>
            <externalTypeMoniker name="/d0ed9acb-0435-4532-afdd-b5115bc4d562/String" />
          </type>
        </attributeProperty>
      </attributeProperties>
    </configurationElement>
  </configurationElements>
  <propertyValidators>
    <validators>
      <stringValidator name="Non-empty String" minLength="1" />
      <integerValidator name="Positive Integer" minValue="1" />
      <integerValidator name="Valid Port" maxValue="65535" minValue="0" />
      <stringValidator name="HttpProtocolValidator" maxLength="5" minLength="4" />
      <integerValidator name="Non-negative Integer" minValue="0" />
    </validators>
  </propertyValidators>
</configurationSectionModel>