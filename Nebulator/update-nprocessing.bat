
echo off

rem dir

rem Copy executables
copy /Y "Nebulator.Common.dll" "..\..\..\NProcessing\lib"
copy /Y "Nebulator.Common.xml" "..\..\..\NProcessing\lib"
copy /Y "Nebulator.Script.dll" "..\..\..\NProcessing\lib"
copy /Y "Nebulator.Script.xml" "..\..\..\NProcessing\lib"
copy /Y "Nebulator.Protocol.dll" "..\..\..\NProcessing\lib"
copy /Y "Nebulator.Protocol.xml" "..\..\..\NProcessing\lib"

rem Copy doc files
copy /Y "..\..\..\Nebulator.wiki\ScriptSyntax.md" "..\..\..\NProcessing.wiki"
copy /Y "..\..\..\Nebulator.wiki\ScriptApiProcessing.md" "..\..\..\NProcessing.wiki"
copy /Y "..\..\..\Nebulator.wiki\Porting.md" "..\..\..\NProcessing.wiki"
copy /Y "..\..\..\Nebulator.wiki\Notes.md" "..\..\..\NProcessing.wiki"

rem pause
