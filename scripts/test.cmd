@echo off
setlocal
cd /d "%~dp0.."
dotnet test "RelatedPartiesRegister\RelatedPartiesRegister.sln" --no-restore
if errorlevel 1 exit /b %errorlevel%
cd /d "%~dp0..\src\Web"
call pnpm.cmd lint
if errorlevel 1 exit /b %errorlevel%
call pnpm.cmd localization:validate
if errorlevel 1 exit /b %errorlevel%
call pnpm.cmd test
