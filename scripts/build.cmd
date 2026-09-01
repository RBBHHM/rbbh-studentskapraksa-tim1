@echo off
setlocal
cd /d "%~dp0.."
dotnet build "RelatedPartiesRegister\RelatedPartiesRegister.sln" --configuration Release --no-restore
if errorlevel 1 exit /b %errorlevel%
cd /d "%~dp0..\src\Web"
call pnpm.cmd build
