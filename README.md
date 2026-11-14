# Hệ Thống Thông Báo Đa Kênh Doanh Nghiệp (Multi-Channel Notification Platform)
### Kiến Trúc Microservices Hướng Sự Kiện (Event-Driven) & Phân Tách Lệnh - Truy Vấn (CQRS)

Hệ thống thông báo phân tán đa kênh quy mô doanh nghiệp được thiết kế theo tiêu chuẩn công nghiệp hiện đại, kết hợp **CQRS (Command Query Responsibility Segregation)**, **Event-Driven Architecture (EDA)**, **Transactional Outbox Pattern**, và cơ chế **Chống chịu lỗi (Fault-Tolerance)** với Apache Kafka, SQL Server, MongoDB, và .NET 8.

---

## 🏗️ Kiến Trúc Hệ Thống (Architecture Overview)

```
[ Client / Business Services / Webhook ]
                   │
                   ▼ (HTTP POST /commands)
┌────────────────────────────────────────────────────────┐
│             COMMAND SERVICE (Port 5001)                │
│  - Tiếp nhận lệnh thông báo, kiểm tra IdempotencyKey   │
│  - Ghi Transactional Outbox vào SQL Server (Atomic)    │
│  - Background Outbox Relay Worker (UPDLOCK, READPAST)  │
└──────────────────────────┬─────────────────────────────┘
                           │ Phát tán sự kiện (At-Least-Once)
                           ▼
┌────────────────────────────────────────────────────────┐
│           APACHE KAFKA CLUSTER (KRaft Mode)            │
│  - Topic: notification-events-critical (Ưu tiên cao)   │
│  - Topic: notification-events (Tiêu chuẩn)             │
│  - Topic: inapp-push-events (Realtime Push)            │
│  - Topic: notification-dlq (Dead Letter Queue)         │
└──────────────┬──────────────────────────┬──────────────┘
               │                          │
               ▼                          ▼
┌──────────────────────────────┐ ┌──────────────────────────────┐
│  CONSUMER SERVICE (Worker)   │ │  QUERY SERVICE (Port 5002)   │
│  - Strategy Multi-Channel:   │ │  - Đồng bộ Read Model        │
│    + Email (Google SMTP)     │ │  - SignalR WebSocket Hub     │
│    + Telegram Bot API        │ │  - Truy vấn tốc độ cao từ    │
│    + Web Push (W3C VAPID)    │ │    MongoDB                   │
│    + Discord / Zalo Webhook  │ └──────────────┬───────────────┘
│  - Polly Fault-Tolerance:    │                │
│    + Exponential Retry       │                │ WebSocket / REST
│    + Circuit Breaker         │                ▼
│    + Channel Fallback        │ ┌──────────────────────────────┐
│  - Cập nhật Read Model Mongo │ │  FRONTEND CONSOLE (Port 5173)│
└──────────────────────────────┘ │  Vue 3 + Vite + TailwindCSS  │
                                 └──────────────────────────────┘
```

---

## ✨ Các Tính Năng Nổi Bật

1. **Phân Tách CQRS Độc Lập:**
   - **Ghi (Write Side):** Tối ưu hóa ghi với SQL Server và Transactional Outbox, loại bỏ Distributed Transactions (2PC) nhưng vẫn đảm bảo tính nhất quán (Eventual Consistency).
   - **Đọc (Read Side):** Tối ưu hóa tốc độ truy vấn với MongoDB, cấu trúc phi chuẩn hóa (denormalized) cho lịch sử, truy vết và thống kê realtime.
2. **Transactional Outbox Pattern:**
   - Khóa dòng an toàn với `UPDLOCK, READPAST` trong SQL Server, hỗ trợ mở rộng ngang đa tiến trình Relay mà không gây tranh chấp hoặc nhân bản thông điệp.
3. **Phát Tán Đa Kênh Song Song (Strategy Pattern):**
   - **Email:** Kết nối SMTP bảo mật (Google App Password / STARTTLS).
   - **Telegram:** Bot API gửi tin nhắn thông báo tức thì.
   - **Web Push:** Chuẩn W3C Web Push Notification với cặp khóa VAPID.
   - **In-App / Realtime:** SignalR Hub đẩy thông báo trực tiếp đến UI.
4. **Khả Năng Chống Chịu Lỗi & Tự Phục Hồi (Resilience):**
   - **Polly Retry:** Thử lại tự động với Exponential Backoff khi nhà cung cấp mạng gặp sự cố tạm thời.
   - **Circuit Breaker:** Tự động ngắt mạch (Open State) bảo vệ hệ thống khi dịch vụ bên thứ ba bị gián đoạn kéo dài.
   - **Channel Fallback:** Tự động chuyển hướng kênh thay thế (Ví dụ: Telegram $\rightarrow$ Email) khi ngắt mạch.
   - **Dead Letter Queue (DLQ):** Bảo lưu toàn bộ thông điệp lỗi để kỹ sư giám sát và tái kích hoạt (Replay).
5. **Token Bucket Rate Limiting & TTL:**
   - Hạn chế nghẽn mạng bằng thuật toán Token Bucket.
   - Cơ chế tự hủy sự kiện quá hạn (Expired TTL) đối với mã OTP hoặc thông báo nhạy cảm về thời gian.

---

## 📂 Cấu Trúc Dự Án (Repository Structure)

```
NotificationService_CQRS/
├── backend/
│   ├── Common/              # Thư viện dùng chung (DTOs, Events, EnvLoader, Enums)
│   ├── CommandService/      # API tiếp nhận Command & Outbox Relay Worker
│   ├── ConsumerService/     # Worker Kafka xử lý phát tán đa kênh & Polly Resilience
│   └── QueryService/        # API truy vấn MongoDB Read Model & SignalR Hub
├── frontend/                # Giao diện điều khiển (Vue 3, Vite, Tailwind CSS, Chart.js)
├── docker-compose.yml       # Cụm hạ tầng Docker (Kafka KRaft, SQL Server, MongoDB)
├── run_all.ps1              # Kịch bản PowerShell khởi chạy toàn bộ hệ thống 1-click
├── .env.example             # File mẫu cấu hình biến môi trường
└── .gitignore               # Loại bỏ các tệp nhạy cảm và build artifacts
```

---

## 🚀 Hướng Dẫn Cài Đặt & Khởi Chạy

### 1. Yêu Cầu Tiền Đề (Prerequisites)
- **Hệ điều hành:** Windows 10/11, macOS hoặc Linux
- **Docker & Docker Compose:** Docker Desktop đang chạy
- **.NET SDK:** Phiên bản .NET 8.0 trở lên
- **Node.js:** Phiên bản 18+ và `npm`

### 2. Thiết Lập Biến Môi Trường (Environment Setup)
Tất cả các thông tin mật (Mật khẩu cơ sở dữ liệu, Token bot, Khóa VAPID) được quản lý qua biến môi trường hoặc tệp `.env`. Tệp `.env` được bảo mật và **không bao giờ commit lên Git**.

1. Sao chép tệp mẫu cấu hình:
   ```bash
   cp .env.example .env
   ```
2. Mở tệp `.env` và cập nhật thông tin cá nhân của bạn:
   - `Channels__Email__SenderEmail`: Địa chỉ Gmail của bạn.
   - `Channels__Email__Password`: Google App Password (Mật khẩu ứng dụng 16 ký tự).
   - `Channels__Telegram__BotToken`: Token Bot Telegram tạo từ `@BotFather`.
   - `VITE_VAPID_PUBLIC_KEY`: Khóa công khai VAPID cho Web Push.

### 3. Khởi Chạy Toàn Bộ Hệ Thống Với 1 Lệnh (Recommended)
Trên Windows PowerShell:
```powershell
.\run_all.ps1
```
Kịch bản tự động thực hiện:
- Kiểm tra và khởi động các container Docker (`kafka-kraft`, `sqlserver-db`, `mongodb-read`).
- Nạp cấu hình từ `.env` vào phiên làm việc.
- Biên dịch giải pháp .NET để ngăn chặn xung đột khóa tệp.
- Khởi động đồng thời 4 dịch vụ:
  - **CommandService:** `http://localhost:5001` (Swagger API: `/swagger`)
  - **QueryService:** `http://localhost:5002` (SignalR Hub: `/hubs/notifications`)
  - **ConsumerService:** Kafka Dispatcher & Strategy Hub
  - **Frontend Console:** `http://localhost:5173`

---

## 🛠️ Khởi Chạy Từng Thành Phần Thủ Công (Manual Run)

### Bước 1: Khởi động Hạ tầng Docker
```bash
docker compose up -d
```

### Bước 2: Khởi động Command Service
```bash
cd backend/CommandService
dotnet run
```

### Bước 3: Khởi động Consumer Service
```bash
cd backend/ConsumerService
dotnet run
```

### Bước 4: Khởi động Query Service
```bash
cd backend/QueryService
dotnet run
```

### Bước 5: Khởi động Frontend
```bash
cd frontend
npm install
npm run dev
```

---

## 🔒 Chính Sách Bảo Mật (Security & Configuration)
- **Bảo Mật Tuyệt Đối Thông Tin Đăng Nhập:** Không đưa thông tin mật (Secret Keys, Passwords, Connection Strings) lên kho mã nguồn.
- **Tải Cấu Hình Linh Hoạt:** Hệ thống sử dụng module `Common.EnvLoader` tự động tìm kiếm tệp `.env` từ thư mục gốc dự án và nạp vào biến môi trường hệ điều hành trước khi cấu hình `appsettings.json` được kích hoạt.
- **Thực Tiễn Môi Trường Triển Khai:** Khi triển khai trên Docker/Kubernetes/Cloud, các biến môi trường có thể được nạp trực tiếp thông qua Kubernetes Secrets hoặc CI/CD Pipeline.

---

## 👥 Tác Giả & Bản Quyền
- **Dự án:** Bài Tập Lớn / Đồ Án Kiến Trúc Phần Mềm & Hệ Thống Phân Tán (Microservices)
- **Kho lưu trữ:** [GitHub - tyanzuq2811/NotificationService](https://github.com/tyanzuq2811/NotificationService.git)
