# He Thong Thong Bao Da Kenh Doanh Nghiep (Multi-Channel Notification Platform)
## Kien Truc Microservices Huong Su Kien (Event-Driven) & Phan Tach Lenh - Truy Van (CQRS)

He thong thong bao phan tan da kenh quy mo doanh nghiep duoc thiet ke theo tieu chuan ky thuat hien dai, ket hop CQRS (Command Query Responsibility Segregation), Event-Driven Architecture (EDA), Transactional Outbox Pattern va co che chong chiu loi (Fault-Tolerance) voi Apache Kafka, SQL Server, MongoDB va .NET 8.

---

## 1. Kien Truc Tong Quan

```
[ Client / Business Services / Webhook ]
                   |
                   v (HTTP POST /commands)
+--------------------------------------------------------+
|             COMMAND SERVICE (Port 5001)                |
|  - Tiep nhan lenh thong bao, kiem tra IdempotencyKey   |
|  - Ghi Transactional Outbox vao SQL Server (Atomic)    |
|  - Background Outbox Relay Worker (UPDLOCK, READPAST)  |
+--------------------------+-----------------------------+
                           | Phat tan su kien (At-Least-Once)
                           v
+--------------------------------------------------------+
|           APACHE KAFKA CLUSTER (KRaft Mode)            |
|  - Topic: notification-events-critical (Uu tien cao)   |
|  - Topic: notification-events (Tieu chuan)             |
|  - Topic: inapp-push-events (Realtime Push)            |
|  - Topic: notification-dlq (Dead Letter Queue)         |
+--------------------------+-----------------------------+
               |                          |
               v                          v
+------------------------------+ +------------------------------+
|  CONSUMER SERVICE (Worker)   | |  QUERY SERVICE (Port 5002)   |
|  - Strategy Multi-Channel:   | |  - Dong bo Read Model        |
|    + Email (Google SMTP)     | |  - SignalR WebSocket Hub     |
|    + Telegram Bot API        | |  - Truy van toc do cao tu    |
|    + Web Push (W3C VAPID)    | |    MongoDB                   |
|    + Discord / Zalo Webhook  | +--------------+---------------+
|  - Polly Fault-Tolerance:    |                |
|    + Exponential Retry       |                | WebSocket / REST
|    + Circuit Breaker         |                v
|    + Channel Fallback        | +------------------------------+
|  - Cap nhat Read Model Mongo | |  FRONTEND CONSOLE (Port 5173)|
+------------------------------+ |  Vue 3 + Vite + TailwindCSS  |
                                 +------------------------------+
```

---

## 2. Dac Diem Ky Thuat Cot Loi

1. **Phan Tach CQRS Triet De:**
   - **Ghi (Write Stack):** Xu ly nghiep vu va ghi vao SQL Server thong qua mo hinh Transactional Outbox, dam bao tinh nhat quan cuoi cung (Eventual Consistency) ma khong can giao dich phan tan (2PC).
   - **Doc (Read Stack):** Su dung MongoDB duoc toi uu hoa cho truy van doc phi chuan hoa (denormalized), phuc vu hien thi hop thu, truy vet nhat ky va thong ke thoi gian thuc.

2. **Mo Hinh Transactional Outbox:**
   - Su dung co che khoa dong `UPDLOCK, READPAST` tren SQL Server giup cac Outbox Relay Worker hoat dong song song ma khong gay tranh chap (race condition) hay trung lap thong diep.

3. **Chien Luoc Phat Tan Da Kenh (Strategy Pattern):**
   - **Email:** Giao thuc SMTP ket hop xac thuc TLS/STARTTLS.
   - **Telegram:** Telegram Bot API gui tin nhan truc tiep theo Chat ID.
   - **Web Push:** Chuan W3C Push API tich hop cap khoa VAPID cho thong bao he thong.
   - **In-App:** SignalR WebSocket Hub truyen tin nhan truc tiep toi trinh duyet nguoi dung.
   - **Webhook:** Phat su kien toi cac ben thu ba (Discord, Zalo ZNS).

4. **Co Che Chong Chiu Loi & Tu Phuc Hoi (Resilience):**
   - **Polly Retry:** Thu lai tu dong voi Exponential Backoff khi gap loi mang tam thoi.
   - **Circuit Breaker:** Tu dong ngat mach (Open State) khi ty le loi vuot nguong, bao ve he thong khoi cascading failure.
   - **Channel Fallback:** Tu dong dieu huong sang kenh du phong khi kenh chinh bi gian doan.
   - **Dead Letter Queue (DLQ):** Luu tru cac thong diep loi khong the phuc hoi de quan tri vien phan tich va phat lai (Replay).

5. **Kiem Soat Tai & Gioi Han Thoi Gian (Rate Limiting & TTL):**
   - Ap dung thuat toan Token Bucket de kiem soat luu luong phat tan.
   - Tu dong huy cac su kien het han (TTL) doi voi ma OTP va thong bao khan cap.

---

## 3. Cau Truc Thu Muc Du An

```
NotificationService_CQRS/
├── backend/
│   ├── Common/              # Thu vien dung chung (DTOs, Events, EnvLoader, Enums)
│   ├── CommandService/      # API tiep nhan Command & Outbox Relay Worker
│   ├── ConsumerService/     # Worker Kafka xu ly phat tan da kenh & Polly Resilience
│   └── QueryService/        # API truy van MongoDB Read Model & SignalR Hub
├── frontend/                # Giao dien dieu khien (Vue 3, Vite, Tailwind CSS, Chart.js)
├── docker-compose.yml       # Cum ha tang Docker (Kafka KRaft, SQL Server, MongoDB)
├── run_all.ps1              # Kich ban PowerShell khoi chay toan bo he thong
├── .env.example             # Tep mau cau hinh bien moi truong
└── .gitignore               # Loai bo cac tep nhay cam va build artifacts
```

---

## 4. Yeu Cau He Thong

- He dieu hanh: Windows 10/11, Linux hoac macOS
- Docker Desktop (ho tro Docker Compose v2)
- .NET 8.0 SDK tro len
- Node.js phien ban 18 tro len va npm

---

## 5. Thiet Lap Cau Hinh & Bien Moi Truong

Du an ap dung nguyen tac bao mat khong commit thong tin nhay cam (mat khau, token, connection string) len Git. Cac thong so duoc doc tu bien moi truong hoac tep `.env`.

1. Tao tep `.env` tu tep mau:
   ```bash
   cp .env.example .env
   ```

2. Cap nhat cac gia tri can thiet trong tep `.env`:
   - `Channels__Email__SenderEmail`: Dia chi Gmail gui thu.
   - `Channels__Email__Password`: Mat khau ung dung Google (App Password 16 ky tu).
   - `Channels__Telegram__BotToken`: Token Bot Telegram tu BotFather.
   - `VITE_VAPID_PUBLIC_KEY`: Khoa cong khai VAPID cho Web Push.

---

## 6. Huong Dan Khoi Chay

### Cach 1: Khoi chay tu dong (Khuyen nghi tren Windows)

Chay file kich ban duy nhat:
```powershell
.\run_all.ps1
```

Kich ban se tu dong:
- Khoi dong cum Docker container (`kafka-kraft`, `sqlserver-db`, `mongodb-read`).
- Nap bien moi truong tu `.env`.
- Bien dich solution backend.
- Khoi chay dong thoi CommandService (port 5001), QueryService (port 5002), ConsumerService va Frontend Console (port 5173).

### Cach 2: Khoi chay thu cong tung thanh phan

1. Khoi dong ha tang Docker:
   ```bash
   docker compose up -d
   ```

2. Khoi chay Command Service:
   ```bash
   cd backend/CommandService
   dotnet run
   ```

3. Khoi chay Consumer Service:
   ```bash
   cd backend/ConsumerService
   dotnet run
   ```

4. Khoi chay Query Service:
   ```bash
   cd backend/QueryService
   dotnet run
   ```

5. Khoi chay Frontend Console:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

---

## 7. Dia Chi Cac Dich Vu

- Command Service API (Swagger): `http://localhost:5001/swagger`
- Query Service API (Swagger): `http://localhost:5002/swagger`
- SignalR Realtime Hub: `http://localhost:5002/hubs/notifications`
- Frontend Developer Console: `http://localhost:5173`
- Apache Kafka Bootstrap Server: `localhost:9092`
- SQL Server (Write DB): `localhost:1433`
- MongoDB (Read DB): `localhost:27017`

---

## 8. Chinh Sach Bao Mat

- Tep `.env` va cac tep build (`bin/`, `obj/`, `node_modules/`) duoc loai tru hoan toan khoi Git thong qua `.gitignore`.
- Lop `Common.EnvLoader` tu dong tim kiem tep `.env` o thu muc goc va nap vao runtime moi truong cua tien trinh.
- Khi trien khai moi truong production (Kubernetes/Docker Swarm), cac bien moi truong duoc truyen truc tiep thong qua co che ConfigMap hoac Secret.
