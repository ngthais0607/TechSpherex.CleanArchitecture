#requires -Version 7.2
param(
    [ValidatePattern('^snp-integration-[a-z0-9-]+$')][string]$Project = 'snp-integration-dev',
    [ValidateRange(1024,65535)][int]$ApiPort = 15000,
    [switch]$DemoData,
    [ValidateRange(1,3)][int]$RestartCount = 1
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$root = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$work = Join-Path $root ".superpowers/sdd/2026-10-21-fe-be-integration/$Project"
[IO.Directory]::CreateDirectory($work) | Out-Null
$envFile = Join-Path $work 'smoke.env'
function FreePort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try { return $listener.LocalEndpoint.Port } finally { $listener.Stop() }
}
if (-not (Test-Path -LiteralPath $envFile)) {
    [IO.File]::WriteAllLines($envFile, @(
        'POSTGRES_DB=snp_integration_dev', 'POSTGRES_USER=integration',
        "POSTGRES_PASSWORD=$([Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24)))",
        "POSTGRES_PORT=$(FreePort)", "REDIS_PORT=$(FreePort)", "API_PORT=$ApiPort",
        "JWT_SECRET=$([Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(48)))",
        'JWT_ISSUER=SnpIntegration', 'JWT_AUDIENCE=SnpIntegration',
        'ADMIN_TENANT_ID=DEPOT-A', 'ADMIN_USERNAME=admin',
        "ADMIN_PASSWORD=Smoke9!$([Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(20)))",
        "SEED_USER_PASSWORD=Smoke9!$([Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(20)))"
    ))
}
$settings = @{}
foreach ($line in [IO.File]::ReadAllLines($envFile)) {
    if ($line -match '^([^#=]+)=(.*)$') { $settings[$matches[1]] = $matches[2] }
}
if ([int]$settings.API_PORT -ne $ApiPort) { throw 'Existing project uses a different API port; use another project name.' }
# Only this process receives the flag; it does not overwrite a developer's .env.
$env:SEED_DEMO_DATA = $DemoData.IsPresent.ToString().ToLowerInvariant()
$composeArgs = @('compose','-p',$Project,'--env-file',$envFile,'-f',(Join-Path $root 'docker-compose.yml'))
function Compose([string[]]$Arguments) {
    & docker @composeArgs @Arguments >> (Join-Path $work 'docker.log') 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Compose $($Arguments[0]) failed; inspect $work/docker.log locally." }
}
$url = "http://127.0.0.1:$ApiPort"
function WaitLive {
    $deadline = [DateTime]::UtcNow.AddMinutes(2)
    do {
        try {
            if ((Invoke-WebRequest "$url/health/live" -TimeoutSec 3).StatusCode -eq 200) { return }
        } catch { }
        Start-Sleep -Seconds 1
    } while ([DateTime]::UtcNow -lt $deadline)
    throw 'API did not become live; inspect the isolated project logs.'
}
function VerifyApi {
    if ($null -eq $script:authHeaders) {
        $auth = Invoke-RestMethod "$url/api/auth/login" -Method Post -ContentType 'application/json' -Body (
            @{userName=$settings.ADMIN_USERNAME; password=$settings.ADMIN_PASSWORD} | ConvertTo-Json
        )
        $script:authHeaders = @{Authorization="Bearer $($auth.accessToken)"}
        if ($DemoData) {
            foreach ($account in @(@('yard','YardPlanner'), @('order','OrderClerk'), @('gate','GateClerk'))) {
                $roleAuth = Invoke-RestMethod "$url/api/auth/login" -Method Post -ContentType 'application/json' -Body (
                    @{userName=$account[0]; password=$settings.SEED_USER_PASSWORD} | ConvertTo-Json
                )
                $roleMe = Invoke-RestMethod "$url/api/auth/me" -Headers @{Authorization="Bearer $($roleAuth.accessToken)"}
                if ($roleMe.roles -notcontains $account[1]) { throw "Unexpected role for demo user $($account[0])." }
            }
        }
    }
    $headers = $script:authHeaders
    $me = Invoke-RestMethod "$url/api/auth/me" -Headers $headers
    if ($me.tenantId -ne $settings.ADMIN_TENANT_ID) { throw 'Authenticated tenant does not match configuration.' }
    $page = Invoke-RestMethod "$url/api/containers/?pageSize=1" -Headers $headers
    if ($DemoData -and $page.totalCount -ne 50) { throw 'Expected exactly 50 seeded containers.' }
    return $page.totalCount
}
Compose -Arguments @('config','--quiet')
Compose -Arguments @('up','-d','--build','api')
WaitLive
$before = VerifyApi
for ($restart = 0; $restart -lt $RestartCount; $restart++) {
    Compose -Arguments @('restart','api')
    WaitLive
    $after = VerifyApi
    if ($after -ne $before) { throw 'Container count changed after restarting the API.' }
}
Write-Output "PASS: isolated $Project is live; login/me/database query and restart succeeded ($after containers)."
Write-Output "Project remains running at $url; credentials stay in ignored $envFile."
