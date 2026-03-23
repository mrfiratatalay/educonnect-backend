# EduConnect Backend

Bu backend, `MSSQL Server + ASP.NET Core (.NET 10) + EF Core` ile hazırlanmıştır.

## Mimarî

- `src/EduConnect.Api`: Controller, SignalR hub, auth, Swagger, startup
- `src/EduConnect.Application`: DTO, sözleşmeler, interface'ler
- `src/EduConnect.Domain`: Entity ve enum tanımları
- `src/EduConnect.Infrastructure`: EF Core, DbContext, JWT, hashing, seed, migration

## Çalıştırma

```powershell
dotnet build backend\EduConnect.sln
dotnet ef database update --project backend\src\EduConnect.Infrastructure --startup-project backend\src\EduConnect.Api
dotnet run --project backend\src\EduConnect.Api
```

## Varsayılan Ayarlar

- API: `http://localhost:5099` veya launch profile'ın verdiği port
- Swagger: `/swagger`
- Health: `/health`
- SignalR Hub: `/hubs/chat`
- CORS izinli frontend origin: `http://localhost:5173`

## Varsayılan Admin Kullanıcısı

Seed sırasında aşağıdaki kullanıcı oluşturulur:

- Email: `admin@educonnect.local`
- Şifre: `Admin123!`

## Temel Endpoint Grupları

- `api/auth`
- `api/users`
- `api/posts`
- `api/groups`
- `api/events`
- `api/products`
- `api/visual-search`
- `api/notifications`
- `api/discounts`
- `api/feedbacks`

## Notlar

- Connection string varsayılan olarak `localhost` üzerindeki MSSQL için ayarlı.
- Uygulama açılışında migration ve seed denenir; veritabanına ulaşılamazsa uygulama log warning vererek ayağa kalkmaya devam eder.
- Görsel arama ve chatbot modülü şu an gerçek model entegrasyonu yerine backend-uyumlu servis katmanı ile hazırlanmıştır; daha sonra gerçek AI servisleri aynı interface'ler üzerinden değiştirilebilir.
