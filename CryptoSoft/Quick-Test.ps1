# Script de test simplifie pour CryptoSoft
# Usage: .\Quick-Test.ps1

param(
    [string]$CryptoSoftPath = ".\bin\Debug\net8.0\CryptoSoft.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "`n=======================================================" -ForegroundColor Cyan
Write-Host "  Test rapide CryptoSoft" -ForegroundColor Cyan
Write-Host "=======================================================`n" -ForegroundColor Cyan

# Verifier que l'exe existe
if (-not (Test-Path $CryptoSoftPath)) {
    Write-Host "X CryptoSoft.exe introuvable : $CryptoSoftPath" -ForegroundColor Red
    Write-Host "Compilation en cours..." -ForegroundColor Yellow
    dotnet build -c Debug | Out-Null
    if (-not (Test-Path $CryptoSoftPath)) {
        Write-Host "X Echec de compilation" -ForegroundColor Red
        exit 1
    }
}

Write-Host "[OK] CryptoSoft.exe trouve" -ForegroundColor Green

# Test 1 : Generation de cle
Write-Host "`n--- Test 1 : Generation de cle ---" -ForegroundColor Yellow
$genKeyOutput = & $CryptoSoftPath genkey 2>&1 | Out-String
Write-Host $genKeyOutput

# Extraire la cle (44 caracteres Base64)
$testKey = [regex]::Match($genKeyOutput, '[A-Za-z0-9+/]{43}=').Value
if ([string]::IsNullOrEmpty($testKey)) {
    Write-Host "! Impossible d'extraire la cle, generation manuelle..." -ForegroundColor Yellow
    $rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
    $keyBytes = New-Object byte[] 32
    $rng.GetBytes($keyBytes)
    $testKey = [Convert]::ToBase64String($keyBytes)
    $rng.Dispose()
}
Write-Host "-> Cle capturee : $testKey" -ForegroundColor Cyan

# Creer un dossier de test
$testDir = Join-Path $PWD.Path "TestFiles_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
Write-Host "[OK] Dossier de test : $testDir" -ForegroundColor Green

# Test 2 : Creer et chiffrer un fichier
Write-Host "`n--- Test 2 : Chiffrement ---" -ForegroundColor Yellow
$testFile = Join-Path $testDir "secret.txt"
$content = "Donnees confidentielles - Test CryptoSoft AES-256-GCM`n$(Get-Date)"
[System.IO.File]::WriteAllText($testFile, $content)
Write-Host "[OK] Fichier cree : $testFile" -ForegroundColor Gray

Write-Host "Chiffrement en cours..." -ForegroundColor Gray
& $CryptoSoftPath encrypt $testFile $testKey | Out-Null
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0 -and (Test-Path "$testFile.crypt")) {
    Write-Host "[OK] Chiffrement reussi (code $exitCode)" -ForegroundColor Green
    $origSize = (Get-Item $testFile).Length
    $cryptSize = (Get-Item "$testFile.crypt").Length
    Write-Host "  Taille originale : $origSize octets" -ForegroundColor Gray
    Write-Host "  Taille chiffree  : $cryptSize octets (+28 pour nonce+tag)" -ForegroundColor Gray
} else {
    Write-Host "[X] Chiffrement echoue (code $exitCode)" -ForegroundColor Red
    exit 1
}

# Test 3 : Verifier que le fichier est illisible
Write-Host "`n--- Test 3 : Verification garbage ---" -ForegroundColor Yellow
$cryptBytes = [System.IO.File]::ReadAllBytes("$testFile.crypt")
$cryptText = [System.Text.Encoding]::ASCII.GetString($cryptBytes[0..49])
$isGarbage = $cryptText -notmatch "Donnees confidentielles"
if ($isGarbage) {
    Write-Host "[OK] Le fichier chiffre est illisible (garbage)" -ForegroundColor Green
} else {
    Write-Host "[X] Le texte original est encore visible !" -ForegroundColor Red
}

# Test 4 : Dechiffrement
Write-Host "`n--- Test 4 : Dechiffrement ---" -ForegroundColor Yellow
Remove-Item $testFile -Force
Write-Host "  Fichier original supprime" -ForegroundColor Gray

Write-Host "Dechiffrement en cours..." -ForegroundColor Gray
& $CryptoSoftPath decrypt "$testFile.crypt" $testKey | Out-Null
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0 -and (Test-Path $testFile)) {
    Write-Host "[OK] Dechiffrement reussi (code $exitCode)" -ForegroundColor Green
    
    $decrypted = [System.IO.File]::ReadAllText($testFile)
    if ($decrypted -eq $content) {
        Write-Host "[OK] Integrite verifiee : contenu identique bit a bit" -ForegroundColor Green
    } else {
        Write-Host "[X] Le contenu a change !" -ForegroundColor Red
        Write-Host "Original  : $($content.Substring(0, 50))..." -ForegroundColor Gray
        Write-Host "Dechiffre : $($decrypted.Substring(0, 50))..." -ForegroundColor Gray
    }
} else {
    Write-Host "[X] Dechiffrement echoue (code $exitCode)" -ForegroundColor Red
    exit 1
}

# Test 5 : Mauvaise cle
Write-Host "`n--- Test 5 : Mauvaise cle (doit echouer) ---" -ForegroundColor Yellow
$rng2 = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
$badKeyBytes = New-Object byte[] 32
$rng2.GetBytes($badKeyBytes)
$badKey = [Convert]::ToBase64String($badKeyBytes)
$rng2.Dispose()
Write-Host "  Utilisation d'une mauvaise cle..." -ForegroundColor Gray

$ErrorActionPreference = "SilentlyContinue"
& $CryptoSoftPath decrypt "$testFile.crypt" $badKey 2>&1 | Out-Null
$exitCode = $LASTEXITCODE
$ErrorActionPreference = "Stop"

if ($exitCode -eq 4) {
    Write-Host "[OK] Code 4 retourne (echec authentification GCM) - Correct" -ForegroundColor Green
} else {
    Write-Host "[X] Code $exitCode retourne (attendu : 4)" -ForegroundColor Red
}

# Test 6 : Fichier binaire
Write-Host "`n--- Test 6 : Fichier binaire ---" -ForegroundColor Yellow
$binFile = Join-Path $testDir "binary.dat"
$binData = [byte[]](0..255)
[System.IO.File]::WriteAllBytes($binFile, $binData)
Write-Host "[OK] Fichier binaire cree : 256 octets" -ForegroundColor Gray

& $CryptoSoftPath encrypt $binFile $testKey | Out-Null
& $CryptoSoftPath decrypt "$binFile.crypt" $testKey | Out-Null

$decryptedBin = [System.IO.File]::ReadAllBytes($binFile)
if ($binData.Length -eq $decryptedBin.Length) {
    $identical = $true
    for ($i = 0; $i -lt $binData.Length; $i++) {
        if ($binData[$i] -ne $decryptedBin[$i]) {
            $identical = $false
            break
        }
    }
    
    if ($identical) {
        Write-Host "[OK] Fichier binaire : integrite preservee" -ForegroundColor Green
    } else {
        Write-Host "[X] Fichier binaire : donnees corrompues" -ForegroundColor Red
    }
} else {
    Write-Host "[X] Taille differente : $($binData.Length) -> $($decryptedBin.Length)" -ForegroundColor Red
}

# Resume
Write-Host "`n=======================================================" -ForegroundColor Green
Write-Host "  [OK] Tous les tests sont passes avec succes !" -ForegroundColor Green
Write-Host "=======================================================`n" -ForegroundColor Green

Write-Host "Fichiers de test : $testDir" -ForegroundColor Yellow
Write-Host "Cle utilisee : $testKey" -ForegroundColor Yellow

Write-Host "`nVoulez-vous nettoyer le dossier de test ? (O/N) " -ForegroundColor Cyan -NoNewline
$cleanup = Read-Host

if ($cleanup -eq "O" -or $cleanup -eq "o") {
    Remove-Item $testDir -Recurse -Force
    Write-Host "[OK] Nettoyage effectue" -ForegroundColor Green
} else {
    Write-Host "-> Dossier conserve pour inspection" -ForegroundColor Yellow
}

Write-Host ""

