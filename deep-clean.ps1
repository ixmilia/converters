$submodules = @(
    "src/IxMilia.Dwg",
    "src/IxMilia.Dxf",
    "src/IxMilia.Pdf"
)

foreach ($submodule in $submodules) {
    Write-Host "Cleaning submodule: $submodule"
    Push-Location $submodule
    git clean -dxf
    Pop-Location
}

git clean -dxf
