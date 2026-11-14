@echo off
cd /d "%~dp0web"
if not exist node_modules call npm ci
call npm run dev
pause
