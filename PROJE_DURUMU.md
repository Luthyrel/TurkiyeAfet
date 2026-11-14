# Türkiye Afet — Proje Durumu

Son güncelleme: 2026-11-13

## Güncel Özet

Türkçe, 3D afet farkındalığı oyunu. 81 il haritasından il seçilir; seçilen ilin eğitim senaryosu açılır. Deprem ve sel/taşkın akışları birbirinden bağımsız çalışır. Oyun içeriğinin büyük bölümü kodla oluşturulur.

Kullanıcı son oyun oturumunda sel suyunun en yüksek seviyesine kadar içeride kaldığında boğulma/başarısızlık akışının tetiklendiğini doğruladı. Sonrasında görev metniyle puanın HUD içinde üst üste gelmesi düzeltildi. Kullanıcının son bildirimi: şu an oyunda sorun veya doğrulanmamış bir iş kalemi yok.

## Teknik Bilgiler

- Unity Editor: 6000.6.7f1
- Render pipeline: Universal Render Pipeline
- Giriş sistemi: Unity Input System
- Başlangıç sahnesi: Assets/Scenes/DepremPrototip.unity
- Ev ve senaryo nesneleri çalışma sırasında kodla oluşturulur.

## Afet Senaryoları

### Deprem

- Deprem öncesi hazırlık görevleri: rafı sabitleme, ağır eşyayı aşağı alma, çıkış yolunu açma ve isteğe bağlı afet çantası hazırlama.
- Hazırlık tamamlanınca 5 saniyelik geri sayım biter ve deprem otomatik başlar.
- Sarsıntı 10 saniye sürer. Güvenli bölgede çömelip 5 saniye tutunma 100 puan kazandırır.
- Deprem sonunda %50 olasılıkla enkaz aşaması başlar; oluşursa kısa karartmadan sonra enkaz altı davranış görevleri açılır.
- Enkaz oluşmayan akışta deprem sonrası görevler, kapıyı açıp toplanma alanına ilerlemeyi içerir.
- Afet çantası isteğe bağlı bonus puan verir; çanta olmadan temel oyun akışı 700 puan hedefini korur.

### Sel ve taşkın

- Sel senaryosu depremden bağımsız, kodla oluşturulmuş iki katlı evde oynanır.
- Sel öncesinde ana şalteri kapatma, evrak ve ilaçları, modemi üst kata taşıma görevleri bulunur.
- Hazırlık bitince ekranda zaten bulunan “Hazırlık tamamlandı…” bildiriminin içindeki sayı 5’ten 1’e iner; ayrı bir geri sayım metni kullanılmaz.
- Su 18 saniyede yükselir, 8 saniye yüksek seviyede kalır ve 8 saniyede çekilir.
- Sel suyuna girince hareket yavaşlar ve ilk temas için bir defa 25 puan kesilir.
- Su en yüksek seviyedeyken karakter hâlâ suyun içindeyse boğulma geri sayımı başlar. 5 saniyede güvenli alana çıkamazsa başarısızlık ekranı açılır; il tamamlanmış olarak kaydedilmez.
- Üst kattaki güvenli alanda kesintisiz 2 saniye kalınca tahliye görevi tamamlanır ve 100 puan verilir.
- Sel sonrası görevlerde 112 ile konum bildirimi ve resmî duyuruyu değerlendirme bulunur.

## Harita, Arayüz ve Ses

- 81 il, gerçek il sınırlarıyla seçilebilir. Tamamlanan afet illeri gri gösterilir ve yeniden seçilebilir.
- İl seçimi kartında imlecin üzerindeki ilin eğitim senaryosu ve afet türü gösterilir.
- Sol üst görev paneli uzun görev metinlerine göre büyür; puan satırı görevlerle çakışmaz.
- Sel geri sayımı mevcut hazırlık bildiriminin içinde gösterilir. Etkileşim olmayan boş noktaya basınca yanıltıcı geçici sel görevi mesajı gösterilmez.
- Oyun içi sesler yerel Mixkit ses dosyaları ve üretilen telefon sesi üzerinden oynatılır; arka plan müziği kullanılmaz.

## Başlıca Kaynak Dosyalar

- Assets/Scripts/GameManager.cs — Oyun durumu ve afet türü.
- Assets/Scripts/ProvinceManager.cs — İl verileri, afet türü ve tamamlanma kayıtları.
- Assets/Scripts/ProvinceMapView.cs — Türkiye il haritası etkileşimi.
- Assets/Scripts/PrototypeBootstrap.cs — Oyun sahnesi, ev, harita senaryosu ve etkileşimli nesneler.
- Assets/Scripts/PrototypeScenario.cs — Deprem/sel akışları, görevler, puan, su seviyesi ve boğulma sonucu.
- Assets/Scripts/PrototypePlayerController.cs — Oyuncu hareketi, kamera, etkileşim ve sel suyunda hareket etkisi.
- Assets/Scripts/PrototypeUI.cs — Menü, il kartı, görev HUD'ı, geri sayım ve sonuç ekranları.
- Assets/Scripts/PrototypeAudio.cs — Hareket ve etkileşim sesleri.
- Assets/Resources/ProvinceMap/ — İl verisi, harita görseli ve seçim maskesi.
- Assets/Resources/Audio/Mixkit/ — Oyunda kullanılan yerel ses dosyaları.
- Assets/Scenes/DepremPrototip.unity — Başlangıç sahnesi.

## Doğrulama ve Mevcut Durum

- Proje Unity 6000.6.7f1 kullanıyor.
- Kullanıcı sel suyunda kalma, boğulma sonucu ve güncel oyun durumunu Play Mode'da kontrol etti; boğulma akışının çalıştığını ve şu anda açık sorun/doğrulanmamış iş bulunmadığını bildirdi.
- Son HUD düzenlemesi görev metninin uzunluğuna göre panel yüksekliğini ayarlıyor.
- Son kaynak değişikliklerinde git diff --check temiz çıktı.
- Şu an için açık hata veya bekleyen doğrulama işi bulunmuyor.

## Bilinen Tasarım Sınırları

- İl afet türleri eğitim senaryosu seçimi içindir; resmî tehlike veya risk tahmini olarak sunulmaz.
- Ev, merdiven ve birçok eşya oyun prototipi için kodla oluşturulmuş stilize modellerdir.
- İl sınırı verisi geoBoundaries Turkey ADM1 2021 kaynağıdır; resmî kadastral ölçüm olarak sunulmaz.

## Kaynak ve Atıflar

- AFAD, Deprem öncesi, anı ve sonrası alınabilecek önlemler: https://www.afad.gov.tr/deprem-oncesi-ani-ve-sonrasi-alabileceginiz-onlemleri-biliyor-musunuz
- AFAD Çanakkale İl Müdürlüğü, Sel: sel öncesi hazırlık, yüksek ve güvenli alana çıkma, hareketli suya girmeme, elektrik tehlikesi ve sel sonrası güvenli dönüş bilgileri: https://canakkale.afad.gov.tr/sel
- AFAD Antalya İl Müdürlüğü, Şiddetli Yağış ve Selden Korunma: değerli eşya ve belgeleri yükseltme, elektrik/doğalgazı güvenli biçimde kapatma, sel sırasında yüksek yere çıkma ve su çekildikten sonra tedbirli olma: https://antalya.afad.gov.tr/siddetli-yagis-ve-selden-korunma
- AFAD Güvenli Yaşam 1 Eğitim Kitapçığı, Sel/Taşkında Kendinizi Koruyun (PDF): https://cankiri.afad.gov.tr/kurumlar/cankiri.afad/dOKUMANLAR/Guvenli_Yasam_Klavuzu/gy_1_egitim_kitapcigi.pdf
- 112 Acil Çağrı Merkezi, Acil Durum Kılavuzu (PDF): sel veya su baskınlarında 112'ye başvuru bilgisi: https://112.gov.tr/kurumlar/112.gov.tr/IcSite/ankara/ACIL_DURUM_KILAVUZU.pdf
- 112 Acil Çağrı Merkezi, SMS İhbarları: konum/açık adres ve acil durum bilgisini iletme, mümkünse aramanın tercih edilmesi: https://112.gov.tr/112-sms-ihbarlari
- Quaternius Universal Base Characters (CC0 1.0): https://quaternius.itch.io/universal-base-characters
- Quaternius Universal Animation Library (CC0 1.0): https://quaternius.itch.io/universal-animation-library
- Kenney Animated Characters Retro (CC0 1.0): https://kenney.nl/assets/animated-characters-retro
- geoBoundaries Turkey ADM1, 2021: https://www.geoboundaries.org/api/current/gbOpen/TUR/ADM1/
- OpenStreetMap atıf ve lisans bilgisi: https://www.openstreetmap.org/copyright
