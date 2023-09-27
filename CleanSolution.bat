@echo off

TASKKILL  /F /IM vstest.console.exe /T
TASKKILL   /F /IM FileSeeker.exe /T

FOR /d /r . %%d IN ("bin") DO @IF EXIST "%%d" rd /s /q "%%d"

FOR /d /r . %%d IN ("obj") DO @IF EXIST "%%d" rd /s /q "%%d"