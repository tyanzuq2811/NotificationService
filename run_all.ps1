# ==============================================================================
# Script khởi chạy toàn bộ hệ thống Microservices Service Thông Báo Đa Kênh (CQRS)
# Phiên bản Nâng cấp Enterprise (Polly Retry, DLQ, Quiet Hours, Developer Console)
# ==============================================================================

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " KHOI CHAY SERVICE THONG BAO DA KENH (MICROSERVICES CQRS)" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

# 0. Nap bien moi truong tu file .env (neu co)
if (Test-Path "$PSScriptRoot\.env") {
    Write-Host "[0/5] Nap cau hinh bi mat tu file .env..." -ForegroundColor Magenta
    Get-Content "$PSScriptRoot\.env" | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith('#')) {
            $parts = $line.Split('=', 2)
            if ($parts.Length -eq 2) {
                [System.Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim().Trim('"', "'"), "Process")
            }
        }
    }
}

# 1. Kiem tra Docker Daemon & Containers
Write-Host "[1/5] Kiem tra cum ha tang Docker (Kafka KRaft, SQL Server, MongoDB)..." -ForegroundColor Green
$dockerRunning = $false
try {
    $null = docker info 2>&1
    if ($LASTEXITCODE -eq 0) { $dockerRunning = $true }
} catch {
    $dockerRunning = $false
}

if (-not $dockerRunning) {
    Write-Host ">>> LUU Y: Docker Desktop chua duoc bat hoac dang khoi dong!" -ForegroundColor Yellow
    Write-Host ">>> Hay mo ung dung Docker Desktop tren man hinh Windows, cho den khi bieu tuong Docker chuyen sang mau xanh (Running) roi chay lai script nay nhe!" -ForegroundColor Yellow
    Write-Host "----------------------------------------------------------"
} else {
    Write-Host ">>> Docker dang hoat dong. Khoi dong cac containers..." -ForegroundColor Green
    docker compose up -d
    docker compose ps
}

# 2. Bien dich truoc toan bo Solution backend de tranh xung dot khoa file Common.GeneratedMSBuildEditorConfig.editorconfig
Write-Host "[2/5] Bien dich truoc toan bo Solution backend (chong loi khoa file)..." -ForegroundColor Cyan
dotnet build "$PSScriptRoot\backend\NotificationService.sln" -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host ">>> Canh bao: Bien dich Solution gap loi, vui long kiem tra code." -ForegroundColor Red
}

# 3. Khoi chay Command Service (Port 5001)
Write-Host "[3/5] Khoi chay Command Service (Port 5001 + Outbox Relay)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\backend\CommandService'; Write-Host '--- COMMAND SERVICE (Port 5001) ---' -ForegroundColor Green; dotnet run --no-build"

# 4. Khoi chay Consumer Service (Background Worker + Polly + DLQ)
Write-Host "[4/5] Khoi chay Consumer Service (Kafka & Strategy Dispatcher)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\backend\ConsumerService'; Write-Host '--- CONSUMER SERVICE (Worker) ---' -ForegroundColor Magenta; dotnet run --no-build"

# 5. Khoi chay Query Service (Port 5002 + SignalR Hub)
Write-Host "[5/5] Khoi chay Query Service (Port 5002 + SignalR Hub)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\backend\QueryService'; Write-Host '--- QUERY SERVICE (Port 5002) ---' -ForegroundColor Yellow; dotnet run --no-build"

# 5. Khoi chay Frontend Vue 3 (Modern SaaS Developer Console)
Write-Host "[5/5] Khoi chay Frontend Developer Console (Port 5173)..." -ForegroundColor Cyan
cd "$PSScriptRoot\frontend"
npm run dev
