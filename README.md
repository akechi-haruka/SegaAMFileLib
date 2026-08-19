SEGAAMFileLib / SEGAAMFileCmd

(c) 2024-2026 Haruka and contributors

Licensed under the Server Side Public License.

API to interface with SEGA file types used in arcade games.

Get / Download:

* Latest stable Cmd release download: https://github.com/akechi-haruka/SegaAMFileLib/releases/latest
* Latest unstable Cmd
  download: https://nightly.link/akechi-haruka/SegaAMFileLib/workflows/dotnet/master/Sega835Cmd-latest.zip
* NuGet: Haruka.Arcade.SegaAMFileLib / https://www.nuget.org/packages/Haruka.Arcade.SegaAMFileLib
* Nightly builds of Lib and others: https://nightly.link/akechi-haruka/SegaAMFileLib/workflows/dotnet/master?preview

----------------
What can the application be used for?

* Edit sysfile.dat (insert credits, set free play, reset e-money auth)
* Synchronize multiple sysfiles (to have same credits/bookkeeping data in multiple games)
* View and verify ICF files
* Create ICF files
* Commit pending updates in ICF files
* Create DLI files
* View BootID information in fscrypt containers
* Verify fscrypt containers
* Extract fscrypt containers, .app files, .opt files of both standard and APM variant, as well as differential files

----------------
What can the library be used for?

* Write and read the following file types:
    - ICF files (ICF1)
    - sysfile.dat
    - dliApp.ini, dliOpt.ini
* Extract the following container formats:
    - *.app
    - *.opt
    - *.vhd (with partitions or raw)

----------------
Implementation Notes:

* As this is a highly experimental API right now, consumer applications should check
  Haruka.Arcade.SegaAMFileLib.VersionInfo.LIB_API_VERSION. This number will be incremented on any breaking changes for
  consumers.

----------------

A note on differential sysfile sync:

Following scenario:
You have a master sysfile that you want to gain bookkeeping increases and credits from other sysfiles. Given that the
master file is located at C:\master\amfs\sysfile.dat and the sub file at C:\game\amfs\sysfile.dat, run the game as
following:

```
copy C:\game\amfs\sysfile.dat C:\game\amfs\sysfile_diff.dat
SegaAMFileCmd sysfile-sync -f 2 C:\master\amfs\sysfile.dat C:\game\amfs\sysfile.dat File1 NoChange
start /wait game.exe
SegaAMFileCmd sysfile-sync -f 1 -d C:\game\amfs\sysfile_diff.dat C:\master\amfs\sysfile.dat C:\game\amfs\sysfile.dat File2 DifferentialToFile2
```

So for example, in the master application 5 credits were inserted and bookkeeping registered 32 service presses, during
this game session I press service once and spend two credits, then the master application will display 3 credits and 33
service presses. The bookeeping count of service presses in the game also has gone up by 1. (regardless of what the
count is)