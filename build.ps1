$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The Windows .NET Framework C# compiler was not found.'
}
$source = Join-Path $PSScriptRoot 'TinyVolumeKeys.cs'
$output = Join-Path $PSScriptRoot 'TinyVolumeKeys.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /warn:4 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll "/out:$output" $source
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Get-Item -LiteralPath $output | Select-Object FullName, Length
