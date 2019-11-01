---
title: If You Are On macOS Catalina 10.15
---

* TOC
{:toc}

Issue
-------------------

If you are running LogShark on macOS Catalina 10.15, following error message is displayed:

    * **custactutil** can't be opened because its integrity cannot be verified. This software needs to be updated. Contact the developer for more information.*


Resolution
----------
Grant permission to **custactutil** and **custactutil_libFNP.dylib** as follows:
1. Click **Cancel** to dismiss the warning dialog.
**Note:** Do not click Move to Trash.
1. Open the **Security & Privacy** pane in System Preferences.
1. At the bottom of the pane, the message "**custactutil** was blocked from use because it is not from an identified developer" is displayed. Click the **Allow Anyway** button.
1. Run **custactutil** again.
1. The warning message is displayed for a second time, but now an Open button displays. Click **Open**.
1. The warning message displays for a third time, this time referencing **custactutil_libFNP.dylib** and offering options to Move to Trash or Cancel. Click **Cancel**.
1. Go back to the **Security & Privacy** pane and click **Allow Anyway** for this library file.
1. Run **custactutil** again. 
1. The warning message will be displayed for a fourth time. click the **Open** button.

Cause
-----
This behavior is related to a known issue with ID 953649 which is currently under investigation. 
Apple has new requirements for software to run on macOS Catalina.

Additional Information
----------------------
If you clicked **Move to Trash** in response to one of the warning messages, open the trash can, control-click or right-click the needed file, and select **Put back**.