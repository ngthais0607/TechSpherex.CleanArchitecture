param(
    [switch]$DisposableTest,
    [ValidatePattern('^snp_week4_[a-z0-9_]+$')][string]$Project = 'snp_week4_smoke',
    [ValidateRange(1024,65535)][int]$Port = 18084
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
if (-not $DisposableTest) { throw 'This creates and seeds a disposable Docker project. Pass -DisposableTest explicitly.' }
$root = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$work = Join-Path $root ".superpowers/sdd/2026-10-16-week4-api-reports-handover/$Project"
$backupDir = Join-Path $work 'backups'
$mappingDir = Join-Path $work 'migration-input'
[IO.Directory]::CreateDirectory($backupDir) | Out-Null
[IO.Directory]::CreateDirectory($mappingDir) | Out-Null
$envFile = Join-Path $work 'smoke.env'
if (-not (Test-Path -LiteralPath $envFile)) {
    $secret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
    $databasePassword = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    $adminPassword = 'Smoke9!' + [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    [IO.File]::WriteAllLines($envFile, @(
        'POSTGRES_DB=snp_api_test', 'POSTGRES_USER=week4_smoke', "POSTGRES_PASSWORD=$databasePassword",
        "Jwt__Secret=$secret", 'Jwt__Issuer=SnpWeek4Smoke', 'Jwt__Audience=SnpWeek4Smoke',
        'Admin__TenantId=DEPOT-A', 'Admin__UserName=week4admin', "Admin__Password=$adminPassword",
        'Cors__AllowedOrigins__0=https://week4-smoke.example.test', 'Redis__Required=true',
        "API_PORT=$Port", 'MIGRATION_MODE=fresh', 'BACKUP_RETENTION=7',
        "BACKUP_HOST_DIR=$($backupDir.Replace('\','/'))", "MIGRATION_INPUT_DIR=$($mappingDir.Replace('\','/'))"
    ))
}
$settings = @{}
foreach ($line in [IO.File]::ReadAllLines($envFile)) { if ($line -match '^([^#=]+)=(.*)$') { $settings[$matches[1]] = $matches[2] } }
$base = @('compose','-p',$Project,'--env-file',$envFile,'-f',(Join-Path $root 'docker-compose.prod.yml'))
function Compose([string[]]$Arguments, [switch]$AllowFailure) {
    $result = & docker @base @Arguments 2>> (Join-Path $work 'docker.log')
    $script:lastDockerExit = $LASTEXITCODE
    if ($LASTEXITCODE -ne 0 -and -not $AllowFailure) { throw "Docker command failed: $($Arguments[0]); inspect local docker.log" }
    return $result
}
function Check([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
$url = "http://127.0.0.1:$Port"
function Web([string]$Path, [string]$Method = 'GET', [string]$Body, [hashtable]$Headers = @{}) {
    $parameters = @{ Uri = "$url$Path"; Method = $Method; Headers = $Headers; SkipHttpErrorCheck = $true; TimeoutSec = 30 }
    if ($null -ne $Body -and $Body.Length -gt 0) { $parameters.Body = $Body; $parameters.ContentType = 'application/json' }
    return Invoke-WebRequest @parameters
}
function BodyText($Response) {
    if ($Response.Content -is [byte[]]) { return [Text.Encoding]::UTF8.GetString($Response.Content) }
    return [string]$Response.Content
}
Compose -Arguments @('config','--quiet') | Out-Null
Compose -Arguments @('up','-d','--build') | Out-File (Join-Path $work 'startup.log')
$live = $false
for ($i = 0; $i -lt 90; $i++) {
    try { if ((Web '/health/live').StatusCode -eq 200) { $live = $true; break } } catch { }
    Start-Sleep -Seconds 2
}
Check $live 'Liveness did not become available'
$login = Web '/api/auth/login' 'POST' (@{ userName = $settings['Admin__UserName']; password = $settings['Admin__Password'] } | ConvertTo-Json -Compress)
Check ($login.StatusCode -eq 200) 'Admin login failed'
$auth = @{ Authorization = 'Bearer ' + ($login.Content | ConvertFrom-Json).accessToken }
Check ((Web '/health/ready' -Headers $auth).StatusCode -eq 200) 'Readiness is not healthy'
Check ((Web '/health/ready').StatusCode -eq 401) 'Anonymous readiness must be unauthorized'
Check ((Web '/openapi/v1.json').StatusCode -ne 200) 'Production exposes OpenAPI'
Check ((Web '/scalar/v1').StatusCode -ne 200) 'Production exposes Scalar'
$apiId = (Compose -Arguments @('ps','-q','api') | Select-Object -Last 1).Trim()
$uid = (& docker inspect --format '{{.Config.User}}' $apiId).Trim()
Check ($uid -ne '' -and $uid -ne '0' -and $uid -ne 'root') 'API must run as non-root'
foreach ($service in @('postgres','redis')) {
    $id = (Compose -Arguments @('ps','-q',$service) | Select-Object -Last 1).Trim()
    $bindings = (& docker inspect --format '{{json .HostConfig.PortBindings}}' $id).Trim()
    Check ($bindings -eq '{}' -or $bindings -eq 'null') "$service port must not be published"
}
$allowed = Web '/api/containers' 'OPTIONS' -Headers @{ Origin = 'https://week4-smoke.example.test'; 'Access-Control-Request-Method' = 'GET' }
Check ($allowed.Headers['Access-Control-Allow-Origin'] -contains 'https://week4-smoke.example.test') 'Allowed CORS origin missing'
$blocked = Web '/health/live' -Headers @{ Origin = 'https://blocked.example.test' }
Check (-not $blocked.Headers.ContainsKey('Access-Control-Allow-Origin')) 'Blocked CORS origin received an allow header'
$oversize = Web '/api/auth/login' 'POST' ('{"userName":"x","password":"' + ('A' * (1024 * 1024)) + '"}')
Check ($oversize.StatusCode -eq 413) 'Kestrel did not reject oversized request'
Check (((BodyText $oversize) | ConvertFrom-Json).status -eq 413) 'Oversized body must return ProblemDetails'
Compose -Arguments @('stop','redis') | Out-Null
try {
    Check ((Web '/health/ready' -Headers $auth).StatusCode -eq 503) 'Required Redis outage did not fail readiness'
    Check ((Web '/health/live').StatusCode -eq 200) 'Redis outage must not fail liveness'
} finally { Compose -Arguments @('start','redis') | Out-Null }
Compose -Arguments @('stop','postgres') | Out-Null
try {
    Check ((Web '/health/ready' -Headers $auth).StatusCode -eq 503) 'PostgreSQL outage did not fail readiness'
    Check ((Web '/health/live').StatusCode -eq 200) 'Database outage must not fail liveness'
} finally { Compose -Arguments @('start','postgres') | Out-Null }
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    if ((Web '/health/ready' -Headers $auth).StatusCode -eq 200) { $ready = $true; break }
    Start-Sleep -Seconds 2
}
Check $ready 'Dependencies did not recover'
Compose -Arguments @('run','--rm','--no-deps','-e','Jwt__Secret=','api') -AllowFailure | Out-File (Join-Path $work 'missing-secret.log')
Check ($script:lastDockerExit -ne 0) 'Missing JWT secret must fail startup'
# Seed script refuses every DB except explicitly enabled disposable snp_api_test.
Compose -Arguments @('--profile','tools','run','--rm','--entrypoint','psql','backup','-X','-v','ON_ERROR_STOP=1','-c',"SET snp.allow_report_benchmark='true'",'-f','/scripts/benchmarks/seed-reports.sql') | Out-File (Join-Path $work 'seed.log')
$source = Compose -Arguments @('--profile','tools','run','--rm','--entrypoint','psql','backup','-X','-q','-v','ON_ERROR_STOP=1','-f','/scripts/verify-restore.sql')
$source | Set-Content (Join-Path $work 'restore-source.txt')
$dump = Compose -Arguments @('--profile','tools','run','--rm','backup')
$dumpPath = ($dump | Where-Object { $_ -match '^/backups/snp-depot_\d{8}T\d{6}Z\.dump$' } | Select-Object -Last 1).Trim()
Check (-not [string]::IsNullOrWhiteSpace($dumpPath)) 'Backup did not produce a completed dump'
Compose -Arguments @('--profile','tools','run','--rm','--entrypoint','sh','backup','/scripts/restore-db.sh',$dumpPath,'--confirm-target','snp_restore_drill') | Out-File (Join-Path $work 'restore.log')
$target = Compose -Arguments @('--profile','tools','run','--rm','--entrypoint','psql','backup','-X','-q','-v','ON_ERROR_STOP=1','-d','snp_restore_drill','-f','/scripts/verify-restore.sql')
$target | Set-Content (Join-Path $work 'restore-target.txt')
Check (($source -join "`n") -eq ($target -join "`n")) 'Restore counts/quota/tenant/history differ from source'
Compose -Arguments @('--profile','tools','run','--rm','--entrypoint','sh','backup','/scripts/restore-db.sh',$dumpPath,'--confirm-target','snp_api_test') -AllowFailure | Out-Null
Check ($script:lastDockerExit -ne 0) 'Restore must reject the configured application DB'
Compose -Arguments @('--profile','tools','run','--rm','--entrypoint','sh','backup','/scripts/restore-db.sh',$dumpPath,'--confirm-target','snp_restore_drill') -AllowFailure | Out-Null
Check ($script:lastDockerExit -ne 0) 'Restore must reject an existing target DB'
Set-Content (Join-Path $backupDir 'unrelated.keep') 'Preserve this unrelated file'
for ($i = 0; $i -lt 2; $i++) {
    Start-Sleep -Seconds 1
    Compose -Arguments @('--profile','tools','run','--rm','-e','BACKUP_RETENTION=2','backup') | Out-Null
}
Check (@(Get-ChildItem -LiteralPath $backupDir -Filter 'snp-depot_*.dump').Count -eq 2) 'Retention did not keep exactly two completed dumps'
Check (Test-Path -LiteralPath (Join-Path $backupDir 'unrelated.keep')) 'Pruning touched an unrelated file'
$before = @(Get-ChildItem -LiteralPath $backupDir -Filter '*.dump' | Select-Object -ExpandProperty Name)
Compose -Arguments @('--profile','tools','run','--rm','-e','PGDATABASE=missing_database','backup') -AllowFailure | Out-Null
Check ($script:lastDockerExit -ne 0) 'Invalid database backup must fail'
$after = @(Get-ChildItem -LiteralPath $backupDir -Filter '*.dump' | Select-Object -ExpandProperty Name)
Check (($before -join ',') -eq ($after -join ',')) 'Failed backup changed retained dumps'
$result = @{ measuredAtUtc = [DateTime]::UtcNow.ToString('O'); project = $Project; port = $Port; apiUid = $uid;
    startup = 'Passed'; health = 'Passed'; cors = 'Passed'; requestLimit = 'Passed'; missingSecret = 'Passed';
    backupRestore = 'Passed'; restoreGuards = 'Passed'; retention = 'Passed'; failedDumpPreservesBackups = 'Passed';
    sourceHash = (Get-FileHash (Join-Path $work 'restore-source.txt')).Hash; targetHash = (Get-FileHash (Join-Path $work 'restore-target.txt')).Hash }
$result | ConvertTo-Json | Set-Content (Join-Path $work 'result.json')
Write-Output "Production smoke and restore drill passed. Evidence: $work/result.json"
Write-Output "Disposable API remains available at $url for Postman/UAT; credentials are only in ignored smoke.env."
