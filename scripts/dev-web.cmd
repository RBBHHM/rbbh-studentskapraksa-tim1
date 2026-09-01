@echo off
setlocal
cd /d "%~dp0..\src\Web"
call pnpm.cmd dev
