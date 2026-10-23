#requires -Version 7.2
param([ValidatePattern('^snp-integration-e2e-[a-z0-9-]+$')][string]$Project=('snp-integration-e2e-'+(Get-Date -Format yyyyMMddHHmmss)),[ValidateRange(1024,65535)][int]$ApiPort=15002)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$scratch=Join-Path $root '.superpowers/sdd/2026-10-21-fe-be-integration'
& "$PSScriptRoot/smoke-development.ps1" -Project $Project -ApiPort $ApiPort -DemoData -RestartCount 2
$envFile=Join-Path $scratch "$Project/smoke.env"
$settings=@{}
foreach($line in [IO.File]::ReadAllLines($envFile)){if($line -match '^([^#=]+)=(.*)$'){$settings[$matches[1]]=$matches[2]}}
$override=Join-Path $scratch "$Project/short-tokens.json"
@{services=@{api=@{environment=@{Jwt__AccessTokenExpirationMinutes='1'}}}} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $override
$env:SEED_DEMO_DATA='true'
& docker compose -p $Project --env-file $envFile -f "$root/docker-compose.yml" -f $override up -d --no-build api *> (Join-Path $scratch "$Project/short-token-compose.log")
if($LASTEXITCODE -ne 0){throw 'Cannot configure isolated short-token API.'}
$deadline=[DateTime]::UtcNow.AddMinutes(2)
$taskLive=$false
do {try {if((Invoke-WebRequest "http://127.0.0.1:$ApiPort/health/live" -TimeoutSec 2).StatusCode -eq 200){$taskLive=$true;break}}catch{};Start-Sleep -Seconds 1}while([DateTime]::UtcNow -lt $deadline)
if(-not $taskLive){throw 'Isolated short-token API did not become live.'}
$proxy=Join-Path $scratch 'e2e-proxy.json'
@{'/api'=@{target="http://127.0.0.1:$ApiPort";secure=$false;changeOrigin=$true}} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $proxy
@{project=$Project;apiUrl="http://127.0.0.1:$ApiPort";baseUrl='http://127.0.0.1:14202';proxy=$proxy;adminUserName=$settings.ADMIN_USERNAME;adminPassword=$settings.ADMIN_PASSWORD;demoPassword=$settings.SEED_USER_PASSWORD;accessTokenMinutes=1} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $scratch 'e2e-config.json')
Write-Output "PASS: isolated E2E project $Project prepared; short token TTL only applies to this stack. Run npm.cmd run e2e:api --prefix frontend. Credentials remain ignored."
