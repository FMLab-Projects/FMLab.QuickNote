#!/usr/bin/env bash
# Publica FMLab.QuickNote.App self-contained, single-file, para cada RID suportado
# (Fase 10 do TASKS.md). Saída em dist/<rid>/ (ignorado pelo git).
#
# Uso: ./publish.sh                    # todos os RIDs
#      ./publish.sh win-x64 linux-x64  # só os RIDs informados
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

if [ "$#" -gt 0 ]; then
    rids=("$@")
else
    rids=(win-x64 linux-x64 osx-x64 osx-arm64)
fi

for rid in "${rids[@]}"; do
    echo "==> Publicando FMLab.QuickNote.App para ${rid}..."
    dotnet publish src/FMLab.QuickNote.App/FMLab.QuickNote.App.csproj \
        -c Release \
        -r "${rid}" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o "dist/${rid}"
done

echo "==> Publish concluído. Binários em dist/<rid>/."
