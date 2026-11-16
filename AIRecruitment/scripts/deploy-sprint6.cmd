@echo off
setlocal
cd /d "%~dp0.."

echo Rebuild and start the services. This does NOT remove SQL Server volumes.
docker compose up --build -d
if errorlevel 1 goto :failed

echo.
echo Container status:
docker compose ps

echo.
echo Recent backend log:
docker compose logs --tail=80 backend

echo.
echo Recent AI service log:
docker compose logs --tail=80 ai-service
exit /b 0

:failed
echo Deployment command failed. Review Docker output above.
exit /b 1
