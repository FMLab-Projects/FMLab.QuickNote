#!/usr/bin/env pwsh
# Publica FMLab.QuickNote.App self-contained, single-file, para cada RID suportado
# (Fase 10 do TASKS.md). Saída em dist/<rid>/ (ignorado pelo git).
#
# Uso: ./publish.ps1              # todos os RIDs
#      ./publish.ps1 win-x64      # só um RID específico
param(
    [string[]]$Rids = @("win-x64", "linux-x64", "osx-x64", "osx-arm64")
)

$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot
try {
    foreach ($rid in $Rids) {
        Write-Host "==> Publicando FMLab.QuickNote.App para $rid..."
        dotnet publish src/FMLab.QuickNote.App/FMLab.QuickNote.App.csproj `
            -c Release `
            -r $rid `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -o "dist/$rid"
        if ($LASTEXITCODE -ne 0) { throw "Publish falhou para $rid." }
    }

    Write-Host "==> Publish concluído. Binários em dist/<rid>/."
}
finally {
    Pop-Location
}
