# Türkiye Afet — Proje Durumu

Son güncelleme: 2026-10-25

## Amaç

Türkiye'de afetlere hazırlık ve doğru davranışları öğreten, Türkçe, 3D ve Windows hedefli bir oyun geliştirmek. Bu dosya mevcut geliştirme durumunu ve doğrulanmış testleri kaydeder.

## Teknik Bilgiler

- Unity Editor: 6000.6.4f1
- Proje klasörü: C:\Users\User\Documents\Codex\2026-10-03\TurkiyeAfet
- Render pipeline: Universal Render Pipeline
- Giriş: Unity Input System
- Aktif başlangıç sahnesi: Assets/Scenes/DepremPrototip.unity
- Build listesinde DepremPrototip ilk sırada; özgün SampleScene ikinci sırada tutuluyor.

## Mevcut Dosyalar

- Assets/Scripts/GameManager.cs — Projenin mevcut oyun durumu yöneticisi; bu aşamada değiştirilmedi.
- Assets/Scripts/ProvinceManager.cs — 81 il kataloğu, plaka/sınır kimliği kontrolleri ve il bazında kayıt anahtarları.
- Assets/Scripts/ProvinceMapView.cs — Harita piksel maskesiyle il hover'ı, tıklaması ve klavye seçimi.
- Assets/Scripts/PrototypeBootstrap.cs — Işık, ev ortamı, mobilyalar, giriş/çıkış ve oyun akışını runtime'da oluşturur.
- Assets/Scripts/PrototypePlayerController.cs — Ayakta birinci şahıs, çömelme/tutunmada üçüncü şahıs kamera; WASD hareketi, koşma, animasyonlu çömelme, bağlama duyarlı E etkileşimi, kamera çarpışması ve hedef yönlendirmesi.
- Assets/Scripts/PrototypeInteractable.cs — Hazırlık, gaz vanası ve pencere görevleri; aktif hedef görsel vurgusu.
- Assets/Scripts/PrototypeScenario.cs — Hazırlık, sarsıntı, güvenli bölge/çökme/tutunma koşulları, deprem sonrası, puan ve ilerleme akışı.
- Assets/Scripts/PrototypeUI.cs — Mevcut ana menü stili korunarak harita, senaryo tanıtımı, görev HUD'ı, duraklatma ve sonuç ekranları.
- Assets/Resources/ProvinceMap/provinces.json — Türkçe il adları, 01–81 plakaları ve genel eğitim senaryosu verileri.
- Assets/Resources/ProvinceMap/turkey-provinces.png — 81 gerçek idari sınırı gösteren harita görseli.
- Assets/Resources/ProvinceMap/province-id-mask.bytes — Harita hover/tıklama için plaka kodlu piksel maskesi.
- Assets/MapData/turkey_provinces_geoboundaries_simplified.geojson — geoBoundaries Turkey ADM1 kaynak geometrisi.
- Assets/MapData/ATTRIBUTION.md — Harita kaynağı ve CC BY-SA 2.0 atıf/lisans bilgisi.
- Assets/Editor/PrototypeSceneSetup.cs — Unity menüsünden prototip sahnesini üretir ve build listesine ekler.
- Assets/Editor/QuaterniusPlayerSetup.cs — Karakter FBX'i için Humanoid Avatar eşlemesini yeniler, model ve UAL kliplerini içe aktarır ve Animator Controller durumlarını kurar.
- Assets/Resources/PlayerCharacter/ — Quaternius Animated Men sivil Humanoid modeli, Quaternius Humanoid animasyonları ve Animator Controller.
- Assets/ThirdParty/Quaternius/ — CC0 lisans metinleri ve varlık atıfları.
- Assets/ThirdParty/Kenney/AnimatedCharactersRetro/ — Sivil karakterin CC0 lisansı, önizlemesi ve kaynak atfı.
- Assets/Scenes/DepremPrototip.unity — Prototip başlangıç sahnesi. Ev ve arayüz oyun çalışırken yerleşik Unity nesneleriyle üretilir.
- Assets/Scenes/SampleScene.unity — Özgün örnek sahne korunmuştur.
- ProjectSettings/EditorBuildSettings.asset — Prototip sahnesi başlangıç sırasına alınmıştır.


## Tamamlananlar

- Ana menüden il seçimine ve senaryo tanıtımına geçiş.
- Türkiye'nin 81 ilini gerçek il sınırları üzerinden seçme; haritada hover/tıklama, plaka koduyla seçili il vurgusu ve il bilgi kartı.
- 01–81 arası plaka kodları, Türkçe il adları ve gelecekteki farklı senaryolara uygun veri alanları.
- geoBoundaries Turkey ADM1'nin 2021 sınır verisinden üretilen harita ve oyun ekranında görünür CC BY-SA 2.0 atfı.
- Her ilde aynı genel deprem eğitim senaryosu; veri gerçek risk tahmini olarak sunulmaz.
- Yerleşik primitive nesnelerle oluşturulan 3D ev, giriş, çıkış yolu, mobilyalar ve güvenli alan.
- WASD hareketi, fare bakışı, Shift koşusu, C çömelmesi ve E etkileşimi.
- Quaternius Animated Men CC0 `Male_Casual.fbx` mevcut sivil model dosyası olarak yerleştirildi. Önizlemesi doğal kafa/vücut oranı, saç, uzun kollu üst, pantolon ve ayakkabı gösteriyor; Unity importu ve oyundaki görünümü henüz doğrulanmadı. Kamera kontrol kodu değiştirilmedi.
- C ile çömelme kodu `Crouch` Animator durumunu çağırır; bu klip UAL1 içindeki `Crouch_Idle_Loop`'tur. Eski Kenney modelinden kalan Avatar iskelet eşlemesi temizlendi; model FBX'inin animasyon importu açıldı ve modelin kendi Idle/Walk/Run klipleri Animator Controller'a atanacak. Unity Editor'de yeniden import ve Play Mode doğrulaması bekliyor.
- Özel IK kaldırıldı. `Male_Casual.fbx` içinde masa korunma/tutunma klibi bulunmuyor; oyun tutunma süresini ve konumunu puanlar, ancak karakter görsel olarak Crouch klibini oynatır ve gerçek tutunma pozu göstermez.
- Deprem anında masanın önünde güvenli bölge halkası ve yön oku; doğru yerde çömelme ve tutunma ayrı HUD koşullarıdır.
- Deprem 10 saniye sürer; güvenli bölgede çöküp masaya 5 saniye tutunma 100 puan kazandırır. Bu koşul tamamlanmazsa puan kesilmez.
- Rafı sabitleme, ağır eşyayı alt seviyeye alma, çıkış yolunu açma görevleri.
- Deprem sarsıntısı, kamera sarsılması ve sabitlenmemiş nesnelerin fiziksel tepkisi.
- Masanın yanında çök-kapan-tutun uygulaması, gaz vanası görevi, binadan çıkış ve toplanma alanına ilerleme.
- Gaz kokusu eğitiminde vanayı kapatma ve pencereyi açma adımları etkileşimlidir; ikisi de 100 puan verir ve cam açılışta yana kayar.
- Puan, sonuç değerlendirmesi ve yerel tamamlanma/yüksek puan kaydı.
- Ana menü, oyun içi görev paneli ve duraklatma/sonuç ekranı Türkçe.
- Etkin hazırlık görevi panelde renkli/simgeli işaretlenir; hedef nesne renk ve emisyonla vurgulanır.
- Hedef görünmüyorsa veya duvar arkasındaysa yön oku, nesne adı, eylem ve mesafe gösterilir; duvar arkasındaki nesne görünür kılınmaz.
- Hedefe bakınca istem nesne adını ve E eylemini gösterir; yanlış nesnede kısa geri bildirim vardır.
- Hazırlık bitince ayrı fare düğmesi kaldırılmıştır; sarsıntı Enter ile başlatılacak şekilde kodlanmıştır.
- Menü ve il seçimi ekranlarında imleç görünür; oyuncu kontrolünde kilitlenir, uygulama odağı kaybolursa geçici olarak açılır.
- AFAD'ın deprem öncesi, anı ve sonrası davranış rehberine dayalı ilk eğitim mesajları.

## Test Edilenler

- Unity Editor 6000.6.4f1 kaynakları içe aktardı; Play Mode açılışındaki Console: 3 bilgi logu, 0 uyarı, 0 hata.
- Statik katalog kontrolü: 81 kayıt, 81 benzersiz plaka, TR-01–TR-81 sınır kimliği eşleşmelerinde hata yok.
- Piksel maskesi kontrolü: 1200×720; 01–81 arası tüm plaka kimlikleri kapsanıyor, eksik/fazla kimlik yok.
- Play Mode'da ana menü, harita, il kartı, Ankara tanıtımı ve 3D oynanış açıldı.
- Play Mode'da stilize insan karakteri ve üçüncü şahıs kamera görüntülendi; ana menü tasarımı korundu.
- Unity kaynak yenilemesinden sonra Play Mode'da ana menü ve gerçek il sınırı haritası açıldı; Kırşehir 40 fareyle seçilip il kartı güncellendi.
- Haritadaki “SENARYOYU GÖR” düğmesine otomasyon tıklaması sayfa geçişi oluşturmadı; bu düğme gerçek fareyle yeniden doğrulanmalı.
- Quaternius animasyon FBX'inde idle, walk, jog ve crouch kliplerinin döngü ayarları içe aktarılan meta verisinde 1 olarak doğrulandı.
- Unity Console son gözleminde 0 uyarı ve 0 hata gösterdi. Windows build'i alınmadı.
- Bu oturumda Quaternius Animated Men `Male_Casual.fbx` önizlemesi ve CC0 lisans dosyası incelendi; dosya mevcut sivil model dosyasının yerine alındı.
- Bozuk IK katmanı ve masa ayağı/el hedef koordinatı hesabı kaldırıldı; dosya adı/kodu proje içinden silindi. Mevcut Humanoid `Crouch_Idle_Loop` klibi `Hold` durumu için atanmış durumda.
- Bu oturumda Unity Editor kapalıydı. Modelin Unity Humanoid avatar eşlemesi, animasyon retarget'ı, görünüşü ve masa altı pozisyonu Editor/Play Mode'da test edilmedi.
- Unity 6000.6.4f1 batchmode içe aktarma/derleme denemesi lisans istemcisine bağlanamadı; Unity logunda Windows WMI `wmiOpened` assertion hatasıyla çıkış kodu 1 görüldü. Bu yüzden derleme ve Play Mode sonucu yok.
- Unity'nin kurulu Roslyn derleyicisiyle güncel `Assembly-CSharp` ve `Assembly-CSharp-Editor` C# kaynakları, Editor'ın önceki derleme referansları kullanılarak geçici çıktılara derlendi; iki derleme de çıkış kodu 0 verdi. Bu, Unity asset importu veya Play Mode testi değildir.
- 2026-10-12: `Male_Casual.fbx` gerçek klip adları binary FBX verisinden incelendi: `Man_Idle`, `Man_Walk`, `Man_Run`, `Man_Clapping`, `Man_Death`, `Man_Jump`, `Man_Punch`, `Man_RunningJump`, `Man_Sitting`, `Man_Standing`, `Man_SwordSlash`; crouch/cover/protect/hold klibi yok.
- 2026-10-12: Karakter modelinin `.meta` dosyasında önceki Kenney iskeletinin `HipsCtrl` ve `characterMedium` kemik eşlemesi bulundu; animasyon importu kapalıydı. Eski eşleme temizlenip `importAnimation` açıldı. Controller hazırlama kodu modelin kendi idle/yürüme/koşma kliplerini ve UAL çömelme klibini mevcut state'lere bağlayacak şekilde güncellendi.
- 2026-10-12: Unity Roslyn ile güncel oyun ve Editor kaynakları yeniden derlendi; iki derleme de çıkış kodu 0 verdi. Unity Editor içe aktarma ve Play Mode animasyon testi bu oturumda yapılmadı.
- Önceki bir oturumda Unity batchmode, proje başka bir Unity Editor örneğinde açık olduğu için çalışmamıştı. Bu oturumdaki yeni deneme farklı bir engelle, lisans istemcisine bağlanamama ve Windows WMI assertion ile sonlandı (çıkış kodu 1).
- Harita tıklamasında Adana 01, Ankara 06, İstanbul 34, İzmir 35 ve Kocaeli 41 doğru gösterildi; her biri haritada vurgulandı.
- İlk görev HUD'ı ve duvar önünde yön oku; son derlemede “Kitaplık • Rafı duvara sabitle • 9 m” hedef metni görüntülendi.
- Oyun görünümünde fare hareketi üçüncü şahıs kamera bakışını değiştirdi.
- Bu oturumda C/W/Enter girdileri Unity Game View'a masaüstü otomasyonundan geçmedi; normal çömelme, masaya tutunma animasyonu, güvenli bölge algısı ve görev tamamlama Play Mode'da uçtan uca doğrulanamadı.
- ESC/WASD/E klavye girdileri gerçek klavyeyle ayrıca denenmeli.
- Yeni birinci/üçüncü şahıs geçişi, fare dikey bakışı ve kamera çarpışması Play Mode'da ayrıca denenmeli.
- Windows çalıştırılabilir build'i henüz üretilmedi veya test edilmedi.
- Hazırlık görevlerinin tamamlanması, yanlış etkileşim bildirimi, deprem/sonrası ve kayıt sonucu uçtan uca Play Mode'da henüz doğrulanmadı.
- Bu oturumda Unity Editor 6000.6.4f1 açıldı; Play Mode'da ana menü, il haritası, Yozgat seçimi, senaryo tanıtımı ve 3D hazırlık sahnesi gerçekten görüntülendi. Oyun sahnesi Console sayacı 0 uyarı ve 0 hata gösteriyordu.
- Play Mode otomasyonunda fare hareketi kamera bakışını değiştirdi; klavye girdileri C/E/WASD akışına tutarlı ulaşmadı. Bu nedenle yeni modelin görünüşü, çömelme/tutunma pozu, masa altına giriş ve görev tamamlama bu oturumda Play Mode'da doğrulanmış sayılmaz.

## Bilinen Sınırlar

- 81 il seçilebilir; her ildeki içerik şimdilik aynı genel deprem eğitim prototipidir ve doğrulanmış risk dağılımı değildir.
- Harita sınır verisi geoBoundaries/OpenStreetMap kaynaklı, sınır yılı 2021'dir; resmi ölçüm/kadastral harita olarak sunulmaz.
- Ev ve mobilyalar geçici, kodla oluşturulmuş 3D nesnelerdir. Hareket klipleri Quaternius CC0 varlıklarıdır. Tutunma animasyonu bulunmadığından bu hareket görsel olarak gerçek masa tutuşunu göstermemektedir.
- Yeni Quaternius modelinin yenilenmiş Humanoid Avatar'ı ve C çömelme geçişi Unity Editor/Play Mode'da ayrıca doğrulanmalı. E ile konum/süre tabanlı tutunma çalışır, ancak karakter animasyonu gerçek masa tutuşunu göstermemektedir.
- Sahne içeriği Play sırasında oluşturulur; Scene görünümünde düzenlenebilir mobilya prefab'ları henüz yoktur.
- Ayarlar menüsü, diğer afetler, resmî kaynaklarla belgelenmiş geniş eğitim içeriği, yapılandırılmış profil kaydı ve Windows build'i bekliyor.
- Bu oturumda Unity 6000.6.4f1 batchmode lisans istemcisi/WMI hatasıyla kapanmıştır; yeni model importu, C# derlemesi ve Play Mode doğrulanmadı.

## Kaynak

- AFAD, Deprem öncesi, anı ve sonrası alınabilecek önlemler: https://www.afad.gov.tr/deprem-oncesi-ani-ve-sonrasi-alabileceginiz-onlemleri-biliyor-musunuz
- Quaternius Universal Base Characters (CC0 1.0): https://quaternius.itch.io/universal-base-characters
- Quaternius Universal Animation Library (CC0 1.0): https://quaternius.itch.io/universal-animation-library
- Kenney Animated Characters Retro (CC0 1.0): https://kenney.nl/assets/animated-characters-retro
- geoBoundaries Turkey ADM1 (81 il, 2021 sınır verisi, CC BY-SA 2.0; kaynak verisi OpenStreetMap): https://www.geoboundaries.org/api/current/gbOpen/TUR/ADM1/
- OpenStreetMap atıf ve lisans bilgisi: https://www.openstreetmap.org/copyright

## Son Değişiklik

2026-10-12: Unity Console çıktısı, önceki Humanoid haritasının yanlış üretildiğini gösterdi: kaynak iskelette kalça `Body`, omurga `Hips > Abdomen > Torso`; ayak kemikleri üst bacak zincirinin dışında. Eşleme bu hiyerarşiye göre güncellendi; zorunlu ayak uçları `LowerLeg.L_end` / `LowerLeg.R_end` olarak atanıyor. Var olan ama yanlış otomatik eşlemenin de yenilenmesi için kontrol eklendi. Klipler ilk kontrolde görünmüyorsa FBX'ler eşzamanlı yeniden içe aktarılıyor ve klip araması tekrarlanıyor. Editor C# kaynak derlemesi başarılı; Unity yeniden içe aktarma ve Avatar geçerliliği henüz doğrulanmadı.

2026-10-12: `Male_Casual.fbx.meta` içindeki eski Kenney kemik eşlemesi temizlendi ve gerçek FBX animasyon kliplerinin importu açıldı. Animator hazırlama kodu modelin gerçek Idle/Walk/Run kliplerini, UAL'ın gerçek Crouch klibini kullanacak ve mevcut Controller'ı yenileyecek şekilde güncellendi. Model ve UAL kaynaklarında uygun Hold/Protect/Cover klibi olmadığından E için sahte Hold durumu kaldırılacak; senaryo bunu açıkça bildirecek. Kamera, WASD/fare, harita ve senaryo geçişlerine dokunulmadı. Statik C# derlemeleri başarılı; Unity import/Play Mode testi yapılamadı.

## Bir Sonraki Somut Adım

Unity Editor'de model FBX'ini yeniden içe aktar; Humanoid Avatar'ın geçerli olduğunu ve `Crouch` durumunun C ile gerçekten oynadığını Play Mode'da kontrol et. E animasyonu için gerçek korunma/tutunma klibi içeren uygun bir kaynak edinilmeden mekanik tamamlandı sayılmamalı. Sonrasında E'nin koşul kontrolünü, kamera/kontrolleri ve senaryo mesajlarını görüntülü olarak doğrula.



## 2026-10-25 — Enkaz senaryosu ve puan akışı

- 10 saniyelik deprem sonunda, tutunma tamamlanmış olsun veya olmasın, %50 olasılıkla enkaz senaryosu başlar. Enkaz oluşursa ekran 2,5 saniye tamamen kararır ve normal deprem sonrası görevler yerine enkaz altı bilgi görevleri açılır.
- Enkaz altı görevleri: tozdan korunmak için ağız-burnu bez/giysiyle örtme; durumu kontrol edip tehlikeli hareketlerden kaçınarak enerjiyi koruma; kurtarma ekiplerini dinleyip aralıklı ses/vuruşla yerini bildirme. Her doğru adım 100 puan verir.
- Bu içerik AFAD'ın enkaz altında mahsur kalma yönergelerindeki sakin kalma, hareketleri kontrol etme, enerjiyi koruma ve kontrollü ses verme önerilerine dayanır.
- Pencere görevi 100 puan, 5 saniyelik tutunma 100 puan verir. Başarılı normal tahliye ve başarılı enkaz senaryolarının toplam üst sınırı 700 puandır.
- Unity Play Mode uçtan uca doğrulaması bu değişiklikten sonra yapılmadı.
