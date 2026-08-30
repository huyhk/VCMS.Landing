$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$cssRoot = Join-Path $projectRoot "wwwroot\css"
$bundleRoot = Join-Path $cssRoot "bundles"
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)

$sharedStyles = @(
    "site.css",
    "contact.css",
    "media.css",
    "rich-content.css",
    "mobile-navigation.css",
    "content-variants.css",
    "enhanced-sections.css"
)

function Write-CssBundle([string] $outputName, [string] $templateStyle) {
    $sourceFiles = $sharedStyles + $templateStyle
    $content = ($sourceFiles | ForEach-Object {
        $sourcePath = Join-Path $cssRoot $_
        if (-not (Test-Path $sourcePath)) {
            throw "CSS source not found: $sourcePath"
        }

        "/* Source: /css/$_ */`n" + [IO.File]::ReadAllText($sourcePath)
    }) -join "`n`n"

    [IO.File]::WriteAllText((Join-Path $bundleRoot $outputName), $content + "`n", $utf8WithoutBom)
}

[IO.Directory]::CreateDirectory($bundleRoot) | Out-Null
Write-CssBundle "corporate.bundle.css" "templates/corporate.css"
Write-CssBundle "minimal.bundle.css" "templates/minimal.css"
Write-CssBundle "editorial.bundle.css" "templates/editorial.css"
Write-CssBundle "full-width.bundle.css" "templates/full-width.css"
Write-CssBundle "conversion.bundle.css" "templates/conversion.css"
