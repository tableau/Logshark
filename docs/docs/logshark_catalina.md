---
title: If You Are On macOS Catalina 10.15
---

* TOC
{:toc}

Issue
-------------------

If you are running LogShark on macOS Catalina 10.15, following error message is displayed:

    * **LogShark** cannot be opened because the developer cannot be verified. macOS cannot verify that this app is free from malware.*


Resolution
----------
Grant permission to **LogShark** and **System.Private.CoreLib.dll** as follows:
1. Click **Cancel** to dismiss the warning dialog.
**Note:** Do not click Move to Trash.
1. Open the **Security & Privacy** pane in System Preferences.
1. At the bottom of the pane, the message "**LogShark** was blocked from use because it is not from an identified developer" is displayed. Click the **Allow Anyway** button.
1. Run **LogShark** again.
1. The warning message is displayed for a second time, but now an Open button displays. Click **Open**.
1. The warning message displays for a third time, this time referencing **System.Private.CoreLib.dll** and offering options to Move to Trash or Cancel. Click **Cancel**.
1. Go back to the **Security & Privacy** pane and click **Allow Anyway** for this library file.
1. Run **custactutil** again. 
1. The warning message will be displayed for a fourth time. click the **Open** button.

System.Runtime.dll
netstandard.dll
System.ComponentModel.dll
System.Console.dll
System.IO.FileSystem.dll
System.Runtime.Extensions.dll
System.Linq.dll
System.ComponentModel.Annotation.dll
System.Collections.dll
System.Threading.dll
System.Runtime.InteropServices.dll
System.Text.Encoding.Extentions.dll
System.Private.Uri.dll
System.Threading.Tasks.dll
System.Threading.Thread.dll
System.Memory.dll
System.Buffers.dll
System.Linq.Expressions.dll
System.ComponentModel.TypeConverter.dll
System.ObjectModel.dll
System.Runtime.Numerics.dll
System.IO.dll
System.Globalization.dll
System.Text.Encoding.dll
System.Text.RegularExpressions.dll
System.Threading.Timer.dll
System.Collections.NonGeneric.dll
System.Collections.Concurrent.dll
System.Runtime.InteropServices.RuntimeInformation.dll
System.Collections.Specialized.dll
System.Drawing.Primitives.dll

-----
This behavior is related to a known issue with ID 953649 which is currently under investigation. 
Apple has new requirements for software to run on macOS Catalina.

Additional Information
----------------------
If you clicked **Move to Trash** in response to one of the warning messages, open the trash can, control-click or right-click the needed file, and select **Put back**.