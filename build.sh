#!/usr/bin/env bash
# Build + test da solução FMLab.QuickNote.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

dotnet build
dotnet test
