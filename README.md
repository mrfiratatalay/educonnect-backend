# EduConnect Backend

Bu backend `MSSQL Server + ASP.NET Core (.NET 10) + EF Core` ile hazirlandi.

## Mimari

- `src/EduConnect.Api`: controller, hub, middleware, startup
- `src/EduConnect.Application`: contract ve interface tanimlari
- `src/EduConnect.Domain`: entity ve enum tanimlari
- `src/EduConnect.Infrastructure`: EF Core, auth, email, servisler, migration

## Calistirma

```powershell
docker compose -f docker-compose.mailpit.yml up -d mailpit
dotnet build backend\EduConnect.sln
dotnet ef database update --project backend\src\EduConnect.Infrastructure --startup-project backend\src\EduConnect.Api
dotnet run --project backend\src\EduConnect.Api
```

## Varsayilan Adresler

- API: `http://localhost:5160`
- Swagger: `http://localhost:5160/swagger`
- Health: `http://localhost:5160/health`
- SignalR Hub: `http://localhost:5160/hubs/chat`
- Frontend CORS origin: `http://localhost:5173`
- Mailpit SMTP: `localhost:1025`
- Mailpit UI: `http://localhost:8025`

## Varsayilan Admin

- Email: `admin@educonnect.local`
- Sifre: `Admin123!`

## Auth Notlari

- Kayit yalnizca secilen universitenin kurumsal e-posta alani ile kabul edilir.
- Kayit sonrasi kullanici dogrudan login olmaz.
- Backend 6 haneli bir dogrulama kodu uretir ve development ortaminda bu kod Mailpit'e duser.
- Kullanici frontend uzerindeki `/verify-email` ekranindan kodu onayladiktan sonra login olabilir.

## Temel Endpoint Gruplari

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

- Connection string varsayilan olarak `localhost\SQLEXPRESS` icin ayarlidir.
- Uygulama acilisinda migration ve seed denenir; veritabani erisilemezse API warning log ile ayakta kalir.
- Mailpit sadece development ortaminda local e-posta dogrulama testi icin kullanilir. Canli ortamda ayni akisin arkasina gercek SMTP/provider baglanir.
