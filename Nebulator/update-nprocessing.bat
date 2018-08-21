
echo off

rem dir

rem Copy executables
copy /Y "Nebulator.Common.dll" "..\..\..\NProcessing\lib" >NUL
copy /Y "Nebulator.Common.xml" "..\..\..\NProcessing\lib" >NUL
copy /Y "Nebulator.Script.dll" "..\..\..\NProcessing\lib" >NUL
copy /Y "Nebulator.Script.xml" "..\..\..\NProcessing\lib" >NUL
copy /Y "Nebulator.Comm.dll" "..\..\..\NProcessing\lib" >NUL
copy /Y "Nebulator.Comm.xml" "..\..\..\NProcessing\lib" >NUL

rem Copy doc files
copy /Y "..\..\..\Nebulator.wiki\ScriptSyntax.md" "..\..\..\NProcessing.wiki" >NUL
copy /Y "..\..\..\Nebulator.wiki\ScriptApiProcessing.md" "..\..\..\NProcessing.wiki" >NUL
copy /Y "..\..\..\Nebulator.wiki\Porting.md" "..\..\..\NProcessing.wiki" >NUL
copy /Y "..\..\..\Nebulator.wiki\Notes.md" "..\..\..\NProcessing.wiki" >NUL

rem pause
