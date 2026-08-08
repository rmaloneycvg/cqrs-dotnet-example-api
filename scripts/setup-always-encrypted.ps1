#!/usr/bin/env pwsh
# Always Encrypted Setup Script
# Encrypts PHI columns in the ClinicDb database using SQL Server Always Encrypted.
#
# Prerequisites:
#   - SqlServer PowerShell module: Install-Module SqlServer
#   - Azure Key Vault with a key created for CMK
#   - Service principal or managed identity with Key Vault Crypto User role
#   - Database with tables already created (run EF migrations first)
#
# Usage:
#   ./scripts/setup-always-encrypted.ps1 -ServerInstance "mssql-service,1433" \
#       -Database "ClinicDb" -KeyVaultUrl "https://clinic-keyvault.vault.azure.net" \
#       -KeyName "ClinicPhiMasterKey"
#
# For local dev with certificate store:
#   ./scripts/setup-always-encrypted.ps1 -ServerInstance "localhost,1433" \
#       -Database "ClinicDb" -UseCertStore

param(
    [Parameter(Mandatory)]
    [string]$ServerInstance,

    [Parameter(Mandatory)]
    [string]$Database,

    [string]$KeyVaultUrl,
    [string]$KeyName = "ClinicPhiMasterKey",

    [switch]$UseCertStore,

    [string]$SqlUser = "sa",
    [string]$SqlPassword
)

$ErrorActionPreference = "Stop"
Import-Module SqlServer

Write-Host "=== Always Encrypted Setup for $Database ===" -ForegroundColor Cyan

# ─── Step 1: Create Column Master Key ────────────────────────────────────────

if ($UseCertStore) {
    Write-Host "Creating CMK using local certificate store..." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate -Subject "CN=ClinicApi-AlwaysEncrypted" `
        -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable `
        -Type DocumentEncryptionCert -KeyUsage KeyEncipherment -KeySpec KeyExchange

    $cmkSettings = New-SqlCertificateStoreColumnMasterKeySettings `
        -CertificateStoreLocation "CurrentUser" -Thumbprint $cert.Thumbprint

    Write-Host "  CMK Certificate thumbprint: $($cert.Thumbprint)" -ForegroundColor Green
} else {
    if (-not $KeyVaultUrl) { throw "Provide -KeyVaultUrl or use -UseCertStore" }

    Write-Host "Creating CMK using Azure Key Vault: $KeyVaultUrl/keys/$KeyName" -ForegroundColor Yellow
    $cmkSettings = New-SqlAzureKeyVaultColumnMasterKeySettings -KeyUrl "$KeyVaultUrl/keys/$KeyName"
}

# ─── Step 2: Connect and create CMK/CEK in SQL Server ────────────────────────

$connParams = @{ ServerInstance = $ServerInstance; Database = $Database }
if ($SqlPassword) {
    $connParams.Username = $SqlUser
    $connParams.Password = $SqlPassword
    $connParams.TrustServerCertificate = $true
}

Write-Host "Connecting to $ServerInstance/$Database..." -ForegroundColor Yellow

# Create CMK metadata
New-SqlColumnMasterKey -Name "CMK_ClinicPhi" -ColumnMasterKeySettings $cmkSettings @connParams
Write-Host "  ✓ Column Master Key created" -ForegroundColor Green

# Create CEKs
New-SqlColumnEncryptionKey -Name "CEK_Deterministic" -ColumnMasterKey "CMK_ClinicPhi" @connParams
New-SqlColumnEncryptionKey -Name "CEK_Randomized" -ColumnMasterKey "CMK_ClinicPhi" @connParams
Write-Host "  ✓ Column Encryption Keys created" -ForegroundColor Green

# ─── Step 3: Encrypt PHI Columns ─────────────────────────────────────────────

Write-Host "Encrypting PHI columns..." -ForegroundColor Yellow

$encryptionChanges = @(
    # Deterministic (supports equality, unique index)
    New-SqlColumnEncryptionSettings -ColumnName "dbo.Patients.Email" `
        -EncryptionType "Deterministic" -EncryptionKey "CEK_Deterministic"

    # Randomized (maximum security)
    New-SqlColumnEncryptionSettings -ColumnName "dbo.Patients.FirstName" `
        -EncryptionType "Randomized" -EncryptionKey "CEK_Randomized"
    New-SqlColumnEncryptionSettings -ColumnName "dbo.Patients.LastName" `
        -EncryptionType "Randomized" -EncryptionKey "CEK_Randomized"
    New-SqlColumnEncryptionSettings -ColumnName "dbo.Patients.PhoneNumber" `
        -EncryptionType "Randomized" -EncryptionKey "CEK_Randomized"
    New-SqlColumnEncryptionSettings -ColumnName "dbo.Patients.AddressStreet" `
        -EncryptionType "Randomized" -EncryptionKey "CEK_Randomized"
    New-SqlColumnEncryptionSettings -ColumnName "dbo.Patients.InsurancePolicyNumber" `
        -EncryptionType "Randomized" -EncryptionKey "CEK_Randomized"
    New-SqlColumnEncryptionSettings -ColumnName "dbo.Appointments.Notes" `
        -EncryptionType "Randomized" -EncryptionKey "CEK_Randomized"
    New-SqlColumnEncryptionSettings -ColumnName "dbo.Prescriptions.Notes" `
        -EncryptionType "Randomized" -EncryptionKey "CEK_Randomized"
)

Set-SqlColumnEncryption -ColumnEncryptionSettings $encryptionChanges @connParams

Write-Host ""
Write-Host "=== Always Encrypted Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Encrypted columns:" -ForegroundColor Cyan
Write-Host "  Deterministic: Patients.Email (supports unique index)"
Write-Host "  Randomized:    Patients.FirstName, LastName, PhoneNumber, AddressStreet, InsurancePolicyNumber"
Write-Host "  Randomized:    Appointments.Notes, Prescriptions.Notes"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Add 'Column Encryption Setting=Enabled' to your connection string"
Write-Host "  2. Configure CMK credential access (Vault sidecar or Azure.Identity)"
Write-Host "  3. Test: dotnet test tests/ClinicApi.Integration"
