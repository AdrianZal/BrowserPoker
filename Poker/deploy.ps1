# --- KONFIGURACJA ---
$IP = "pi5"
$USER = "pi"
$DEST_PATH = "/home/pi/poker-app"
$SERVICE_NAME = "poker.service"

Write-Host "1. Czyszczenie i kompilacja (Release - ARM64)..." -ForegroundColor Cyan
dotnet publish -c Release -r linux-arm64 --self-contained false -o ./publish

if ($LASTEXITCODE -ne 0) { 
    Write-Host "Blad kompilacji! Przerywam." -ForegroundColor Red
    exit 
}

$REMOTE = $USER + "@" + $IP

Write-Host "2. Zatrzymywanie serwisu..." -ForegroundColor Cyan
ssh $REMOTE "sudo systemctl stop $SERVICE_NAME"

Write-Host "3. Przesylanie plikow binarnych..." -ForegroundColor Cyan
$SCP_DEST = $REMOTE + ":" + $DEST_PATH + "/"
scp -r ./publish/* $SCP_DEST

if ($LASTEXITCODE -ne 0) {
    Write-Host "Blad przesylania! Sprawdz czy folder $DEST_PATH istnieje na Pi." -ForegroundColor Red
    exit
}

Write-Host "4. Uprawnienia i Start serwisu..." -ForegroundColor Cyan
$CHMOD_CMD = "chmod +x " + $DEST_PATH + "/Poker; sudo systemctl start " + $SERVICE_NAME
ssh $REMOTE $CHMOD_CMD

Write-Host "--- GOTOWE ---" -ForegroundColor Green
ssh $REMOTE "sudo systemctl status $SERVICE_NAME"