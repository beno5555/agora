<#
.SYNOPSIS
Builds Agora (frontend + self-contained Linux backend) and deploys it to the droplet. See deploy/README.md.

.EXAMPLE
./deploy/deploy.ps1 -Server 203.0.113.5 -Setup    # first time: set up the fresh droplet, then deploy

.EXAMPLE
./deploy/deploy.ps1 -Server 203.0.113.5           # every later deploy

.EXAMPLE
./deploy/deploy.ps1 -Server 203.0.113.5 -HostName agora-example.duckdns.org   # served at a name pointed at the droplet
#>
#Requires -Version 7.4
param(
    # The droplet's public IPv4 address.
    [Parameter(Mandatory)] [string] $Server,
    # The public host name, already pointed at $Server (e.g. a DuckDNS name). Without it the site is
    # served at https://<ip-with-dashes>.sslip.io; with it, that sslip.io address redirects here.
    [string] $HostName,
    # One-time server setup (Caddy, firewall, swap, the systemd service, secrets) before deploying.
    [switch] $Setup
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$address = $null
if (-not [System.Net.IPAddress]::TryParse($Server, [ref] $address) -or $address.AddressFamily -ne 'InterNetwork') {
    throw "-Server must be the droplet's IPv4 address, e.g. 203.0.113.5 (got '$Server')"
}

$repo      = Split-Path $PSScriptRoot -Parent
$sslipHost = ($Server -replace '\.', '-') + '.sslip.io'
if (-not $HostName) { $HostName = $sslipHost }
$remote    = "root@$Server"
$work      = Join-Path ([System.IO.Path]::GetTempPath()) 'agora-deploy'
$publish   = Join-Path $work 'app'
$archive   = Join-Path $work 'agora.tar.gz'
# Windows' own tar (bsdtar): Git's GNU tar would read "C:\..." as a remote host.
$tar       = Join-Path $env:SystemRoot 'System32\tar.exe'
# Files are uploaded from Windows, so strip any CRLF line endings before running them.
$stripCr   = 'sed -i ''s/\r$//'''

if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory $work | Out-Null

try {
    if ($Setup) {
        if (-not $env:Solana__RpcUrl) {
            throw 'Set $env:Solana__RpcUrl to the Helius devnet URL first; it goes into the server''s secrets file.'
        }

        Write-Host "== Setting up $remote for https://$HostName"
        $setupDir = Join-Path $work 'setup'
        New-Item -ItemType Directory $setupDir | Out-Null
        Copy-Item (Join-Path $PSScriptRoot 'setup-server.sh'), (Join-Path $PSScriptRoot 'agora.service'), (Join-Path $PSScriptRoot 'Caddyfile') $setupDir

        # The secrets file: generated here, never written into the repo, deleted locally in `finally`.
        $jwtKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
        $envLines = @(
            "Auth__Domain=$HostName"
            "AllowedHosts=$HostName;localhost"
            "Jwt__SigningKey=$jwtKey"
            "Solana__RpcUrl=$env:Solana__RpcUrl"
        )
        [System.IO.File]::WriteAllText((Join-Path $setupDir 'agora.env'), ($envLines -join "`n") + "`n")

        scp -r $setupDir "${remote}:/root/agora-setup"
        ssh $remote "$stripCr /root/agora-setup/* && bash /root/agora-setup/setup-server.sh $HostName $sslipHost; status=`$?; rm -rf /root/agora-setup; exit `$status"
    }

    Write-Host '== Building the frontend'
    Push-Location (Join-Path $repo 'frontend')
    try {
        # npm ci replaces node_modules, which fails while `ng serve` holds files there, so only run it
        # when package-lock.json changed since the last install (npm's copy is node_modules/.package-lock.json).
        $installed = 'node_modules/.package-lock.json'
        if (-not (Test-Path $installed) -or (Get-Item 'package-lock.json').LastWriteTime -gt (Get-Item $installed).LastWriteTime) {
            npm.cmd ci
        }
        npx.cmd ng build
    }
    finally {
        Pop-Location
    }

    Write-Host '== Publishing the backend (self-contained, linux-x64)'
    dotnet publish (Join-Path $repo 'backend/colosseumbackend') -c Release -r linux-x64 --self-contained -o $publish

    # A stale local wwwroot (it's git-ignored) must not ship; the fresh Angular build replaces it.
    $wwwroot = Join-Path $publish 'wwwroot'
    if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
    Copy-Item (Join-Path $repo 'frontend/dist/frontend/browser') $wwwroot -Recurse

    Write-Host '== Uploading and installing'
    & $tar -czf $archive -C $publish .
    scp $archive "${remote}:/tmp/agora.tar.gz"
    scp (Join-Path $PSScriptRoot 'install-release.sh') "${remote}:/tmp/agora-install-release.sh"
    ssh $remote "$stripCr /tmp/agora-install-release.sh && bash /tmp/agora-install-release.sh"

    Write-Host "== Checking https://$HostName/health"
    # Right after setup, Caddy may still be getting the certificate.
    for ($attempt = 1; $attempt -le 12; $attempt++) {
        try {
            Invoke-WebRequest "https://$HostName/health" -TimeoutSec 10 | Out-Null
            Write-Host "Deployed: https://$HostName"
            return
        }
        catch {
            Start-Sleep -Seconds 5
        }
    }
    throw "https://$HostName/health didn't answer. On the server: journalctl -u caddy -n 50; journalctl -u agora -n 50"
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
