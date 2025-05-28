SEGAAMFileLib / SEGAAMFileCmd
(c) 2025 Haruka and contributors

Licensed under the Server Side Public License.

API to interface with SEGA file types used in arcade games.

Nightly builds / downloads: https://nightly.link/akechi-haruka/SEGAAMFileLib/workflows/dotnet/master?preview

----------------
What can the library and application be used for?

* Write and read the following file types:
 - *.icf
 - sysfile.dat

TODOs:
* *.app, *.opt
* DLI

----------------
Implementation Notes:

* As this is a highly experimental API right now, consumer applications should check Haruka.Arcade.SEGAAMFileLib.VersionInfo.LIB_API_VERSION. This number will be incremented on any breaking changes for consumers.
