@echo off
setlocal
cd /d "%~dp0"
echo === SalamandraTA test kit: downloading model + llama.cpp ===
echo.

if not exist model mkdir model
if not exist bin-vulkan mkdir bin-vulkan
if not exist bin-cpu mkdir bin-cpu

if exist model\salamandraTA_7B_inst_q4.gguf (
  echo Model already downloaded, skipping.
) else (
  echo Downloading model - 5.07 GB, this is the long one...
  curl.exe -L --retry 3 -o model\salamandraTA_7B_inst_q4.gguf.part "https://huggingface.co/BSC-LT/salamandraTA-7B-instruct-GGUF/resolve/main/salamandraTA_7B_inst_q4.gguf"
  if errorlevel 1 goto :fail
  move /y model\salamandraTA_7B_inst_q4.gguf.part model\salamandraTA_7B_inst_q4.gguf >nul
)

if exist bin-vulkan\llama-completion.exe (
  echo llama.cpp Vulkan build already present, skipping.
) else (
  echo Downloading llama.cpp Vulkan build - 34 MB...
  curl.exe -L --retry 3 -o llama-vulkan.zip "https://github.com/ggml-org/llama.cpp/releases/download/b10242/llama-b10242-bin-win-vulkan-x64.zip"
  if errorlevel 1 goto :fail
  tar -xf llama-vulkan.zip -C bin-vulkan
  del llama-vulkan.zip
)

if exist bin-cpu\llama-completion.exe (
  echo llama.cpp CPU build already present, skipping.
) else (
  echo Downloading llama.cpp CPU build - 18 MB fallback...
  curl.exe -L --retry 3 -o llama-cpu.zip "https://github.com/ggml-org/llama.cpp/releases/download/b10242/llama-b10242-bin-win-cpu-x64.zip"
  if errorlevel 1 goto :fail
  tar -xf llama-cpu.zip -C bin-cpu
  del llama-cpu.zip
)

echo.
echo === Done. Now run run-tests.cmd ===
pause
exit /b 0

:fail
echo.
echo *** A download failed. Check the internet connection and re-run this script.
echo *** Already-completed downloads are kept and will be skipped on re-run.
pause
exit /b 1
