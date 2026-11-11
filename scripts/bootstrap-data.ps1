# ============================================================================
# scripts/bootstrap-data.ps1
# 1. Laptop IP ko Postgres firewall mein allow karta hai
# 2. Har service ka random password banakar Key Vault mein rakhta hai
# 3. Postgres users aur database owners banata hai (bootstrap-db.sql)
# 4. Firewall rule hata deta hai
# Har native command ka exit code check hota hai. Koi step fail ho to script ruk jati hai.
# ============================================================================
param(
    [Parameter(Mandatory)][string]$Rg,
    [Parameter(Mandatory)][string]$PgServer,
    [Parameter(Mandatory)][string]$KeyVault,
    [string]$PgAdminLogin = 'pgadmin'
)

$ErrorActionPreference = 'Stop'

# Native command ke baad exit code check karne ka helper
function Assert-Ok([string]$step) {
    if ($LASTEXITCODE -ne 0) { throw "FAILED at: $step (exit code $LASTEXITCODE)" }
}

$pgFqdn = az postgres flexible-server show -g $Rg -n $PgServer --query fullyQualifiedDomainName -o tsv
Assert-Ok 'get FQDN'

$myIp = (Invoke-RestMethod -Uri 'https://api.ipify.org').Trim()
Write-Host "Laptop IP: $myIp" -ForegroundColor Cyan

# Firewall rule (sahi flags: --server-name, --name)
az postgres flexible-server firewall-rule create -g $Rg --server-name $PgServer `
    --name bootstrap-laptop --start-ip-address $myIp --end-ip-address $myIp --output none
Assert-Ok 'firewall create'

# Random password generator (confusing characters 0/O, 1/l/I hata diye)
$chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789'.ToCharArray()
function New-Pw { -join (1..24 | ForEach-Object { $chars | Get-Random }) }

$services = @('catalog', 'inventory', 'payment', 'notification', 'orders')
$pws = @{}

foreach ($s in $services) {
    $pw = New-Pw
    $pws[$s] = $pw
    az keyvault secret set --vault-name $KeyVault --name "$s-db-connection" --output none `
        --value "Host=$pgFqdn;Port=5432;Database=$($s)_db;Username=$s;Password=$pw;SSL Mode=Require"
    Assert-Ok "keyvault set $s"
}

az keyvault secret set --vault-name $KeyVault --name 'rabbitmq-username' --value 'sangh' --output none
Assert-Ok 'keyvault rabbit user'
az keyvault secret set --vault-name $KeyVault --name 'rabbitmq-password' --value (New-Pw) --output none
Assert-Ok 'keyvault rabbit pw'

$adminSecure = Read-Host "Postgres admin password ($PgAdminLogin)" -AsSecureString
$adminPlain  = [System.Net.NetworkCredential]::new('', $adminSecure).Password

$psqlVars = @()
foreach ($s in $services) {
    $psqlVars += '-v'
    $psqlVars += "$($s)_pw=$($pws[$s])"
}

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

docker run --rm -e "PGPASSWORD=$adminPlain" -v "${root}\scripts:/scripts" postgres:16 `
    psql "host=$pgFqdn port=5432 dbname=postgres user=$PgAdminLogin sslmode=require" `
    @psqlVars -v ON_ERROR_STOP=1 -f /scripts/bootstrap-db.sql
Assert-Ok 'bootstrap-db.sql'

Remove-Variable adminPlain, adminSecure -ErrorAction SilentlyContinue

az postgres flexible-server firewall-rule delete -g $Rg --server-name $PgServer `
    --name bootstrap-laptop --yes --output none
Assert-Ok 'firewall delete'

Write-Host "Bootstrap complete. Passwords sirf Key Vault mein hain." -ForegroundColor Green
