@echo off
setlocal
cd /d "%~dp0.."
set "ASPNETCORE_ENVIRONMENT=Development"
set "DOTNET_ENVIRONMENT=Development"
if exist ".env" for /f "usebackq eol=# tokens=1,* delims==" %%A in (".env") do set "%%A=%%~B"
if exist "RelatedPartiesRegister\.env" for /f "usebackq eol=# tokens=1,* delims==" %%A in ("RelatedPartiesRegister\.env") do set "%%A=%%~B"

if /i "%~1"=="--check-config" (
  if not defined Database__ServerName exit /b 1
  if not defined Database__Name exit /b 1
  if /i not "%Database__IntegratedSecurity%"=="true" (
    if not defined Database__User exit /b 1
    if not defined Database__Password exit /b 1
  )
  echo Database configuration loaded successfully.
  exit /b 0
)
dotnet build "RelatedPartiesRegister\RelatedPartiesRegister.csproj" --configuration Debug --no-restore -p:UseAppHost=false -p:UseLocalDataCoreAssemblyName=true
if errorlevel 1 exit /b %errorlevel%
dotnet "RelatedPartiesRegister\bin\Debug\net10.0\DATA_CORE.dll" --urls http://127.0.0.1:5000
