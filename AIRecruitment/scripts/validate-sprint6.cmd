@echo off
setlocal
cd /d "%~dp0.."

echo [1/4] Validate Docker Compose configuration
docker compose config --quiet
if errorlevel 1 goto :failed

echo [2/4] Run AI Service tests in Docker
docker compose run --rm ai-service python -m pytest -q
if errorlevel 1 goto :failed

echo [3/4] Build ASP.NET Core backend
dotnet build .\src\AIRecruitment.Presentation\AIRecruitment.Presentation.csproj --configuration Release
if errorlevel 1 goto :failed

echo [4/4] Build React frontend
if not exist .\frontend\node_modules\ (
  echo Installing frontend dependencies...
  pushd frontend
  call npm install --no-audit --no-fund
  if errorlevel 1 (popd & goto :failed)
  popd
)
pushd frontend
call npm run build
if errorlevel 1 (popd & goto :failed)
popd

echo.
echo Validation passed. This script did not migrate or delete the database.
exit /b 0

:failed
echo.
echo Validation failed. Review the first error above; no Docker volume was deleted.
exit /b 1
