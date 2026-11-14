# Hệ Thống Thông Báo Đa Kênh Doanh Nghiệp (Multi-Channel Notification Platform)
## Kiến Trúc Microservices Hướng Sự Kiện (Event-Driven) & Phân Tách Lệnh - Truy Vấn (CQRS)

Hệ thống thông báo phân tán đa kênh quy mô doanh nghiệp được thiết kế theo tiêu chuẩn kỹ thuật hiện đại, kết hợp CQRS (Command Query Responsibility Segregation), Event-Driven Architecture (EDA), Transactional Outbox Pattern và cơ chế chống chịu lỗi (Fault-Tolerance) với Apache Kafka, SQL Server, MongoDB và .NET 8.

---

## 1. Kiến Trúc Tổng Quan

```
[ Client / Business Services / Webhook ]
                   |
                   v (HTTP POST /commands)
+--------------------------------------------------------+
|             COMMAND SERVICE (Port 5001)                |
|  - Tiếp nhận lệnh thông báo, kiểm tra IdempotencyKey   |
|  - Ghi Transactional Outbox vào SQL Server (Atomic)    |
|  - Background Outbox Relay Worker (UPDLOCK, READPAST)  |
+--------------------------+-----------------------------+
                           | Phát tán sự kiện (At-Least-Once)
                           v
+--------------------------------------------------------+
|           APACHE KAFKA CLUSTER (KRaft Mode)            |
|  - Topic: notification-events-critical (Ưu tiên cao)   |
|  - Topic: notification-events (Tiêu chuẩn)             |
|  - Topic: inapp-push-events (Realtime Push)            |
|  - Topic: notification-dlq (Dead Letter Queue)         |
+--------------------------+-----------------------------+
               |                          |
               v                          v
+------------------------------+ +------------------------------+
|  CONSUMER SERVICE (Worker)   | |  QUERY SERVICE (Port 5002)   |
|  - Strategy Multi-Channel:   | |  - Đồng bộ Read Model        |
|    + Email (Google SMTP)     | |  - SignalR WebSocket Hub     |
|    + Telegram Bot API        | |  - Truy vấn tốc độ cao từ    |
|    + Web Push (W3C VAPID)    | |    MongoDB                   |
|    + Discord / Zalo Webhook  | +--------------+---------------+
|  - Polly Fault-Tolerance:    |                |
|    + Exponential Retry       |                | WebSocket / REST
|    + Circuit Breaker         |                v
|    + Channel Fallback        | +------------------------------+
|  - Cập nhật Read Model Mongo | |  FRONTEND CONSOLE (Port 5173)|
+------------------------------+ |  Vue 3 + Vite + TailwindCSS  |
                                 +------------------------------+
```

---

## 2. Đặc Điểm Kỹ Thuật Cốt Lõi

1. **Phân Tách CQRS Triệt Để:**
   - **Ghi (Write Stack):** Xử lý nghiệp vụ và ghi vào SQL Server thông qua mô hình Transactional Outbox, đảm bảo tính nhất quán cuối cùng (Eventual Consistency) mà không cần giao dịch phân tán (2PC).
   - **Đọc (Read Stack):** Sử dụng MongoDB được tối ưu hóa cho truy vấn đọc phi chuẩn hóa (denormalized), phục vụ hiển thị hộp thư, truy vết nhật ký và thống kê thời gian thực.

2. **Mô Hình Transactional Outbox:**
   - Sử dụng cơ chế khóa dòng `UPDLOCK, READPAST` trên SQL Server giúp các Outbox Relay Worker hoạt động song song mà không gây tranh chấp (race condition) hay trùng lặp thông điệp.

3. **Chiến Lược Phát Tán Đa Kênh (Strategy Pattern):**
   - **Email:** Giao thức SMTP kết hợp xác thực TLS/STARTTLS.
   - **Telegram:** Telegram Bot API gửi tin nhắn trực tiếp theo Chat ID.
   - **Web Push:** Chuẩn W3C Push API tích hợp cặp khóa VAPID cho thông báo hệ thống.
   - **In-App:** SignalR WebSocket Hub truyền tin nhắn trực tiếp tới trình duyệt người dùng.
   - **Webhook:** Phát sự kiện tới các bên thứ ba (Discord, Zalo ZNS).

4. **Cơ Chế Chống Chịu Lỗi & Tự Phục Hồi (Resilience):**
   - **Polly Retry:** Thử lại tự động với Exponential Backoff khi gặp lỗi mạng tạm thời.
   - **Circuit Breaker:** Tự động ngắt mạch (Open State) khi tỷ lệ lỗi vượt ngưỡng, bảo vệ hệ thống khỏi sự cố sụp đổ dây chuyền (cascading failure).
   - **Channel Fallback:** Tự động điều hướng sang kênh dự phòng khi kênh chính bị gián đoạn.
   - **Dead Letter Queue (DLQ):** Lưu trữ các thông điệp lỗi không thể phục hồi để quản trị viên phân tích và phát lại (Replay).

5. **Kiểm Soát Tải & Giới Hạn Thời Gian (Rate Limiting & TTL):**
   - Áp dụng thuật toán Token Bucket để kiểm soát lưu lượng phát tán.
   - Tự động hủy các sự kiện hết hạn (TTL) đối với mã OTP và thông báo khẩn cấp.

---

## 3. Cấu Trúc Thư Mục Dự Án

```
NotificationService_CQRS/
├── backend/
│   ├── Common/              # Thư viện dùng chung (DTOs, Events, EnvLoader, Enums)
│   ├── CommandService/      # API tiếp nhận Command & Outbox Relay Worker
│   ├── ConsumerService/     # Worker Kafka xử lý phát tán đa kênh & Polly Resilience
│   └── QueryService/        # API truy vấn MongoDB Read Model & SignalR Hub
├── frontend/                # Giao diện điều khiển (Vue 3, Vite, Tailwind CSS, Chart.js)
├── docker-compose.yml       # Cụm hạ tầng Docker (Kafka KRaft, SQL Server, MongoDB)
├── run_all.ps1              # Kịch bản PowerShell khởi chạy toàn bộ hệ thống
├── .env.example             # Tệp mẫu cấu hình biến môi trường
└── .gitignore               # Loại bỏ các tệp nhạy cảm và build artifacts
```

---

## 4. Yêu Cầu Hệ Thống

- Hệ điều hành: Windows 10/11, Linux hoặc macOS
- Docker Desktop (hỗ trợ Docker Compose v2)
- .NET 8.0 SDK trở lên
- Node.js phiên bản 18 trở lên và npm

---

## 5. Thiết Lập Cấu Hình & Biến Môi Trường

Dự án áp dụng nguyên tắc bảo mật không commit thông tin nhạy cảm (mật khẩu, token, connection string) lên Git. Các thông số được đọc từ biến môi trường hoặc tệp `.env`.

1. Tạo tệp `.env` từ tệp mẫu:
   ```bash
   cp .env.example .env
   ```

2. Cập nhật các giá trị cần thiết trong tệp `.env`:
   - `Channels__Email__SenderEmail`: Địa chỉ Gmail gửi thư.
   - `Channels__Email__Password`: Mật khẩu ứng dụng Google (App Password 16 ký tự).
   - `Channels__Telegram__BotToken`: Token Bot Telegram từ BotFather.
   - `VITE_VAPID_PUBLIC_KEY`: Khóa công khai VAPID cho Web Push.

---

## 6. Hướng Dẫn Khởi Chạy

### Cách 1: Khởi chạy tự động (Khuyến nghị trên Windows)

Chạy file kịch bản duy nhất:
```powershell
.\run_all.ps1
```

Kịch bản sẽ tự động:
- Khởi động cụm Docker container (`kafka-kraft`, `sqlserver-db`, `mongodb-read`).
- Nạp biến môi trường từ `.env`.
- Biên dịch solution backend.
- Khởi chạy đồng thời CommandService (port 5001), QueryService (port 5002), ConsumerService và Frontend Console (port 5173).

### Cách 2: Khởi chạy thủ công từng thành phần

1. Khởi động hạ tầng Docker:
   ```bash
   docker compose up -d
   ```

2. Khởi chạy Command Service:
   ```bash
   cd backend/CommandService
   dotnet run
   ```

3. Khởi chạy Consumer Service:
   ```bash
   cd backend/ConsumerService
   dotnet run
   ```

4. Khởi chạy Query Service:
   ```bash
   cd backend/QueryService
   dotnet run
   ```

5. Khởi chạy Frontend Console:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

---

## 7. Địa Chỉ Các Dịch Vụ

- Command Service API (Swagger): `http://localhost:5001/swagger`
- Query Service API (Swagger): `http://localhost:5002/swagger`
- SignalR Realtime Hub: `http://localhost:5002/hubs/notifications`
- Frontend Developer Console: `http://localhost:5173`
- Apache Kafka Bootstrap Server: `localhost:9092`
- SQL Server (Write DB): `localhost:1433`
- MongoDB (Read DB): `localhost:27017`

---

## 8. Chính Sách Bảo Mật

- Tệp `.env` và các tệp build (`bin/`, `obj/`, `node_modules/`) được loại trừ hoàn toàn khỏi Git thông qua `.gitignore`.
- Lớp `Common.EnvLoader` tự động tìm kiếm tệp `.env` ở thư mục gốc và nạp vào runtime môi trường của tiến trình.
- Khi triển khai môi trường production (Kubernetes/Docker Swarm), các biến môi trường được truyền trực tiếp thông qua cơ chế ConfigMap hoặc Secret.
