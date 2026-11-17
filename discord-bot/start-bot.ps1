# Lance le bot Discord du Terminus (et le relance s'il s'arrête).
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$py = Join-Path $here ".venv\Scripts\python.exe"
if (-not (Test-Path $py)) {
    py -3 -m venv (Join-Path $here ".venv")
    & $py -m pip install -r (Join-Path $here "requirements.txt")
}
while ($true) {
    & $py (Join-Path $here "bot.py")
    Write-Host "Bot arrêté (code $LASTEXITCODE), relance dans 10 s... (Ctrl+C pour quitter)"
    Start-Sleep 10
}
