# Test des différentes méthodes de stockage de clé
# Usage: .\Test-KeyStorage.ps1

param(
    [string]$CryptoServicePath = "..\Infrastructure\CryptoSoftIntegrationService.cs"
)

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Test des méthodes de stockage de clé" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

Write-Host "Ce script vérifie les emplacements de stockage selon l'environnement :" -ForegroundColor Gray
Write-Host ""

# Test 1 : ProtectedFile (Config/encryption.key)
Write-Host "━━━ Test 1 : ProtectedFile (Config/encryption.key) ━━━" -ForegroundColor Yellow
$appDir = $PWD.Path
$protectedPath = Join-Path $appDir "Config\encryption.key"
Write-Host "Emplacement : $protectedPath" -ForegroundColor Gray
$configDir = Split-Path $protectedPath -Parent
if (Test-Path $configDir) {
    Write-Host "✓ Dossier Config existe" -ForegroundColor Green
} else {
    Write-Host "⚠ Dossier Config sera créé automatiquement" -ForegroundColor Yellow
}

# Test 2 : LocalFile (encryption.key)
Write-Host "`n━━━ Test 2 : LocalFile (encryption.key) ━━━" -ForegroundColor Yellow
$localPath = Join-Path $appDir "encryption.key"
Write-Host "Emplacement : $localPath" -ForegroundColor Gray
Write-Host "✓ À côté de l'exécutable" -ForegroundColor Green

# Test 3 : ApplicationData (ProgramData)
Write-Host "`n━━━ Test 3 : ApplicationData (ProgramData) ━━━" -ForegroundColor Yellow
$programData = $env:ProgramData
$appDataPath = Join-Path $programData "EasySave\encryption.key"
Write-Host "Emplacement : $appDataPath" -ForegroundColor Gray
$easySaveDir = Split-Path $appDataPath -Parent
if (Test-Path $easySaveDir) {
    Write-Host "✓ Dossier ProgramData\EasySave existe" -ForegroundColor Green
} else {
    Write-Host "⚠ Dossier ProgramData\EasySave sera créé automatiquement" -ForegroundColor Yellow
}

# Vérifications système
Write-Host "`n━━━ Vérifications système ━━━" -ForegroundColor Yellow
Write-Host "OS : $([System.Environment]::OSVersion.Platform)" -ForegroundColor Gray

if ($IsWindows -or $env:OS -eq "Windows_NT") {
    Write-Host "✓ Windows détecté - DPAPI disponible" -ForegroundColor Green
    Write-Host "  → ProtectedFile utilisera le chiffrement DPAPI" -ForegroundColor Gray
} else {
    Write-Host "⚠ Système non-Windows - DPAPI non disponible" -ForegroundColor Yellow
    Write-Host "  → ProtectedFile utilisera des permissions fichier" -ForegroundColor Gray
}

# Recommandations
Write-Host "`n━━━ Recommandations ━━━" -ForegroundColor Yellow
Write-Host ""

Write-Host "Pour un poste utilisateur :" -ForegroundColor Cyan
Write-Host "  var service = new CryptoSoftIntegrationService(" -ForegroundColor Gray
Write-Host "      extensions, KeyStorageMethod.ProtectedFile);" -ForegroundColor Gray
Write-Host ""

Write-Host "Pour un serveur Windows :" -ForegroundColor Cyan
Write-Host "  var service = new CryptoSoftIntegrationService(" -ForegroundColor Gray
Write-Host "      extensions, KeyStorageMethod.ProtectedFile);" -ForegroundColor Gray
Write-Host "  // Clé protégée par DPAPI LocalMachine" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Pour un serveur Linux :" -ForegroundColor Cyan
Write-Host "  var service = new CryptoSoftIntegrationService(" -ForegroundColor Gray
Write-Host "      extensions, KeyStorageMethod.LocalFile);" -ForegroundColor Gray
Write-Host "  // Permissions Unix 600 (rw-------)" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Pour un service multi-utilisateur :" -ForegroundColor Cyan
Write-Host "  var service = new CryptoSoftIntegrationService(" -ForegroundColor Gray
Write-Host "      extensions, KeyStorageMethod.ApplicationData);" -ForegroundColor Gray
Write-Host "  // Accessible à tous les utilisateurs autorisés" -ForegroundColor DarkGray
Write-Host ""

# Test des permissions
Write-Host "━━━ Test de création de dossier ━━━" -ForegroundColor Yellow

try {
    $testDir = Join-Path $appDir "Config"
    if (!(Test-Path $testDir)) {
        New-Item -ItemType Directory -Path $testDir -Force | Out-Null
        Write-Host "✓ Dossier Config créé avec succès" -ForegroundColor Green
    } else {
        Write-Host "✓ Dossier Config existe déjà" -ForegroundColor Green
    }

    $testFile = Join-Path $testDir "test.key"
    "Test key content" | Out-File -FilePath $testFile -Force
    
    if (Test-Path $testFile) {
        Write-Host "✓ Écriture de fichier dans Config réussie" -ForegroundColor Green
        Remove-Item $testFile -Force
    }
} catch {
    Write-Host "❌ Erreur lors du test : $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Test de stockage terminé" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green

Write-Host "`nNote importante :" -ForegroundColor Yellow
Write-Host "La méthode par défaut (ProtectedFile) utilise maintenant :" -ForegroundColor Yellow
Write-Host "  [ExeDirectory]/Config/encryption.key" -ForegroundColor Yellow
Write-Host "au lieu de %AppData% pour être compatible serveur." -ForegroundColor Yellow

Write-Host ""
