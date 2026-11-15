# Türkiye Afet — Proje Durumu

Son güncelleme: 2026-11-15

## Güncel Özet

Türkçe, 3D afet farkındalığı oyunu. 81 il haritasından il seçilir; seçilen ilin eğitim senaryosu açılır. Deprem, sel/taşkın, heyelan, orman yangını ve fırtına akışları birbirinden bağımsız çalışır. Oyun içeriğinin büyük bölümü kodla oluşturulur.

Kullanıcı önceki oyun denemesinde açık bir sorun olmadığını bildirdi; sel suyunda kalma başarısızlık akışının çalıştığını da ayrıca doğruladı. Bu geçmiş kullanıcı bildirimi bu güncellemedeki kod değişikliklerinin Play Mode'da doğrulandığı anlamına gelmez.

Bu güncellemede fırtına sonrası direk/kablo görsellerinin 18 saniyelik akış bitmeden tamamlanması, orman yangını güvenli alan bonusunun tek sefer verilmesi ve heyelan çarpışma/güvenli alan kontrolleri düzeltildi. Unity Play Mode doğrulaması yapılmadı.

Fırtına senaryosunda ev kapısının açık/kapalı durumu barınma güvenliğine bağlandı. Kapı açıkken içeri sığ su yayılıyor, oyuncu rüzgâr etkisi alıyor ve güvenli barınma koşulu sağlanmıyor; kapatınca su geri çekiliyor. Yağmur sesi gerçek bir CC0 kaydına geçirildi ve kapı kapalıyken belirgin biçimde kısılıyor.

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

### Heyelan

- Heyelan, mevcut 81 il haritası ve il bilgi kartının görünümünü/akışını kullanır. Eğitim senaryosu için Bingöl, Erzincan, Gümüşhane, Hakkâri, Tunceli, Bayburt ve Ardahan seçilebilir; bu seçim resmî heyelan risk sınıflaması değildir.
- Eski büyük düz heyelan duvarı, tehlike şeridi ve üç görev panosu kaldırıldı. Yüksekliği ve yüzeyi değişen özel bir yamaç ağı, düzensiz kaya yüzlekleri, açık toprak katmanları, dağ ağaçları, çalılar, kıvrımlı köy yolu ve yerleşim kurulur. Projede hazır dağ/çevre modeli bulunmadığı için bu alan kodla oluşturulmuş özel mesh ve sahne nesnelerinden oluşur.
- Afet öncesinde oyuncu zemindeki çatlakları, kökleri açılmış eğik ağacı ve yamaç eteğindeki yeni su sızıntısını dünyada bulup etkileşerek inceler. Hazırlık bitince mevcut bildirimde 5 saniyelik geri sayım görünür.
- Heyelan 18 saniyelik, farklı zamanlarda başlayan küçük taş, çamur kütlesi ve köşeli kaya dalgalarıyla ilerler. MeshCollider/Convex MeshCollider ve Rigidbody kullanılır; devrilmesi planlanan ağaçlar, hareketli ev ön duvarı ve çatı parçası ile yola inen çamur kütleleri de hareket eder. Hareketli ağaçlara, duvara ve çatıya kaya/toprak kütleleriyle aynı çarpma bileşeni eklenir. Ağır çarpma başarısız kılar; küçük taş çarpması puan düşürür; bileşen her nesne çarpmasını yalnızca bir kez işler.
- Oyuncu akış hattına paralel/aşağı yönde değil yana doğru kaçıp sahnede belirlenmiş güvenli çayıra ulaşmalı ve 18 saniye boyunca orada kalmalıdır. Güvenli çayır görselde 6,6 birim çapındadır; mantıksal kontrol aynı merkezde 3,3 birim yarıçaplı dairesel alanı ve yükseklik sınırını kullanır. Güvenli alanda sabit acil çağrı cihazı bulunur. Oyuncu 112 konuşmasında konum/yaklaşım bilgisini ve olası mahsur kişileri bildirir; güvenli alanda kalma ve alana geri dönmeme seçenekleri puanlanır.
- Senaryo eğitim amaçlıdır; gerçek zamanlı/yerel heyelan uyarısı veya il bazında risk haritası değildir.

### Fırtına

- Fırtına senaryosunda oyuncu hazırlık görevlerinden sonra sağlam evin içine girip kapıyı kapatmalı, pencerelerden uzakta kalmalı ve kısa fırtına süresince barınmalıdır.
- Kapı başlangıçta kapalıdır ve oyuncu etkileşimiyle açılıp kapanır. Fırtına sırasında kapı açık kalırsa içeri düzensiz dalgalı kenarla yayılan sığ su görseli oluşur; oyuncu içeri savrulan rüzgâr ve kamera sarsıntısı etkisi alır. Kapı açıkken güvenli barınma puanı kazanılamaz; fırtına bu koşulda biterse senaryo başarısız olur. Kapı kapanınca su kademeli olarak geri çekilir.
- Fırtına 18 saniye sürer. Yağmur sesi yerel Mixkit seslerinden değil, `Assets/Resources/Audio/StormRain.mp3` içindeki gerçek yağmur kaydından gelir. Kapı kapalıyken ses seviyesi belirgin biçimde düşürülür, açıkken yükselir; fırtına bitince kademeli olarak kapanır.
- Elektrik direğinin devrilme animasyonu ve kopmuş kablo/uyarı görselleri 16. saniyede tamamlanır/görünür olur; nesneler fırtına sonrası aşamaya geçince de görünür kalır.
- Kullanıcı önceki oyun denemesinde sorun olmadığını bildirdi. Bu belge güncellemesi sırasında Unity Play Mode çalıştırılmadı.
- Yağmur kaydı Freesound'da colorsCrimsonTears tarafından CC0 lisansıyla yayımlanan “Rainfall” kaydıdır: https://freesound.org/people/colorsCrimsonTears/sounds/532681/

### Orman Yangını

- Senaryo için eğitim ili Muğla'dır (plaka 48). Bu, `ProvinceManager` içindeki tek orman yangını eğitim seçeneğidir; resmî yangın tehlike/risk sınıflandırması veya il geneli risk iddiası değildir.
- Orman zemini, sırtlar, ağaç kümeleri, alev cephesi ve duman parçacıkları çalışma anında `PrototypeBootstrap` tarafından kodla oluşturulur. Açık tahliye patikası, gözetleme noktası/112 telefonu ve fenerli 9×9 birimlik güvenli toplanma alanı da aynı şekilde kurulur.
- Oyuncu dumanı güvenli mesafeden fark edip gözetleme noktasındaki ahizeli telefonla 112'yi arar. Görüşmede üç aşama vardır: yangının konumu/yakındaki yol veya yerleşim ve duman yönü; oyuncunun ve çevredeki kişilerin durumu; çağrıdan sonra güvenli tahliye davranışı. Her aşamada üç yanıt karıştırılarak sunulur; yanlış cevapta geri bildirim verilir ve aynı soruda seçenek sırası korunur. Üç doğru yanıt ihbarı tamamlar ve 100 puan verir.
- İhbar tamamlanıp hazırlık görevi bittiğinde mevcut hazırlık bildirimi içinde 5 saniyelik geri sayım başlar. Ardından 24 saniyelik tahliye süresi işler; alev cephesi her 4,8 saniyede bir ileri katmana geçer.
- Oyuncu görsel güvenli alanın içindeki mantıksal sınırda en az 3 saniye kesintisiz kalmalıdır. Alan dışına çıkmak bekleme süresini sıfırlar. Güvenli alana ilk ulaşma 150 puan verir; bu bonus aynı senaryo içinde ikinci kez kazanılamaz.
- Oyuncu ilerleyen alev koridorunda kalırsa veya 24 saniye içinde güvenli alanda üç saniyeyi tamamlayamazsa senaryo başarısız olur. Başarılı bitiş sonuç ekranını açar ve orman yangını için tamamlandı/son puan/en iyi puan kayıtlarını `PlayerPrefs` anahtarlarına yazar. Başarılı senaryo bitişi ayrıca 100 puanlık genel bitiş bonusu verir.
- Senaryo eğitim amaçlıdır; gerçek zamanlı yangın uyarısı, resmî risk haritası veya itfaiye yönlendirmesi değildir.

## Harita, Arayüz ve Ses

- 81 il, gerçek il sınırlarıyla seçilebilir. Tamamlanan afet illeri gri gösterilir ve yeniden seçilebilir.
- İl seçimi kartında imlecin üzerindeki ilin eğitim senaryosu ve afet türü gösterilir.
- Sol üst görev paneli uzun görev metinlerine göre büyür; puan satırı görevlerle çakışmaz.
- Sel geri sayımı mevcut hazırlık bildiriminin içinde gösterilir. Etkileşim olmayan boş noktaya basınca yanıltıcı geçici sel görevi mesajı gösterilmez.
- Oyun içi sesler yerel Mixkit dosyaları, üretilen telefon sesi ve fırtına için gerçek yağmur kaydı üzerinden oynatılır; arka plan müziği kullanılmaz.

## Başlıca Kaynak Dosyalar

- Assets/Scripts/GameManager.cs — Oyun durumu ve afet türü.
- Assets/Scripts/ProvinceManager.cs — İl verileri, eğitim senaryosu/afet türü eşlemesi ve tamamlanma kayıtları.
- Assets/Scripts/ProvinceMapView.cs — Türkiye il haritası etkileşimi.
- Assets/Scripts/PrototypeBootstrap.cs — Oyun sahnesi, ev, beş afet ortamı, görselleri ve etkileşimli nesneler.
- Assets/Scripts/PrototypeScenario.cs — Beş afetin görev/olay akışları, puanlama, tahliye, güvenli alan ve sonuç kayıtları.
- Assets/Scripts/PrototypeLandslideDebrisImpact.cs — Hareketli heyelan kütlesi, devrilen ağaç veya ev parçasının oyuncuya çarpmasını bir kez bildirir.
- Assets/Scripts/PrototypeInteractable.cs — Dünyadaki nesnelerle etkileşim; fırtına kapısı ve afetlere ait acil çağrı cihazları dâhil.
- Assets/Scripts/PrototypePlayerController.cs — Oyuncu hareketi, kamera, etkileşim ve sel suyunda hareket etkisi.
- Assets/Scripts/PrototypeUI.cs — Menü, il kartı, görev HUD'ı, geri sayım ve sonuç ekranları.
- Assets/Scripts/PrototypeAudio.cs — Hareket/etkileşim sesleri, fırtına yağmuru ve kapı açık/kapalı ses seviyesi.
- Assets/Resources/ProvinceMap/ — İl verisi, harita görseli ve seçim maskesi.
- Assets/Resources/Audio/Mixkit/ — Oyunda kullanılan yerel ses dosyaları.
- Assets/Resources/Audio/StormRain.mp3 — Fırtına sırasında döngülenen CC0 yağmur kaydı.
- Assets/Scenes/DepremPrototip.unity — Başlangıç sahnesi.

## Doğrulama ve Mevcut Durum

- Proje Unity 6000.6.7f1 kullanıyor.
- Kullanıcı önceki oyun denemesinde açık bir sorun olmadığını bildirdi; selde su altında kalma başarısızlık akışını da Play Mode'da doğruladığını belirtti.
- Bu güncellemedeki C# kaynakları derlendi: `Assembly-CSharp.csproj` başarılı, 0 uyarı ve 0 hata. `git diff --check` temiz çıktı.
- Unity Play Mode çalıştırılmadı; bu nedenle değişikliklerin oyun içi davranışı bu güncellemede test edilmiş olarak belirtilmiyor.

## Bilinen Tasarım Sınırları

- İl afet türleri eğitim senaryosu seçimi içindir; resmî tehlike veya risk tahmini olarak sunulmaz.
- Ev, merdiven ve birçok eşya oyun prototipi için kodla oluşturulmuş stilize modellerdir.
- İl sınırı verisi geoBoundaries Turkey ADM1 2021 kaynağıdır; resmî kadastral ölçüm olarak sunulmaz.

## Kaynak ve Atıflar

- AFAD, Deprem öncesi, anı ve sonrası alınabilecek önlemler: https://www.afad.gov.tr/deprem-oncesi-ani-ve-sonrasi-alabileceginiz-onlemleri-biliyor-musunuz
- AFAD Çanakkale İl Müdürlüğü, Sel: sel öncesi hazırlık, yüksek ve güvenli alana çıkma, hareketli suya girmeme, elektrik tehlikesi ve sel sonrası güvenli dönüş bilgileri: https://canakkale.afad.gov.tr/sel
- AFAD Antalya İl Müdürlüğü, Şiddetli Yağış ve Selden Korunma: değerli eşya ve belgeleri yükseltme, elektrik/doğalgazı güvenli biçimde kapatma, sel sırasında yüksek yere çıkma ve su çekildikten sonra tedbirli olma: https://antalya.afad.gov.tr/siddetli-yagis-ve-selden-korunma
- AFAD Hazırım, Heyelan İçin Hazırlık Rehberi: heyelan belirtilerini tanıma ve bildirme, akış hattına dik yönde uzaklaşma, kapalı alanda güvenli davranma ve olay sonrası resmî ekipleri bekleme: https://hazirim.gov.tr/tr/afetler/heyelan
- AFAD Güvenli Yaşam 1 Eğitim Kitapçığı, Sel/Taşkında Kendinizi Koruyun (PDF): https://cankiri.afad.gov.tr/kurumlar/cankiri.afad/dOKUMANLAR/Guvenli_Yasam_Klavuzu/gy_1_egitim_kitapcigi.pdf
- 112 Acil Çağrı Merkezi, Acil Durum Kılavuzu (PDF): sel veya su baskınlarında 112'ye başvuru bilgisi: https://112.gov.tr/kurumlar/112.gov.tr/IcSite/ankara/ACIL_DURUM_KILAVUZU.pdf
- 112 Acil Çağrı Merkezi, SMS İhbarları: konum/açık adres ve acil durum bilgisini iletme, mümkünse aramanın tercih edilmesi: https://112.gov.tr/112-sms-ihbarlari
- Quaternius Universal Base Characters (CC0 1.0): https://quaternius.itch.io/universal-base-characters
- Quaternius Universal Animation Library (CC0 1.0): https://quaternius.itch.io/universal-animation-library
- Kenney Animated Characters Retro (CC0 1.0): https://kenney.nl/assets/animated-characters-retro
- geoBoundaries Turkey ADM1, 2021: https://www.geoboundaries.org/api/current/gbOpen/TUR/ADM1/
- OpenStreetMap atıf ve lisans bilgisi: https://www.openstreetmap.org/copyright
