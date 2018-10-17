## Logshark-CLI ##

### Usage Instructions ###
See the [Logshark user guide](https://tableau.github.io/Logshark/) for instructions on how to install & use Logshark CLI.

### Developer Setup ###

The current development requirements are:

1. Windows operating system.
2. Visual Studio 2015 or later.
3. WiX Toolset Visual Studio Extension v3.10.1 or later - Required if you wish to to modify the installer projects.
  * Available at http://www.wixtoolset.org
4. ConfigurationSectionDesigner Visual Studio Extension - Required if you wish to modify & regenerate the "LogsharkConfigSection" custom config section class.
  * Go to `Tools > Extensions and Updates`.
  * Select the `Online` section.
  * Search for "ConfigurationSectionDesigner" (*no spaces*) and install it.
5. NUnit Test Adapter 3 Extension - Required if you wish to run the unit tests.

It is recommended that you install the Logshark Project Templates extension by running the "Logshark Artifact Processor Project Template.vsix" and "Logshark Workbook Creation Plugin Project Template.vsix" files found in the root directory.  These add "Logshark Artifact Processor" and "Logshark Workbook Creation Plugin" project types to Visual Studio which you can use to easily get up and running developing a new artifact processor or plugin.

**To build the solution**, you'll want to set the Solution Platform to x64.

**To run the unit tests**, you must:
1. Be running Visual Studio as an Administrator.
2. Set the Test Settings File: `Test > Test Settings > Select Test Settings File > Test.runsettings`

If you encounter a build error related to MongoDB when you try to run the tests, you may need open Task Manager and manually kill the `mongod` process.

**To build the installer**, you will need to switch the build configuration to "Installer".

**To regenerate LogsharkConfigSection.cs**, you will need to explicitly build the `Logshark.ConfigSectionGenerator` project.  It is omitted from the build configurations in order to avoid the time cost of re-running code generation on every build.

### Branch Conventions ###

Development and master are protected branches.  No commits should take place directly against them; any changes should happen through merge requests.

* **master** : Head branch, reserved for releases.
   + **development** : Main development branch.
      - **feature/branchname** : Reserved for new features or changes to core Logshark code.
      - **artifactprocessordevelopment/branchname** : Use this for developing new artifact processors.
      - **plugindevelopment/branchname** : Use this for developing new plugins.
      - **viz/branchname**: Use this for developing new workbooks, or updating existing workbooks.

### Plugin Development Conventions ###

To be accepted into the development & master branches, plugins must adhere to to the following conventions:

+ All plugins should be nested under the Plugins subdirectory, e.g. `<Solution Root>\Plugins\YourPluginName\`.
+ The namespace for each plugin should be `Logshark.Plugins.YourPluginName`.
+ Plugin must not reference any Nuget packages or References that it does not actually use.
+ Helper functions or data object models which may benefit the Plugin development community as a whole may be submitted to the Logshark.PluginLib project.

### Viz Development Conventions ###

These instructions are for people planning to only make viz changes. If you are changing code and a viz, please follow the plugin development process.

+ All workbooks should be located under their respective plugin directory. For example, the viz for ClusterController is under "Plugins\ClusterController".
+ Create a branch off of the development branch and use the following naming convention: `viz/<Workbook Name>`
+ When you are done working the viz, check it in and ask a member of the Customer Data Platform team for a review.
