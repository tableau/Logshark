---
title: Replayer Plugin
---

---------------------------
### What is Replayer? ###

Replay is a tool that can play back real traffic, it replays Tableau Server single or multi-user sessions. Replay reconstructs the URL access and interactions on the viz using Tableau Server logs. You can use Replay to reproduce and troubleshoot customer issues, perform regression testing, measure and verify performance by replaying/simulating real-world multi-user load conditions. 

In this section:
* TOC
{:toc}

------------------
### How does Replayer work? ###

Replayer uses Logshark to extract browser session and interaction information from Tableau Server log files. Replayer takes this information and builds a JSON file that can be used to play back sessions. The Replayer file contains the URLs that users accessed during the session, along with the commands users entered and the time the actions took place. Using the Replayer file, single or multiple sessions can be played back.

In Tableau, when users load a viz, select marks, apply filters, or interact with Tableau in other ways, the commands and actions get recorded in the log files. To collect the logs, you can use `tabadmin` or `tsm` commands to create an archive of the Tableau Server log files (for example, Logs.zip).

Starting with the Tableau Server archive, you run Logshark with a special plugin for Replayer, which extracts browser session information from the Apache logs and correlates that data with vizqlserver logs and generates a JSON file. This JSON file contains the browser interaction information, with all the viz links and commands from the Tableau Server sessions.

Using the JSON file as input, Replayer can walk through the browser session, opening the URL and running Tableau commands on the viz. 

Sessions can be played back in a browser or without a browser. With a browser, the Replayer uses the Selenium libraries to load the viz and playback commands. Verification is done at each step of the way, and exceptions that are thrown are captured in logs.

Without a browser, you can play back multiple sessions simultaneously, so you can simulate load conditions that can help you test your Tableau Server configuration.

For more information on running Replayer, see "Using Replayer".  For information about installation, see "Replayer Installation guide".

![Replayer Screeenshot](/docs/assets/replay_overview.jpg)

----
### Replayer key features ###

**Using Replayer, you can:**
- Playback specific Tableau Server sessions, and filter the session based upon start time or RequestID.
- Use it to simulate load conditions so that you can test how to scale and balance your Tableau Server installations.
- Perform regression testing by running and comparing end-to-end user scenarios for Tableau Server upgrades.
- Capture and report HTTP exceptions that occur in a single-user session.
- Replay a customer defect, so that you can verify that it is fixed.

**Replayer supports:**
- Replaying single-user sessions in a browser (on Chrome & IE).
- Replaying multiple-user sessions without a browser.
- Control of the playback speed (can scale to the time recorded in the session).
- Altering traffic in Replayer file, such as multiplying the load, consolidating traffic to run at specified rate.
- Verification of each action, but reading the HTTP response code, presmodel or UI objects.

**Replayer supports viz rendering scenarios only:**
- **NOTE:** It does not replay other tasks like publishing, creating  a viz, background tasks, etc.
- Replay viz viewing and authoring scenarios along with interactions.
- Replay specific slices of the logs based on:
-- Session ID
-- Time segment
-- User
-- Request ID
- Altering traffic by multiplying load for performance and capacity planning.

----
### Viewing the results of a Replayer test run ###
When you run Replayer, it writes information about each session it executes. The results can be consumed using many methods:
- For user acceptance testing to understand page load and interaction issues in A-B comparison, use the Replayer-Results viz.
- Use the Logshark viz generated from the Tableau Server log files, after a Replayer run to analyze results.

More details on looking at Replayer results can be found hereViewing results from Replayer test run .


