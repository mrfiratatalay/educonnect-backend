# EduConnect Friend PC Setup Guide

Bu dokuman, temiz bir Windows PC'de EduConnect sistemini GitHub'dan clone edip calistirmak icin hazirlandi. Bir AI modeline bu dosyayi veriyorsan, adimlari sirasiyla uygulat.

## Hedef

Tek PC'de su servisler calismali:

- Backend API: `http://localhost:5160`
- Frontend: `http://localhost:5173`
- EduAI NLP/Vision/ResNet servisi: `http://localhost:8000`
- SQL Server veritabani: `EduConnectDb`

## Gerekli Kurulumlar

Windows PC'de sunlar kurulu olmali:

- Git
- Git LFS
- .NET SDK 10
- Node.js LTS ve npm
- Python 3.11 veya 3.12
- SQL Server Express
- SQL Server Management Studio veya `sqlcmd`

Git LFS yoksa scriptler `winget` ile kurmayi dener. Yine de manuel kurulum gerekirse:

```bat
git lfs install
```

## Repo Klasor Yapisi

Iki repo ayni ust klasor icinde olmalidir. Ornek:

```text
C:\Users\<USER>\Desktop\EduConnect\
  TEZ-Backend\
  TEZ-Frontend\
```

Clone komutlari:

```bat
cd C:\Users\<USER>\Desktop
mkdir EduConnect
cd EduConnect

git clone https://github.com/mrfiratatalay/TEZ-Backend.git
git clone https://github.com/mrfiratatalay/TEZ-Frontend.git
```

Backend repo private ise GitHub hesabi yetkili olmalidir.

## Buyuk Dosyalari Indir

Backend klasorunde:

```bat
cd TEZ-Backend
git lfs install
git lfs pull
```

Kontrol edilmesi gereken dosyalar:

```text
TEZ-Backend\nlp-service\models\intent_classifier\model.safetensors
TEZ-Backend\database\EduConnectDb.bak
```

`model.safetensors` kucuk bir text dosyasi gibi gorunuyorsa Git LFS pointer kalmistir. Bu durumda:

```bat
git lfs pull
```

## Veritabanini Restore Et

Backup dosyasi:

```text
TEZ-Backend\database\EduConnectDb.bak
```

SQL Server instance genelde:

```text
localhost\SQLEXPRESS
```

Restore icin SSMS kullanilabilir:

1. SQL Server Management Studio ac.
2. `localhost\SQLEXPRESS` ile baglan.
3. `Databases` uzerine sag tikla.
4. `Restore Database...`
5. `Device` sec.
6. `EduConnectDb.bak` dosyasini sec.
7. Database name: `EduConnectDb`
8. `Options` sekmesinde gerekirse `Overwrite the existing database (WITH REPLACE)` sec.
9. `OK`.

`sqlcmd` ile restore yapmak gerekirse:

```bat
sqlcmd -S localhost\SQLEXPRESS -E -Q "RESTORE DATABASE [EduConnectDb] FROM DISK = N'C:\Users\<USER>\Desktop\EduConnect\TEZ-Backend\database\EduConnectDb.bak' WITH REPLACE;"
```

Eger `database is in use` hatasi olursa:

```bat
sqlcmd -S localhost\SQLEXPRESS -E -Q "ALTER DATABASE [EduConnectDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [EduConnectDb] FROM DISK = N'C:\Users\<USER>\Desktop\EduConnect\TEZ-Backend\database\EduConnectDb.bak' WITH REPLACE; ALTER DATABASE [EduConnectDb] SET MULTI_USER;"
```

## Connection String Kontrolu

Backend ayari:

```text
TEZ-Backend\src\EduConnect.Api\appsettings.json
```

Varsayilan:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=EduConnectDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

Eger SQL Server instance farkliysa duzelt:

```json
"DefaultConnection": "Server=localhost;Database=EduConnectDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

veya:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=EduConnectDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

## Sistemi Tek Komutla Baslat

Backend repo icinde:

```bat
cd C:\Users\<USER>\Desktop\EduConnect\TEZ-Backend
scripts\dev\start-all-dev.bat
```

Bu script uc pencere acar:

- EduConnect Backend
- EduConnect Frontend
- EduAI NLP/Vision Service

Frontend klasoru `TEZ-Frontend` veya `frontend` adi ile backend repo ile ayni ust klasorde aranir.

## Saglik Kontrolleri

Backend:

```bat
curl http://localhost:5160/health
```

Beklenen:

```json
{"status":"ok"}
```

NLP/Vision:

```bat
curl http://localhost:8000/health
curl http://localhost:8000/api/nlp/health
curl http://localhost:8000/api/vision/health
```

Beklenen NLP:

```json
"model_loaded": true,
"model_name": "berturk-finetuned"
```

Beklenen Vision:

```json
"model_loaded": true,
"model_name": "resnet50-pretrained"
```

Frontend:

```text
http://localhost:5173
```

## EduAI Notu

EduAI iki katmanlidir:

- BERTurk + bilgi tabani lokal calisir.
- Gemini metin uretimi API key ve kota ile calisir.

Gemini kotasi dolarsa sistem artik tamamen cokmez. Backend lokal KB cevabina duser. Bu durumda cevap `berturk-finetuned+local-kb` modeliyle gelebilir.

Gemini kota hatasi logda boyle gorunur:

```text
Quota exceeded
model: gemini-2.5-flash
```

Bu kod hatasi degildir; API kotasi/plan sorunudur.

## Gorsel Arama Notu

Gorsel arama icin NLP/Vision servisi acik olmalidir:

```text
http://localhost:8000/api/vision/health
```

ResNet ayri bir servis degildir. `start-nlp-dev.bat` icindeki FastAPI servisi hem NLP hem Vision/ResNet endpointlerini acar.

Ilk gorsel aramada urun embeddingleri eksikse backend urun gorsellerinden embedding uretip veritabanina kaydeder. Bu yuzden ilk arama biraz daha yavas olabilir.

## Sik Hatalar ve Cozumler

### Backend DLL locked hatasi

Hata:

```text
The file is locked by: EduConnect.Api
```

Cozum:

```bat
TEZ-Backend\scripts\dev\start-backend-dev.bat
```

Bu script eski backend/watch sureclerini kapatip yeniden baslatir.

### NLP modeli yok

Hata:

```text
NLP modeli bulunamadi
```

Cozum:

```bat
cd TEZ-Backend
git lfs install
git lfs pull
```

Ardindan:

```bat
scripts\dev\start-nlp-dev.bat
```

### Frontend klasoru bulunamadi

Backend ve frontend ayni ust klasorde olmali:

```text
EduConnect\
  TEZ-Backend\
  TEZ-Frontend\
```

### SQL Server baglanamiyor

Kontrol et:

- SQL Server Express kurulu mu?
- Instance adi `SQLEXPRESS` mi?
- `EduConnectDb` restore edildi mi?
- `appsettings.json` connection string dogru mu?

### Port kullanimda

Kullanilan portlar:

- `5160`: backend
- `5173`: frontend
- `8000`: NLP/Vision

Gerekirse ilgili terminal pencerelerini kapat ve `start-all-dev.bat` tekrar calistir.

## Test Kullanici Notu

Database backup mevcut kullanicilarla gelir. Giris icin mevcut test kullanicilarindan biri kullanilabilir. Sifre bilinmiyorsa uygulama uzerinden yeni kullanici kaydi yapilabilir veya DB'de test kullanicisi sifresi resetlenebilir.

## Basarili Kurulum Kriteri

Kurulum basarili sayilmasi icin:

1. `http://localhost:5160/health` OK donmeli.
2. `http://localhost:8000/api/nlp/health` icinde `model_loaded: true` olmali.
3. `http://localhost:8000/api/vision/health` icinde `model_loaded: true` olmali.
4. `http://localhost:5173` acilmali.
5. EduAI mesaj attiginda `Error` yerine cevap donmeli.
6. Pazar > Gorsel Ara yuklenen urune benzer ilanlari getirmeli.
