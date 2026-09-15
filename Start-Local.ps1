[CmdletBinding()]
param([switch]$CheckOnly)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$webRoot = Join-Path $repoRoot 'IncidentIntelligence.Web'

function Test-LocalPort([int]$Port) {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $pending = $client.BeginConnect('127.0.0.1', $Port, $null, $null)
        if (-not $pending.AsyncWaitHandle.WaitOne(500)) { return $false }
        $client.EndConnect($pending)
        return $true
    } catch { return $false }
    finally { $client.Dispose() }
}

function Start-ServiceTerminal([string]$Title, [string]$Directory, [string]$Command) {
    # Encode the script so spaces and apostrophes in checkout paths stay literal.
    $safeDirectory = $Directory.Replace("'", "''")
    $script = "`$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location -LiteralPath '$safeDirectory'; $Command"
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
    # Visible terminals let the developer read logs and stop each service with Ctrl+C.
    Start-Process powershell.exe -WindowStyle Normal -ArgumentList @('-NoProfile', '-NoExit', '-EncodedCommand', $encoded) | Out-Null
}

try {
    foreach ($command in @('dotnet', 'npm.cmd')) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
            throw "$command was not found. Install the .NET SDK and Node.js, then restart your terminal."
        }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $webRoot 'node_modules'))) {
        throw "Frontend dependencies are missing. Run 'npm.cmd install' in IncidentIntelligence.Web first."
    }

    $config = Get-Content -LiteralPath (Join-Path $repoRoot 'IncidentIntelligence.Api/appsettings.Development.json') -Raw | ConvertFrom-Json
    $provider = $config.AI.Provider
    if ($env:AI__Provider) { $provider = $env:AI__Provider }
    if (-not $provider) { $provider = 'Ollama' }
    $ollamaUrl = $config.Ollama.BaseUrl
    if ($env:Ollama__BaseUrl) { $ollamaUrl = $env:Ollama__BaseUrl }
    if (-not $ollamaUrl) { $ollamaUrl = 'http://localhost:11434' }
    $model = $config.Ollama.Model
    if ($env:Ollama__Model) { $model = $env:Ollama__Model }
    if (-not $model) { $model = 'llama3.2:3b' }

    if ($provider -eq 'Ollama') {
        $ollama = Get-Command ollama.exe -ErrorAction SilentlyContinue
        $ollamaPath = if ($ollama) { $ollama.Source } else { Join-Path $env:LOCALAPPDATA 'Programs/Ollama/ollama.exe' }
        $tagsUrl = $ollamaUrl.TrimEnd('/') + '/api/tags'
        $tags = $null
        # Windows localhost resolution can consume most of a two-second timeout.
        $lastOllamaError = $null
        try { $tags = Invoke-RestMethod -Uri $tagsUrl -TimeoutSec 10 } catch { $lastOllamaError = $_.Exception.Message }
        if (-not $tags) {
            if (-not (Test-Path -LiteralPath $ollamaPath)) {
                throw 'Ollama is not reachable or installed at its usual location. Install and open Ollama first.'
            }
            if ($CheckOnly) { throw "Ollama is installed but not reachable at $tagsUrl. $lastOllamaError" }
            if ($ollamaUrl.TrimEnd('/') -notin @('http://localhost:11434', 'http://127.0.0.1:11434')) {
                throw "Start your configured Ollama server at $ollamaUrl, then retry."
            }
            Write-Host 'Starting Ollama...'
            $logDirectory = Join-Path ([IO.Path]::GetTempPath()) ('IncidentIntelligence-Ollama-' + [Guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Path $logDirectory | Out-Null
            $serverProcess = Start-Process -FilePath $ollamaPath -ArgumentList 'serve' -WindowStyle Hidden -PassThru `
                -RedirectStandardOutput (Join-Path $logDirectory 'stdout.log') `
                -RedirectStandardError (Join-Path $logDirectory 'stderr.log')
            $deadline = [DateTime]::UtcNow.AddSeconds(60)
            while ([DateTime]::UtcNow -lt $deadline) {
                Start-Sleep -Seconds 1
                try { $tags = Invoke-RestMethod -Uri $tagsUrl -TimeoutSec 5; break } catch { $lastOllamaError = $_.Exception.Message }
                $serverProcess.Refresh()
                if ($serverProcess.HasExited) { break }
            }
            if (-not $tags) { throw "Ollama is not responding at $tagsUrl. $lastOllamaError See startup logs in $logDirectory." }
        }
        $names = @($tags.models | ForEach-Object { $_.name })
        if ($model -notin $names -and "${model}:latest" -notin $names) {
            throw "The model '$model' is not installed. Run: & `"$ollamaPath`" pull $model"
        }
        Write-Host "Ollama ready: $model"
    }

    if ($CheckOnly) {
        Write-Host 'Startup prerequisites passed. No services were launched.'
        exit 0
    }
    if ((Test-LocalPort 7039) -or (Test-LocalPort 5252)) {
        Write-Host 'API port already in use; leaving the existing process running. Restart it manually if code changed.'
    } else {
        Start-ServiceTerminal 'Incident Intelligence API' $repoRoot 'dotnet run --project IncidentIntelligence.Api --launch-profile https'
    }
    if (Test-LocalPort 3000) {
        Write-Host 'Port 3000 already in use; leaving the existing process running.'
    } else {
        Start-ServiceTerminal 'Incident Intelligence Dashboard' $webRoot 'npm.cmd run dev -- --port 3000'
    }
    Write-Host ''
    Write-Host 'Open http://localhost:3000 once the dashboard terminal reports ready.'
    Write-Host 'Check the API terminal for startup errors. Stop each service with Ctrl+C in its window.'
    Write-Host 'Ollama stays running for future summaries.'
} catch {
    Write-Host "Startup failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
