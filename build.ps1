#!/usr/bin/env pwsh
# Build + test da solução FMLab.QuickNote.
$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot
try {
    dotnet build
    if ($LASTEXITCODE -ne 0) { throw "Build falhou." }

    dotnet test
    if ($LASTEXITCODE -ne 0) { throw "Testes falharam." }
}
finally {
    Pop-Location
}
