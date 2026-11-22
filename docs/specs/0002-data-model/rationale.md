# 0002. Veri modeli: gerekçe

## Context

Roomly'nin ilk dilimleri (kayıt ve giriş, davet koduyla katılım, kod yönetimi, hesap silme) ve ekibin diğer modülleri (UC2 ile UC10) aynı kullanıcı ve grup tablolarına bağlanacak. Şema bir kez yayılınca değiştirmek pahalı: her modülün FK'leri, sorguları ve testleri ona dayanır. `AppDbContext` ve migration'lar ortak dosya, her değişiklik ekip onayı ister ve `ModelSnapshot` paralel branch'lerde çakışır.

Kalite gereksinimleri şemayı doğrudan zorluyor: eşzamanlı katılımda kapasite aşılmamalı (BND-01, TRG-02), kapasite grup türüne göre sınırlı (BND-02: Ev 2 ile 20, Yurt 2 ile 6), zaman test edilebilir saatten okunmalı (BND-03), davet kodu yalnızca SHA-256 özeti olarak durmalı, grupta tek yönetici ve tek aktif kod olmalı, kişisel veri en azda tutulmalı ve hesap silmede anonimleştirilmeli (KVKK, SEC-07). Birden fazla aktif grup kapsam dışı.

Ekip 7 kişilik bir ders ekibi; kurallar yalnızca servis kodunda kalırsa, bir modül sahibinin yazdığı sorgu ya da paralel bir istek kuralı kolayca delebilir. Davet kodunun yalnızca özet olarak saklanması, scope #10'daki "yönetici aktif kodu görür" beklentisiyle çelişiyordu; bu çelişki konuşmada çözüldü.

## Options considered

### Option 1: Kuralları veritabanında da koruyan model (seçilen)

Üyelikte rol, tek grup için `user_id` benzersiz index'i, tek yönetici ve tek aktif kod için kısmi benzersiz index'ler, BND-02 için check kısıtı; snake_case adlar, yalnızca kullanıcı tablolu Identity.

**Pros**:
- Yarış durumunda bile bozuk veri yazılamaz; kural gerçek PostgreSQL'de test edilir.
- Yönetici devri ve ayrılma yalnızca üyelik satırlarına dokunur.

**Cons**:
- Kural değişikliği migration ister; check metni migration'a gömülür.
- Ekip kısmi index ve check kısıtı okumayı bilmeli.

### Option 2: En sade şema, kurallar yalnızca serviste

Yalnızca PK, FK ve `(group_id, user_id)` benzersizliği; tek grup, tek yönetici, kapasite ve tek kod kuralları servis kodunda.

**Pros**:
- En hızlı kurulum; kural değiştirmek migration istemez.

**Cons**:
- Paralel iki istek (ör. iki yeni kod, iki gruba aynı anda katılım) kuralı deler; her modül sahibi kuralı kendi sorgusunda hatırlamak zorunda.

### Option 3: Grupta yönetici sütunu, yumuşak ayrılma

`groups.admin_user_id` ile tek yönetici, üyeliklerde `left_at` ile geçmiş korunur.

**Pros**:
- Tek yönetici sütunun doğası gereği garanti; eski üyeler listelenebilir.

**Cons**:
- Yönetici sütunu üyelikle senkron tutulmalı (yönetici üye olmayabilir); her benzersiz index ve sorgu `left_at IS NULL` filtresi ister. Finansal geçmiş zaten diğer modüllerin satırlarında `user_id` ile duruyor.

## Rationale

Option 1 seçildi çünkü bu projenin en riskli noktası eşzamanlılık ve çok kişili kod: BND-01 / TRG-02 testleri paralel istek atıyor ve 7 kişi aynı tablolara sorgu yazıyor. Kuralın veritabanında durması, servis hatasını "bozuk veri" yerine "23505 hatası"na çevirir. Option 2'nin kazandırdığı esneklik (migration'sız kural değişikliği) ders ölçeğinde nadiren gerekecek, kaybettirdiği güvence ise her gün gerekir. Option 3'ün geçmiş koruma faydası gereksiz: borç ve gider satırları kendi `user_id` ve `group_id`'sini taşıyor, üyelik satırını silmek geçmişi silmiyor.

Kapasite önce serbest sayı olarak konuşuldu, sonra BND-02 gereğince türe bağlı aralığa düzeltildi; `type` sütunu bu yüzden etiket değil, kuralın girdisi. Davet kodunu görüntüleme için "yalnızca üretimde bir kez göster" seçildi, çünkü scope'un "yalnızca SHA-256 özeti" kuralını aynen korur; şifreli kopya saklamak bu kuralı bozardı, telefonda yerel kopya ise cihaz değişince kaybolurdu.

Konuşmada açıkça sorulmayan küçük kararlar ve gerekçeleri:
- **Üyelikte bileşik PK `(group_id, user_id)`** (yerine ayrı `id` sütunu): scope'un benzersizlik kuralını PK'nin kendisi taşır, ek index gerekmez. İkinci seçenek: `id` + benzersiz index, diğer tablolar üyeliğe FK verecekse tercih edilirdi; şimdilik kimse vermiyor.
- **`expires_at` saklanır** (yerine `created_at + 48h` sorguda hesaplanmaz): ömür kuralı ileride değişirse eski kodların süresi geriye dönük değişmez. İkinci seçenek: hesaplamak, bir sütun az.
- **Özet sütunlarında `octet_length = 32` check'i**: yanlış özet (ör. hex metin baytları) yazmayı hemen yakalar. İkinci seçenek: yalnızca kodda kontrol.
- **Enum'lar için `role IN (...)` check'i**: elle yazılan SQL ya da seed yanlış değer yazamaz. `type` için BND-02 check'i bunu zaten kapsar.
- **`replaced_by_id` için `SET NULL`**: kullanıcı silinince cascade ile token'lar giderken kendi kendine FK'nin `RESTRICT`'e takılmasını önler. İkinci seçenek: `NO ACTION`.
- **Zamanlar uygulamadan, DB'de `now()` yok**: BND-03 gereği zaman tek kaynaktan (`IClock`) gelir, testte `FakeClock` ile sınırlar (48. saat) tam doğrulanır.
- **Identity tablo adları `asp_net_*` kalır**: yeniden adlandırmak ek yapılandırma ister, kazancı yalnızca kozmetik.
- **Sabit kısıt adları ve "önce bırak, sonra ekle" yazma sırası** (bağımsız bir model incelemesinden geldi): sonraki özellikler veritabanı hatasını kısıt adıyla hata koduna çevirecek, adların paketin varsayılanına bırakılması kırılgan olurdu. EF tek `SaveChanges` içinde kısmi index koşulunu bilmediği için ekleme ile iptali yanlış sırada yazabilir; sırayı açıkça vermek ertelenebilir kısıt (deferrable constraint) gibi daha karmaşık çözümlerden sade. İkinci seçenek: kısmi index yerine tetikleyici (trigger), ekip için okunması zor.
- **Davet kodu tuzsuz SHA-256**: kod katılımda özetle aranır, tuz (salt) aramayı imkansız kılardı. Güvenlik uzunluk ve 48 saatlik ömre dayanır. İkinci seçenek: sunucu sırrıyla HMAC, ek bir sır yönetimi ister.
- **Test adlarında `DM` öneki**: proje gereksinim kimliği olmayan şema kurallarını izlenebilir kılar (`DM04` = bu spec'in AC-4'ü); kimliği olanlar (`BND02`) kendi kimliğini kullanır.
