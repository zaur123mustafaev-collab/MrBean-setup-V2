#!/usr/bin/env bash
# Сборка MrBeanVisualsSetup.exe — обновляет лаунчер, пакует payload, компилирует установщик.
set -e
HERE="D:/MrBeanSetup"
SRC="D:/MrBeanLauncher/src"
cd "$HERE"

echo "[1/3] dotnet build MrBeanLauncher..."
(cd "$SRC" && dotnet build MrBeanLauncher.csproj -c Release -v q >/dev/null)

echo "[2/3] упаковка payload.zip..."
rm -f payload.zip
powershell -NoProfile -Command "Compress-Archive -Force -Path '$SRC/bin/Release/net48/MrBeanLauncher.exe','$SRC/bin/Release/net48/MrBeanLauncher.exe.config','D:/MrBeanLauncher/updater/MrBeanUpdater.exe' -DestinationPath '$HERE/payload.zip'"

echo "[3/3] csc Setup.cs..."
CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
"$CSC" -nologo -target:winexe \
  -win32icon:D:/MrBeanLauncher/src/app.ico \
  -resource:payload.zip,Setup.payload.zip \
  -reference:System.dll -reference:System.Core.dll \
  -reference:System.Drawing.dll -reference:System.Windows.Forms.dll \
  -reference:System.IO.Compression.dll -reference:System.IO.Compression.FileSystem.dll \
  -out:MrBeanVisualsSetup.exe \
  Setup.cs

SHA=$(certutil -hashfile MrBeanVisualsSetup.exe SHA256 | sed -n 2p | tr -d ' \r')
echo "sha256-$SHA  MrBeanVisualsSetup.exe" > SHA256SUMS.txt
ls -la MrBeanVisualsSetup.exe
echo OK
