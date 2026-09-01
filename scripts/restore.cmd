@echo off
setlocal
cd /d "%~dp0.."
dotnet restore "RelatedPartiesRegister\RelatedPartiesRegister.sln" --configfile "RelatedPartiesRegister\nuget.config"
if errorlevel 1 exit /b %errorlevel%
cd /d "%~dp0..\src\Web"
call pnpm.cmd install --ignore-scripts
