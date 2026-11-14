# ==============================================================================
# Kịch bản khởi chạy toàn bộ hệ thống Microservices Thông Báo Đa Kênh (CQRS)
# Phiên bản Nâng cấp Doanh nghiệp (Polly Retry, DLQ, Quiet Hours, Developer Console)
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " KHỞI CHẠY HỆ THỐNG THÔNG BÁO ĐA KÊNH (MICROSERVICES CQRS)" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

# 0. Nạp biến môi trường từ tệp .env (nếu có)
if (Test-Path "$PSScriptRoot\.env") {
    Write-Host "[0/5] Nạp cấu hình bảo mật từ tệp .env..." -ForegroundColor Magenta
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

# 1. Kiểm tra Docker Daemon & Containers
Write-Host "[1/5] Kiểm tra cụm hạ tầng Docker (Kafka KRaft, SQL Server, MongoDB)..." -ForegroundColor Green
$dockerRunning = $false
try {
    $null = docker info 2>&1
    if ($LASTEXITCODE -eq 0) { $dockerRunning = $true }
} catch {
    $dockerRunning = $false
}

if (-not $dockerRunning) {
    Write-Host ">>> LƯU Ý: Docker Desktop chưa được bật hoặc đang khởi động!" -ForegroundColor Yellow
    Write-Host ">>> Hãy mở ứng dụng Docker Desktop trên màn hình Windows, chờ đến khi biểu tượng Docker chuyển sang màu xanh (Running) rồi chạy lại kịch bản này nhé!" -ForegroundColor Yellow
    Write-Host "----------------------------------------------------------"
} else {
    Write-Host ">>> Docker đang hoạt động. Khởi động các container..." -ForegroundColor Green
    docker compose up -d
    docker compose ps
}

# 2. Biên dịch trước toàn bộ Solution backend để chống xung đột khóa tệp
Write-Host "[2/5] Biên dịch trước toàn bộ Solution backend (chống lỗi khóa tệp)..." -ForegroundColor Cyan
dotnet build "$PSScriptRoot\backend\NotificationService.sln" -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host ">>> Cảnh báo: Biên dịch Solution gặp lỗi, vui lòng kiểm tra mã nguồn." -ForegroundColor Red
}

# 3. Khởi chạy Command Service (Cổng 5001)
Write-Host "[3/5] Khởi chạy Command Service (Cổng 5001 + Outbox Relay)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; cd '$PSScriptRoot\backend\CommandService'; Write-Host '--- COMMAND SERVICE (Cổng 5001) ---' -ForegroundColor Green; dotnet run --no-build"

# 4. Khởi chạy Consumer Service (Kafka & Phân phối đa kênh)
Write-Host "[4/5] Khởi chạy Consumer Service (Kafka & Phân phối đa kênh)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; cd '$PSScriptRoot\backend\ConsumerService'; Write-Host '--- CONSUMER SERVICE (Tiến trình ngầm) ---' -ForegroundColor Magenta; dotnet run --no-build"

# 5. Khởi chạy Query Service (Cổng 5002 + SignalR Hub)
Write-Host "[5/5] Khởi chạy Query Service (Cổng 5002 + SignalR Hub)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; cd '$PSScriptRoot\backend\QueryService'; Write-Host '--- QUERY SERVICE (Cổng 5002) ---' -ForegroundColor Yellow; dotnet run --no-build"

# 6. Khởi chạy Giao diện điều khiển Frontend Vue 3
Write-Host "[6/6] Khởi chạy Giao diện Frontend Developer Console (Cổng 5173)..." -ForegroundColor Cyan
cd "$PSScriptRoot\frontend"
npm run dev
