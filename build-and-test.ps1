
#!/usr/bin/pwsh

[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$configuration = "Debug",
    [switch]$noTest
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Fail([string]$message) {
    throw $message
}

function Single([string]$pattern) {
    $items = @(Get-Item $pattern)
    if ($items.Length -ne 1) {
        $itemsList = $items -Join "`n"
        Fail "Expected single item, found`n$itemsList`n"
    }

    return $items[0]
}

try {
    # build dxf submodule
    $shellExt = if ($IsWindows) { "cmd" } else { "sh" }
    Push-Location "$PSScriptRoot\src\IxMilia.Dxf"
    & "./build-and-test.${shellExt}" --configuration $configuration --notest || Fail "Error building DXF submodule"
    Pop-Location

    # build dwg submodule
    Push-Location "$PSScriptRoot\src\IxMilia.Dwg"
    . .\build-and-test.ps1 -configuration $configuration -noTest
    Pop-Location

    dotnet restore || Fail "Error restoring packages"
    dotnet build --configuration $configuration || Fail "Error building"

    if (-Not $noTest) {
        dotnet test --no-restore --no-build --configuration $configuration || Fail "Error running tests"
    }

    dotnet pack --no-restore --no-build --configuration $configuration || Fail "Error building packages"
    $package = Single "$PSScriptRoot\artifacts\packages\$configuration\*.nupkg"
    Write-Host "Package generated at '$package'"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
