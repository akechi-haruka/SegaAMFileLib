SEGAAMFileLib / SEGAAMFileCmd
(c) 2025 Haruka and contributors

Licensed under the Server Side Public License.

API to interface with SEGA file types used in arcade games.

Nightly builds / downloads: https://nightly.link/akechi-haruka/SEGAAMFileLib/workflows/dotnet/master?preview

----------------
What can the library and application be used for?

* Write and read the following file types:
 - ICF1/ICF2
 - sysfile.dat
 - dliApp.ini, dliOpt.ini

TODOs:
* *.app, *.opt

----------------

A note to differential sysfile sync:

Following scenario:
You have a master sysfile that you want to gain bookkeeping increases and credits from other sysfiles.
Given that the master file is located at C:\master\amfs\sysfile.dat and the sub file at C:\game\amfs\sysfile.dat, run the game as following:

copy C:\game\amfs\sysfile.dat C:\game\amfs\sysfile_diff.dat
SegaAMFileCmd sysfile-sync -f 2 C:\master\amfs\sysfile.dat C:\game\amfs\sysfile.dat File1 NoChange
start /wait game.exe
SegaAMFileCmd sysfile-sync -f 1 -d C:\game\amfs\sysfile_diff.dat C:\master\amfs\sysfile.dat C:\game\amfs\sysfile.dat File2 DifferentialToFile2

So for example, in the master application 5 credits were inserted and bookkeeping registered 32 service presses, during this game session I press service once and spend two credits, then the master application will display 3 credits and 33 service presses. The bookeeping count of service presses in the game also has gone up by 1. (regardless of what the count is)

----------------
Implementation Notes:

* As this is a highly experimental API right now, consumer applications should check Haruka.Arcade.SEGAAMFileLib.VersionInfo.LIB_API_VERSION. This number will be incremented on any breaking changes for consumers.
