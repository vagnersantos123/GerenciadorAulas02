param([string]$ToolsRoot = (Join-Path $env:LOCALAPPDATA 'GA003'))
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $ToolsRoot 'net10\dotnet.exe'
$android = Join-Path $ToolsRoot 'android'
$jdk = Get-ChildItem -LiteralPath (Join-Path $ToolsRoot 'jdk21') -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'bin\java.exe') } | Select-Object -First 1
if (!(Test-Path -LiteralPath $dotnet) -or !$jdk -or !(Test-Path -LiteralPath (Join-Path $android 'platforms\android-36\android.jar'))) {
    throw 'Ferramentas isoladas incompletas. Consulte docs/VALIDACAO_003.md. Este script nao instala componentes.'
}
$previousRoot = $env:DOTNET_ROOT
$previousJava = $env:JAVA_HOME
try {
    $env:DOTNET_ROOT = Split-Path -Parent $dotnet
    $env:JAVA_HOME = $jdk.FullName
    & $dotnet build (Join-Path $repository 'src\GerenciadorAulas.App') -f net10.0-android -p:BuildAndroidOnly=true -c Debug "-p:AndroidSdkDirectory=$android" "-p:JavaSdkDirectory=$($jdk.FullName)" -p:AndroidPackageFormats=apk -p:EmbedAssembliesIntoApk=true
    if ($LASTEXITCODE -ne 0) { throw "Compilacao Android falhou (codigo $LASTEXITCODE)." }
    $apk = Join-Path $repository 'src\GerenciadorAulas.App\bin\Debug\net10.0-android\br.com.gerenciadoraulas.app-Signed.apk'
    if (!(Test-Path -LiteralPath $apk)) { throw 'A compilacao terminou sem produzir o APK assinado esperado.' }
    $outputDirectory = Join-Path $repository 'artifacts\apk'
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    $output = Join-Path $outputDirectory 'GerenciadorAulas-V003-Debug.apk'
    Copy-Item -LiteralPath $apk -Destination $output -Force
    Get-Item -LiteralPath $output | Select-Object FullName, Length
    Get-FileHash -LiteralPath $output -Algorithm SHA256
}
finally {
    $env:DOTNET_ROOT = $previousRoot
    $env:JAVA_HOME = $previousJava
}
