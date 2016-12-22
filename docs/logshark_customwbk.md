---
title: Add Your Own Custom Workbooks
layout: docs
---



Logshark provides an option to include your own custom workbooks in a Logshark run.  These workbooks will then be included in the run output if the CustomWorkbooks plugin is run and all of their dependencies are met.

1. To configure this, first browse to your Logshark installation directory and open up the `CustomWorkbooks` folder.  
2. Add your custom workbook (.twb) files to this folder.  
3. Additionally, you will need to edit `CustomWorkbookConfig.xml` file and add an `Workbook` entry for each custom workbook.  The required attributes and elements are the names of the workbooks and any dependencies they have on other workbooks.  

    For example:



```xml

<!--
  Custom Tableau workbooks can be placed alongside this config and they will be output at runtime if their dependencies are met.
  Plugin dependencies must be declared for any plugins that generate a table that the custom workbook relies on.
  To get a list of eligible plugin dependency names, invoke Logshark with the "listplugins" command line flag.
-->
<CustomWorkbooks>
  <!-- EXAMPLE
  <Workbook name="MyCustomWorkbook.twb">
    <PluginDependency name="Apache" />
    <PluginDependency name="Backgrounder" />
  </Workbook>
  -->

  <Workbook name="MyCustomApacheAndBackgrounderWorkbook.twb">
    <PluginDependency name="Apache" />
    <PluginDependency name="Backgrounder" />
  </Workbook>

</CustomWorkbooks>



```


This entry will cause the file `MyCustomApacheAndBackgrounderWorkbook.twb` to be output at the Logshark run with the appropriate data sources substituted.  NOTE: The `PluginDependency` entries in the example above will make it so that the workbook will only be output if both the Apache and Backgrounder plugins run successfully.  You can declare multiple plugin dependencies for a single workbook, so that you can include workbooks that join the output tables of multiple plugins.


    