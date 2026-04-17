import json
import random
from pathlib import Path

DATASET_PATH = Path("c:/Users/FIRAT/Desktop/Tubitak-Tez/backend/nlp-service/data/dataset.json")

# 10 Intent sınıfı için 40'ar adet sentetik, bağlama uygun veri
synthetic_data = {
    "exam_and_grading": [
        "Vize sınavları haftaya mı başlıyor?", "Finalde en az kaç almam lazım?", "Bütünleme sınavı için başvuru yapacak mıyım?",
        "Ara sınavların ortalamaya etkisi yüzde kaç?", "Tek ders sınavına kimler girebiliyor?", "Mazeret sınavı için raporu nereye vereceğim?",
        "Harf notum CC gelmiş, geçiyor muyum?", "GANO hesaplaması nasıl yapılıyor?", "Vizelere girmek zorunlu mu?",
        "Finalden kalırsam direkt bütlere mi kalıyorum?", "YANO 1.80 olursa sınamalı öğrenci mi oluyorum?", "Sınav kağıdıma nasıl itiraz edebilirim?",
        "Sınav sonuçları ne zaman açıklanacak?", "DC ile geçtiğim dersi yükseltmeye alabilir miyim?", "Sınav yerimi REBİS'ten mi göreceğim?",
        "Mazeret sınavı dilekçesi kime verilir?", "Bütünleme notu final yerine mi geçer?", "Yaz okulunda sınav sistemi farklı mı?",
        "Yıl sonu başarı notu nasıl hesaplanıyor?", "Sınava girmezsem FF mi alırım?", "Geçme notu barajı 50 mi 60 mı?",
        "Harf notlarına çan eğrisi uygulanıyor mu?", "Sınavda optik form nasıl doldurulur?", "Ara sınavlara mazeretsiz girmemenin cezası var mı?",
        "Sınav programı ne zaman ilan edilecek?", "Ortalamam 2'nin altındaysa ne olur?", "Tek ders sınavı ne zaman yapılıyor?",
        "Vize mazeretine itiraz hakkım var mı?", "Final sınavında sorumluluk alanım neresi?", "Bütlere kalmak için bir şart var mı?",
        "Sınav sonuçlarına itiraz dilekçesini OİDB'ye mi veriyoruz?", "Grup projeleri vize yerine mi geçiyor?", "Sınavda kopya çekmenin cezası nedir?",
        "Ders içi performans notu ortalamayı nasıl etkiler?", "Laboratuvar sınavı ne zaman yapılacak?", "Online sınavlarda kamera zorunlu mu?",
        "Sınav esnasında kimlik kontrolü yapılıyor mu?", "Final sonuçlarına göre harf notu değişir mi?", "Bütünleme için harç ödeyecek miyim?",
        "Sınava geç kalırsam ne yapmalıyım?"
    ],
    "course_registration": [
        "Ders kaydımı nasıl onaylatacağım?", "Seçmeli dersleri ne zaman seçeceğiz?", "Danışman onayı olmadan ders kaydı biter mi?",
        "Alttan kalan dersimi bu dönem alabilir miyim?", "Çakışan dersleri seçebilir miyim?", "Ders ekleme çıkarma haftası ne zaman başlıyor?",
        "Muafiyet sınavına başvurmak istiyorum.", "İntibak işlemlerim ne kadar sürer?", "Üstten ders alma şartları neler?",
        "Yandal yapmak için GANO kaç olmalı?", "Çift anadal başvurusu hangi tarihte?", "Öğrenim katkı payı harcını nereye yatıracağız?",
        "Ders kaydım sistemde görünmüyor.", "Dersin kontenjanı dolmuş ne yapmalıyım?", "Bölüm dışı seçmeli ders alabilir miyim?",
        "Yaz okulunda en fazla kaç kredi alınıyor?", "Kayıt dondurmak için nereye başvurmalıyım?", "Dönemlik ders yükü limiti nedir?",
        "Danışmanım ders kaydımı reddetmiş, sebebi nedir?", "Ders silme işlemi nasıl yapılır?", "Ders kayıt sistemine REBİS'ten mi gireceğim?",
        "Yatay geçiş yapanların ders eşleştirmesi nasıl olur?", "Ortalamam yüksekse üstten kaç ders alabilirim?", "Ders saati çakışması durumunda ne yapmalıyım?",
        "Ders programında boş günüm olsun istiyorum.", "Staj dersi kodunu nasıl ekleyeceğim?", "Zorunlu dersi almazsam ne olur?",
        "Açılmayan seçmeli ders yerine ne alabilirim?", "Ders kayıt işlemlerinde sorun yaşıyorum.", "Danışman hocama nasıl ulaşabilirim?",
        "Ders kaydını geç tamamlarsam ceza var mı?", "Harç ücretini yatırmadan ders seçebilir miyim?", "Ders kayıt dönemi uzatılacak mı?",
        "Kayıt yenileme yapmayı unuttum.", "Farklı fakülteden ders alabilir miyim?", "Dersin önkoşulu var mı?",
        "Önkoşullu dersi geçemezsem ne olur?", "Ders listesi REBİS'te ne zaman görünür?", "Kredim yetmediği için ders alamıyorum.",
        "Bitirme projesi dersi nasıl seçilir?"
    ],
    "campus_life": [
        "Öğrenci kulüplerine nasıl üye olabilirim?", "Kampüste bugün ne etkinlik var?", "Bahar şenlikleri iptal mi oldu?",
        "Konferans salonunda etkinlik düzenlemek için izni kim veriyor?", "Tiyatro kulübü nerede toplanıyor?", "Kampüs içinde spor salonu var mı?",
        "Öğrenci toplulukları standı ne zaman açılacak?", "Konser bileti nereden alınır?", "Söyleşiye katılım sertifikası verilecek mi?",
        "Kampüste kapalı spor salonunu öğrenciler kullanabilir mi?", "Yüzme havuzu ücretleri ne kadar?", "Müzik kulübünün etkinlik takvimi nedir?",
        "Üniversitenin bahar şenliği takvimi açıklandı mı?", "Satranç turnuvasına nasıl kayıt yaptırırım?", "Sinema gösterimleri ne zaman yapılıyor?",
        "Fotoğrafçılık kulübüne katılmak istiyorum.", "Kampüste öğrenciler için dinlenme alanları var mı?", "Sosyal tesisler hafta sonu açık mı?",
        "Diksiyon semineri ne zaman başlıyor?", "Halk oyunları topluluğuna nasıl girerim?", "Kampüs içi bisiklet yolu yapılıyor mu?",
        "Öğrenci senatosu seçimleri ne zaman?", "Birim temsilcileri nasıl belirleniyor?", "Robotik kulübü atölyesini nerede bulabilirim?",
        "Kampüs içinde etkinlik afişi asmak yasak mı?", "Kariyer günleri fuarı ne zaman olacak?", "Mezunlar derneğine üye olabilir miyim?",
        "Okulun futbol takımına seçmeler ne zaman?", "Buz pateni pisti ne zaman açılacak?", "Tenis kortu saatleri nelerdir?",
        "Gönüllülük kulübü faaliyetlerine nasıl katılırım?", "Kariyer merkezi nerede?", "Kampüste kırtasiye var mı?",
        "Çimlerde oturmak yasak mı?", "Girişimcilik topluluğu toplantısı nerede?", "Havacılık kulübünün etkinlikleri neler?",
        "Kampüste ücretsiz internet var mı?", "Öğrenci kafesi fiyatları ne kadar?", "Kampüs radyosu nasıl dinleniyor?",
        "Yazılım geliştirme kulübüne katılmak için şart var mı?"
    ],
    "library": [
        "Kütüphane çalışma saatleri nelerdir?", "Kütüphaneden aynı anda kaç kitap ödünç alabilirim?", "VETİS üzerinden evden kütüphaneye nasıl bağlanırım?",
        "Kitabın süresini internetten uzatabilir miyim?", "Kütüphanede grup çalışma odası nasıl rezerve edilir?", "Geciken kitaplar için ne kadar para cezası ödüyorum?",
        "Veritabanlarına kampüs dışından proxy ile erişim nasıl yapılır?", "Kütüphanede e-dergilere nasıl ulaşabilirim?", "Süreli yayınlar ödünç veriliyor mu?",
        "Kitap sorgulama sistemine nereden girerim?", "RTEÜ kütüphanesine dışarıdan biri üye olabilir mi?", "Barkodu okumayan kitap için ne yapmalıyım?",
        "Kütüphaneye üye olmak zorunlu mu?", "Kitap ayırtma işlemi yapabilir miyim?", "Kütüphanede tarayıcı/fotokopi kullanmak ücretli mi?",
        "Kaybettiğim kitap için ne yapmam gerekiyor?", "Kütüphane cezası REBİS sistemine yansır mı?", "Yaz aylarında kütüphane açık olacak mı?",
        "Tez arşivi fiziki kütüphanede nerede?", "Sessiz çalışma salonlarında priz var mı?", "Kütüphaneye yiyecekle girebilir miyim?",
        "Açık erişim kaynakları listesini nereden görebilirim?", "Scopus veritabanına erişemiyorum.", "Ithinticate intihal tarama programı şifresini nasıl alırım?",
        "Referans yönetim araçları için eğitim var mı?", "Kütüphaneciye sor hizmeti çalışıyor mu?", "Sanal kütüphane turu var mı?",
        "Kitap bağışlamak istiyorum, kime vermeliyim?", "Başka bir ildeki üniversitenin kitabını isteyebilir miyim?", "E-kitap indirmek serbest mi?",
        "Ödünç aldığım kitabı postayla iade edebilir miyim?", "Kütüphane hesabımın şifresi ne?", "Kütüphane borcum varsa mezun olabilir miyim?",
        "Nadir eserler bölümüne giriş iznini nasıl alırım?", "Kütüphanenin çalışma saatleri vize haftası değişir mi?", "Kütüphanede internet için eduroam mu lazım?",
        "Kütüphanede dolap kiralayabiliyor muyuz?", "Akademik makale indirme hakkımız sınırlı mı?", "Merkez kütüphane binası nerede?",
        "Sesli kitap arşivi var mı?"
    ],
    "scholarship_support": [
        "Yemek bursu başvuruları ne zaman bitiyor?", "Başarı bursu için ortalamam kaç olmalı?", "KYK burs sonuçları açıklandı mı?",
        "Üniversitenin verdiği karşılıksız burs var mı?", "Maddi destek talep formunu SKS'ye mi vereceğim?", "Yarı zamanlı çalışma programı başvurusu nasıl yapılır?",
        "Burs devam şartları nelerdir?", "Disiplin cezası alırsam bursum kesilir mi?", "Özel vakıf burslarının listesi var mı?",
        "Yemek bursu günde kaç öğün geçerli?", "KYK yurdu ücretini ödeyemezsem ne olur?", "Engelli öğrenci bursu başvurusu nereye yapılır?",
        "Gönüllü çalışma programı ücret veriyor mu?", "Ailesinde şehit olanlar için özel burs var mı?", "Alttan dersim varsa başarı bursu alabilir miyim?",
        "Erasmus hibesi burs sayılır mı?", "Hem KYK hem de okul yemek bursu alabilir miyim?", "TÜBİTAK burs onayı okula ulaştı mı?",
        "Burs ücretleri hangi bankaya yatıyor?", "Öğrenci yardım fonu nasıl çalışıyor?", "Yemek bursuna online başvuru yapılıyor mu?",
        "Kısmi zamanlı öğrenci çalıştırma ilanı nerede yayınlanır?", "Şehit ve gazi yakınları için ücretsiz yemek var mı?", "Yemek bursu çıkmadı, itiraz edebilir miyim?",
        "Yeni öğretim yılında burslar artacak mı?", "Ekonomik durum beyanı formu nerede?", "Akraba veya referans zorunlu mu?",
        "Burs kesintisi ne zaman gerçekleşir?", "Devamsızlıktan kalan öğrencinin bursu kesilir mi?", "Milli sporcu bursu nasıl alınır?",
        "Yazın burs yatmaya devam edecek mi?", "Evli öğrencilere özel yardım var mı?", "Yol/ulaşım yardımı yapılıyor mu?",
        "Kırtasiye ve kitap yardımı nereden istenir?", "TEV bursu üniversitemizde geçerli mi?", "İhtiyaç bursu başvurusu belge ister mi?",
        "Doğrudan dekana burs için çıkabilir miyim?", "SKS burs sonuçları nereden öğrenilir?", "İlk yüze giren öğrenciler için başarı ödülü var mı?",
        "RTEÜ Geliştirme Vakfı bursu nedir?"
    ],
    "campus_logistics": [
        "Kampüse giden en son otobüs kaçta kalkıyor?", "Ring araçları hafta sonu çalışıyor mu?", "KYK kız yurdu kampüsün içinde mi?",
        "Öğrenci yurdundan fakülteye nasıl gidebilirim?", "Kampüste misafirhane ücreti ne kadar?", "Merkez yerleşkede ziraat bankası ATM'si var mı?",
        "Kampüs içi ring hatları güzergahı nedir?", "Fakülte binasına giden yolda ulaşım sorunu var.", "Yeni yapılan yurt binası ne zaman açılacak?",
        "Otopark kullanımı öğrenciler için ücretsiz mi?", "Aracım için kampüs giriş kartını nasıl çıkartırım?", "Yurt çıkış saatleri kaça kadar?",
        "Otobüs kartı dolum noktası kampüste var mı?", "Fakülteler arası ulaşım var mı?", "Mimarlık fakültesi hangi yerleşkede bulunuyor?",
        "Tıp fakültesi hastanesi kampüs içinden mi gidiliyor?", "Kampüste kargo şubesi var mı?", "Kyk yurdu itiraz dilekçemi nereye vereceğim?",
        "Misafir öğrenci yurdunda kaç gün kalınabilir?", "Yurt idaresinin telefonu var mı?", "Ring servis saatleri kış tarifesine geçti mi?",
        "Havaalanından kampüse Havaş var mı?", "Güvenlik amirliği nerede?", "Kampüste bisiklet kiralama noktası var mı?",
        "Öğrenci yurdu yemek saatleri uyumsuz.", "Elektrik kesintisinde yurt jeneratörü devredemi?", "Yurt kaydımı nasıl sildirebilirim?",
        "Kampüste eczane var mı?", "Nöbetçi yurt başvurusu nasıl yapılır?", "Ulaşım zammı ring servislerini etkiliyor mu?",
        "Şehir merkezine giden en hızlı yol hangisi?", "Öğrenci otopark kartı aylık ücreti nedir?", "Misafirlerimi kampa aracıyla alabilir miyim?",
        "Kampüs krokisi haritasını nereden bulurum?", "Sağlık ocağı kampüs içinde mevcut mu?", "Metro kampüse gelecek mi?",
        "Yurtlarda internet çekmiyor.", "Giriş kapılarındaki turnike sistemi değişti mi?", "Yurda gece en geç kaçta girilebiliyor?",
        "Öğrenciler için özel ring seferi var mı?"
    ],
    "cafeteria": [
        "Yemekhanenin bugünkü menüsünde ne var?", "Yemekhane hafta sonu çalışıyor mu?", "Yemek kartıma internetten bakiye yükleyebilir miyim?",
        "Akşam yemeği saatleri kaçla kaç arası?", "Yemek rezervasyonunu bir gün önceden mi yapmalıyız?", "Yemediğim günün parası kartıma geri döner mi?",
        "Kartımda bakiye biterse nakit geçiyor mu?", "Vegan / vejetaryen menü çıkıyor mu?", "Kantin fiyatları yemekhaneye göre nasıl?",
        "Mobil uygulamadan yemek rezervasyonu yapamıyorum.", "Yemek iptalini aynı gün sabah yapabilir miyim?", "Kartsız telefonla yemekhaneye girebilir miyim?",
        "Yemek biletini kampüste nereden alırım?", "Yemek bursluyum, kart basmama gerek var mı?", "Misafirimi yemekhaneye getirebilir miyim, misafir ücreti ne kadar?",
        "Öğle yemeği kaçta bitiyor?", "Diyet menüsü için SKS'ye dilekçe mi vereceğim?", "Yemekhanede ikinci tabak yemek ücretlimi?",
        "Yemek kalitesiyle ilgili şikayetleri nereye yazabilirim?", "Merkezdeki yemekhane nerede kalıyor?", "Kart dolum kioskları arızalı.",
        "Kredi kartıyla direkt turnikeden geçiş var mı?", "QR kod ile yemekhane turnikesini açma özelliği eklenecek mi?", "Yemekhane görevlilerine rezervasyon saatini sormak.",
        "Ramazan ayında iftar çadırı var mı?", "Oruç tutanlar için sahur yemeği veriliyor mu?", "Kantinlerde bakiye sistemi geçerli mi?",
        "Yemek porsiyonları çok az, nasıl itiraz ederiz?", "Personel ve öğrenci menüsü aynı mı?", "Yemek numuneleri kontrol ediliyor mu?",
        "Tıp fakültesindeki kantin kaça kadar açık?", "Yemek rezervasyon sistemi kapandı, aç dururum.", "Kartımı kaybettim yemek hesabım gider mi?",
        "Bakiye aktarımı için öğrenci işlerine mi gideceğim?", "Yemekhane temizlik saatleri ne zaman?", "Öğrenci menüsü ücretine zam geldi mi?",
        "Yemekhanede su parayla mı veriliyor?", "Paket yemek servisi (al-git) var mı?", "Kahvaltı çıkıyor mu yemekhanede?",
        "Glutensiz diyet yemeği çıkartabiliyor musunuz?"
    ],
    "marketplace": [
        "Kullanmadığım sınav kitaplarımı burada satabilir miyim?", "İkinci el hesap makinesi almak istiyorum, ilanı nerede bulurum?", "Buzdolabı devredecek mezun var mı?",
        "Marketplace üzerinden eşya bulmak kolay mı?", "Resmini çektiğim kitaba benzer ilanları göster.", "Satılık ilanı nasıl açabilirim?",
        "Evi devredebileceğimiz ilan köşesi burası mı?", "İkinci el eşyamın fotoğrafını yüklesem fiyat çıkarır mı?", "Dolap yerine kampüs içi pazar yeri lazım.",
        "Sıfır kullanılmamış önlük satmak istiyorum.", "Ürün arama motoruna görseli nasıl yüklerim?", "İlanlara yorum yapabiliyor muyuz?",
        "Sadece RTEU öğrencilerine mi satış yapabilirim?", "Ev arkadaşı arayanların ilanları nerede?", "Platform üzerinden alışverişte güvenli ödeme var mı?",
        "Dizüstü bilgisayar satiyorum, kategori ne olmali?", "Ürün satışında komisyon kesiliyor mu?", "İlanım ne kadar süre yayında kalır?",
        "Uygun fiyatlı ikinci el mobilya arıyorum.", "Mezuniyet cübbesi satan var mı?", "Marketplace görsel doğrulama sistemi nasıl çalışıyor?",
        "Resimdeki çanta kampüste kimde var?", "Sattığım ürünün fiyatını ilan sonrası değiştirebilir miyim?", "Yanlış ürün görseli yükledim, silemiyorum.",
        "Araç satan var mı?", "Bisikletini uygun fiyata verecek var mı?", "Kitap fiyatları çok pahalı, ikinci eli var mıdır?",
        "Ders notları satışı platformda yasak mı?", "Fotoğrafını yüklediğim tişört platformda satılık mı?", "Gitarını satanlarla nasıl iletişime geçerim?",
        "İlanı direkt whatsapp numarama yönlendirebiliyor muyum?", "Görsel benzerliği araması % kaç isabetli?", "Sahte ilanları şikayet et butonu var mı?",
        "Platformda takas seçeneği aktif mi?", "Eşyalarımı ücretsiz vermek istiyorum, ilan türü ne seçeyim?", "Kampüs içi elden teslimat garantili ilan.",
        "Tez yazımı ilanı açmak yasak mı?", "Görsel arama motoru PNG formatını kabul ediyor mu?", "Kategoriler arasında elektronik eşyalar yok.",
        "Satıcı yorumlarını görebiliyor muyuz?"
    ],
    "digital_systems": [
        "REBİS giriş şifremi değiştirmek istiyorum.", "OBS'ye e-Devlet ile giriş yapamıyorum.", "E-kampüs mobil uygulaması hata veriyor.",
        "Üniversitenin verdiği e-posta adresimin uzantısı nedir?", "Kurumsal mail şifremi unuttum, SMS onayı çalışmıyor.", "Kampüsteki Eduroam ağına nasıl bağlanacağım?",
        "Cihazımı okulun WiFi ağına kaydetmek için MAC adresimi nereye gireceğim?", "Öğrenciler için verilen lisanslı Office 365'i nasıl kurarım?", "VPN ile evden bağlandığımda kütüphane açılmıyor.",
        "Bilgi İşlem Daire Başkanlığı nerede?", "E-posta kutum dolu diyor, kapasiteyi nasıl arttırırım?", "Microsoft Teams öğrenci hesabı aktivasyonu",
        "REBİS'te güvenlik kodunu yanlış doğruluyor.", "Transkript onay sistemi hata veriyor.", "Akademik personel değerlendirme anketi sisteme girmiyor.",
        "Telefon numarası güncellemesi OİDB'den mi BİDB'den mi yapılıyor?", "FortiClient VPN ayarları nasıl yapılır?", "WiFi şifrem REBİS şifremle aynı mı?",
        "Öğrenci numarası sorgulama sistemi çökmüş.", "Aynı anda kaç cihazla Eduroam'a bağlanabiliriz?", "Zoom'da kurumsal lisansımız var mı?",
        "Bilgi sisteminde e-imza ayarları nerede?", "E-kampüse girerken sürekli 'oturumunuz düştü' uyarısı alıyorum.", "Linux cihazıma Eduroam ayarını yapamıyorum.",
        "Sürekli 'Hesabınız kilitlendi' hatası alıyorum.", "İki faktörlü doğrulama zorunlu mu?", "Mail yollarken dosya boyutu limiti ne kadar?",
        "Antivirüs programı öğrenciye ücretsiz veriliyor mu?", "Uzaktan eğitim İLİTAM sistemine nereden gireceğim?", "Google Workspace/Drive sınırsız depolama devam ediyor mu?",
        "Öğrenci mailim başkasına spam yolluyor, hesabım hacklendi mi?", "Şifrem içinde özel karakter olduğu için OBS'ye giremiyorum.", "VPN bağlantısı çok yavaş çalışıyor.",
        "MacOS bilgisayarımda VPN kuramıyorum.", "OBS sayfasında dönem seçimi açılmıyor.", "Not giriş şifresi ve öğrenci giriş şifresi ayrımı var mı?",
        "Kurumsal e-posta hesabı ömür boyu mu veriliyor?", "Mezun olduktan sonra REBİS'e girebilir miyim?", "WiFi ağı sürekli kopuyor.",
        "Bilgi işleme destek bileti (ticket) nasıl açarım?"
    ],
    "student_services": [
        "Öğrenci belgesini İngilizce alabiliyor muyum?", "Transkripti e-devlet yerine OİDB'den mi almalıyım?", "Aslı gibidir onayı için fotokopi çektirmem gerekir mi?",
        "Geçici mezuniyet belgesi almadan diploma alabilir miyim?", "Kayıt sildirme dilekçesi örneğini nereden bulacağım?", "İlişik kesme işlemi sistem üzerinden online yapılır mı?",
        "Mezun olduktan sonra diploma eki alacak mıyım?", "Diplomamı kargo ile gönderebiliyor musunuz?", "Başka bir harç borcum çıkmış, diplomamı engeller mi?",
        "Psikolojik danışmanlık merkezinden randevu almak istiyorum.", "Engelli Öğrenci Birimi (ODK) kampüsün neresinde?", "Farabi/Erasmus öğrencisi öğrenci belgesini nasıl alır?",
        "Staj dosyası teslimi de OİDB'ye mi yapılıyor?", "Harçsız pasaport yazısını nereden alacağım?", "Askeralma belgesi öğrenci belgesi midir?",
        "Danışmanım ıslak imza atmadığı için belgeyi alamıyorum.", "Disiplin cezası belgede görünür mü?", "Diplomamı arkadaşım vekaletname ile alabilir mi?",
        "Kartım kırıldı, yeni öğrenci kartını nereden çıkaracağım?", "Mezuniyet GANO'su değişti belgemde.", "Hatalı harf notu düzeltildi, yeni transkript nasıl çıkartırım?",
        "Lise diplomamı geri almak istiyorum, kayıt silicem.", "Psikolojik destek süreci gizli mi kalır?", "ODK sınav okuyucu desteği veriyor mu?",
        "Erkek öğrenciler için askerlik tecil belgesi.", "Şehir dışındayım öğrenci belgemi kuruma OİDB fakslar mı?", "Uluslararası öğrenci ofisi hangi binada?",
        "Yatay geçiş belgesi onayı için Rektörlüğe mi gitmeliyim?", "E-imzalı belgeyi ıslak imzalıya çevirme.", "Kayıt dondurma işlemi OİDB'nin sitesinden mi yapılıyor?",
        "Öğrenci işleri daire başkanlığı ofisi saat kaçta açılıyor?", "OİDB'ye ulaşmak için telefon numaranız var mı?", "Bütün belgeler ücretsiz mi?",
        "Stajyer öğrenci sigorta belgesini OİDB mi SGK mı veriyor?", "Askerlik şubesine belgeyi okul kendisi mi iletiyor?", "OİDB dilekçe hakkı süresi nedir?",
        "Yaz dönemi kayıt sildirenler için süreç ne kadar sürer?", "Mezuniyet töreni için cübbeler nereden alınıyor?", "Bana özel mühürlü kapalı transkript lazım.",
        "Öğrenci işlerindeki personeli şikayet edebilir miyim?"
    ]
}

# Mevcut veri setini oku
try:
    with open(DATASET_PATH, 'r', encoding='utf-8') as f:
        existing_data = json.load(f)
except Exception:
    existing_data = []

# Yeni verileri ekle
new_entries_count = 0
for intent, questions in synthetic_data.items():
    for q in questions:
        existing_data.append({
            "text": q,
            "intent": intent
        })
        new_entries_count += 1

# Karıştır (Shuffle)
random.shuffle(existing_data)

# JSON formatinda geri kaydet
with open(DATASET_PATH, 'w', encoding='utf-8') as f:
    json.dump(existing_data, f, ensure_ascii=False, indent=4)

print(f"Bassarili! Yeni eklenen ornek: {new_entries_count}")
print(f"Toplam Data Boyutu: {len(existing_data)} soru.")
