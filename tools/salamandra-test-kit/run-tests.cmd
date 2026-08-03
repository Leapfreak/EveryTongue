@echo off
setlocal
cd /d "%~dp0"
chcp 65001 >nul

if not exist model\salamandraTA_7B_inst_q4.gguf (
  echo Model missing - run download.cmd first.
  pause
  exit /b 1
)

set MODEL=model\salamandraTA_7B_inst_q4.gguf
set COMMON=-n 200 --temp 0 -c 4096 -no-cnv --no-display-prompt --simple-io

echo === SalamandraTA-7B gomets test - probing GPU (Vulkan) first...
echo     (each step loads the 5 GB model - first load is the slowest)
set BIN=bin-vulkan\llama-cli.exe
set NGL=99
"%BIN%" -m %MODEL% -f prompts\S1-bare.txt -n 4 --temp 0 -c 4096 -no-cnv --no-display-prompt -ngl %NGL% >nul 2>&1
if errorlevel 1 (
  echo     Vulkan failed or no usable GPU - falling back to CPU build.
  set BIN=bin-cpu\llama-cli.exe
  set NGL=0
) else (
  echo     Vulkan OK.
)

echo results from %BIN% ngl=%NGL% > results.txt
echo. >> results.txt
del perf.log 2>nul

for /f "usebackq delims=" %%L in ("prompts\order.txt") do (
  echo Running %%L ...
  echo === [%%L] >> results.txt
  echo === [%%L] >> perf.log
  "%BIN%" -m %MODEL% -f "prompts\%%L.txt" -ngl %NGL% %COMMON% >> results.txt 2>> perf.log
  if errorlevel 1 echo ^(FAILED - see perf.log^) >> results.txt
  echo. >> results.txt
)

echo.
echo === Done. Send back BOTH files:
echo     %~dp0results.txt
echo     %~dp0perf.log
pause
