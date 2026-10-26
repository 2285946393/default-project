@echo off
rem Auto-fill meme names in batches. Scheduled task calls this file.
rem Keep this file ASCII-only (PowerShell/cmd would misread non-ASCII here).
setlocal
set "PYEXE=D:\software\Python\python.exe"
set "ROOT=%USERPROFILE%\Documents\Default Project"
set "SCRIPT=%ROOT%\projects\meme-spire\tools\meme_autofill.py"
set "LOG=%ROOT%\projects\meme-spire\tools\meme_autofill.log"
"%PYEXE%" "%SCRIPT%" --max-batches 6 >> "%LOG%" 2>&1
endlocal
