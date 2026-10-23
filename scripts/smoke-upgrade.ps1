param([switch]$DisposableTest, [ValidatePattern('^snp_week4_[a-z0-9_]+$')][string]$Project = 'snp_week4_smoke',
    [ValidatePattern('^snp_upgrade_[a-z0-9_]+$')][string]$DatabaseName = 'snp_upgrade_test')
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
if (-not $DisposableTest) { throw 'Pass -DisposableTest; this adds a synthetic upgrade DB to the smoke PostgreSQL only.' }
$root = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$work = Join-Path $root ".superpowers/sdd/2026-10-16-week4-api-reports-handover/$Project"
$envFile = Join-Path $work 'smoke.env'
if (-not (Test-Path -LiteralPath $envFile)) { throw 'Run smoke-production.ps1 first' }
$base = @('compose','-p',$Project,'--env-file',$envFile,'-f',(Join-Path $root 'docker-compose.prod.yml'))
function Compose([string[]]$Arguments, [switch]$AllowFailure) {
    $result = & docker @base @Arguments 2>> (Join-Path $work 'upgrade-docker.log')
    $script:lastDockerExit = $LASTEXITCODE
    if ($LASTEXITCODE -ne 0 -and -not $AllowFailure) { throw "Upgrade command failed; see upgrade-docker.log" }
    return $result
}
function Check([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
Compose -Arguments @('--profile','tools','run','--rm','--entrypoint','createdb','backup','--maintenance-db=postgres',$DatabaseName) | Out-Null
$override = Join-Path $work 'upgrade.env'
$mappingDir = Join-Path $work "upgrade-input/$DatabaseName"
[IO.Directory]::CreateDirectory($mappingDir) | Out-Null
[IO.File]::WriteAllLines($override, @("POSTGRES_DB=$DatabaseName",'MIGRATION_MODE=upgrade',"MIGRATION_INPUT_DIR=$($mappingDir.Replace('\','/'))"))
$base = @('compose','-p',$Project,'--env-file',$envFile,'--env-file',$override,'-f',(Join-Path $root 'docker-compose.prod.yml'))
Compose -Arguments @('run','--rm','--no-deps','--entrypoint','/app/efbundle','migrate','20261012122306_AddVehicleTypeToContainerLifecycle') | Out-File (Join-Path $work 'upgrade-baseline.log')
$insert = "INSERT INTO application_users(id,user_name,email,first_name,last_name,password_hash,role,is_active,created_at) VALUES ('11111111-2222-3333-4444-555555555555','legacy_smoke','legacy@example.test','Legacy','Smoke','disabled-test-account','Admin',false,now())"
Compose -Arguments @('run','--rm','--no-deps','--entrypoint','psql','migrate','-X','-v','ON_ERROR_STOP=1','-c',$insert) | Out-Null
Compose -Arguments @('run','--rm','--no-deps','migrate') -AllowFailure | Out-File (Join-Path $work 'upgrade-missing-map.log')
Check ($script:lastDockerExit -ne 0) 'Upgrade must stop without mapping'
$map = Join-Path $mappingDir 'user-tenants.csv'
[IO.File]::WriteAllLines($map, @('user_id,tenant_id','aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee,DEPOT-WRONG'))
Compose -Arguments @('run','--rm','--no-deps','migrate') -AllowFailure | Out-File (Join-Path $work 'upgrade-invalid-map.log')
Check ($script:lastDockerExit -ne 0) 'Upgrade must reject incomplete/unknown mapping'
$contract = Compose -Arguments @('run','--rm','--no-deps','--entrypoint','psql','migrate','-X','-tAc','SELECT count(*) FROM "__EFMigrationsHistory" WHERE migration_id=''20261016012629_RequireUserTenant''')
Check (($contract -join '').Trim() -eq '0') 'Contract migration ran before mapping passed'
[IO.File]::WriteAllLines($map, @('user_id,tenant_id','11111111-2222-3333-4444-555555555555,DEPOT-B'))
Compose -Arguments @('run','--rm','--no-deps','migrate') | Out-File (Join-Path $work 'upgrade-valid-map.log')
Compose -Arguments @('run','--rm','--no-deps','migrate') | Out-File (Join-Path $work 'upgrade-repeat.log')
$mapped = Compose -Arguments @('run','--rm','--no-deps','--entrypoint','psql','migrate','-X','-tAc',"SELECT count(*) FROM application_users WHERE tenant_id='DEPOT-B' AND id='11111111-2222-3333-4444-555555555555'")
Check (($mapped -join '').Trim() -eq '1') 'Approved synthetic mapping was not preserved'
$expectedMigrations = @(Get-ChildItem (Join-Path $root 'src/Infrastructure/Persistence/Migrations') -Filter '*.cs' |
    Where-Object { $_.Name -match '^\d{14}_[^.]+\.cs$' } | ForEach-Object { $_.BaseName } | Sort-Object)
$migrations = @(Compose -Arguments @('run','--rm','--no-deps','--entrypoint','psql','migrate','-X','-tAc','SELECT migration_id FROM "__EFMigrationsHistory" ORDER BY migration_id') |
    ForEach-Object { $_.Trim() } | Where-Object { $_ })
Check ($expectedMigrations.Count -gt 0 -and -not (Compare-Object $expectedMigrations $migrations)) 'Upgrade did not apply exactly the source migrations'
@{ measuredAtUtc = [DateTime]::UtcNow.ToString('O'); project = $Project; database = $DatabaseName;
    missingMap = 'Rejected'; invalidMap = 'Rejected'; validMap = 'Passed'; repeat = 'Passed'; migrations = $migrations.Count } |
    ConvertTo-Json | Set-Content (Join-Path $work 'upgrade-result.json')
Write-Output "Upgrade drill passed. Evidence: $work/upgrade-result.json"
