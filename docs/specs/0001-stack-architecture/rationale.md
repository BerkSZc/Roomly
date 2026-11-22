# 0001. Gerekçe: Roomly stack ve mimari

## Context

Roomly, Yazılım Kalite Standartları dersinin Faz 2 projesi. 7 kişilik ekip, her biri bir ya da iki use case'in sahibi (UC1 ile UC10 arası). Hedef platform Android (emülatör ve USB ile bağlı cihaz); iOS kapsam dışı. Ekip `main` dalını korumalı tutuyor ve herkes kendi modül klasöründe, kendi branch'inde çalışıyor; ortak dosyalar onaya bağlı.

Kalite gereksinimleri teknoloji seçimini doğrudan etkiliyor: şifrelerin standart bir kütüphaneyle hash'lenmesi ve hatalı girişte kilit (SEC-01), kimlik doğrulama isteklerinde hız sınırı (SEC-04), token'ların güvenli saklanması (SEC-05), eşzamanlı katılımda kapasitenin aşılmaması için transaction ve satır kilidi (BND-01, TRG-01), zamanın test edilebilir bir saat arayüzünden okunması (BND-03), kişisel verinin en aza indirilmesi ve loglarda maskelenmesi (SEC-07, KVKK). Testler gerçek PostgreSQL üzerinde çalışmalı.

Geliştirme makinesinde yerel PostgreSQL yok, Docker Desktop var. .NET 8 SDK kurulu değil (SDK 9 ve 10 var, 8.0 runtime var). Flutter ve `adb` henüz PATH'te değil. Backend bitmeden mobil ekranların çalışabilmesi gerekiyor.

Bu karar verilmezse 7 kişi farklı klasör düzenleri, farklı hata biçimleri ve farklı ortam ayarlarıyla başlar; birleştirme maliyeti ve kalite testlerinin tutarsızlığı faz sonunda patlar.

## Options considered

Ana stack (Flutter, ASP.NET Core 8, PostgreSQL, Riverpod, dio, go_router, Identity, EF Core) ekip tarafından proje başında belirlendi; burada seçenek olarak tekrar değerlendirilmedi. Değerlendirilen, bu stack'in nasıl yerleştirileceği ve kurulacağıydı.

### Option 1: Feature first modüler monolit, tek API projesi + iki test projesi

Backend tek API projesi, modüller `Features/<Modul>/` altında; birim ve entegrasyon testleri ayrı projelerde. Mobilde `lib/features/<modul>/` ve `lib/core/`.

**Pros**:
- Modül sahipliği klasör sınırıyla örtüşüyor; çakışmalar ortak dosyalarla sınırlı.
- Docker gerektiren testler ayrı çalıştırılabiliyor.
- Kurulumu ve debug'ı en basit yapı.

**Cons**:
- Modüller arası bağımlılığı derleyici engellemiyor; disiplin kod incelemesine kalıyor.
- `AppDbContext` ve migration'lar ortak, çakışma riski var.

### Option 2: Katmanlı projeler (Api / Application / Domain / Infrastructure)

**Pros**:
- Katman bağımlılıkları derleyiciyle zorlanıyor.

**Cons**:
- Her modül dört projeye dağılıyor; feature first ilkesiyle çelişiyor ve 7 kişi aynı projelerde çakışıyor.
- Ders ölçeği için gereksiz tören.

### Option 3: Tek API projesi + tek test projesi

**Pros**:
- En az proje dosyası.

**Cons**:
- Docker gerektiren testler trait ile ayrılmak zorunda; unutulursa CI ve yerel test kırılır.

## Rationale

Option 1 seçildi çünkü belirleyici kuvvet ekip yapısı: 7 kişi paralel, kendi klasöründe çalışıyor ve ortak dosyalar onaya bağlı. Klasör sınırı sahiplik sınırıyla örtüşünce onay gereken dosya sayısı en aza iniyor. Katmanlı yapı (Option 2) derleyici zorlaması getirirdi ama her modülü dört projeye bölüp tam da kaçınmak istenen çakışmaları artırırdı.

Hata deseninde `Result<T>` seçildi (runner up: domain exception ve global handler). BND ve TRG testleri hata kodunu doğrudan doğruluyor; servisin sonucu açıkça döndürmesi bu testleri basitleştiriyor ve akışı okunur tutuyor.

Zaman için kendi `IClock` arayüzü seçildi (runner up: .NET 8'in yerleşik `TimeProvider`). `TimeProvider` daha standart, ama gereksinim metni `IClock` diyor ve izlenebilirlik açısından isim eşleşmesi değerli. `SystemClock` içeride `TimeProvider.System` kullanabilir.

SDK için SDK 10 ve `net8.0` hedefi seçildi (runner up: .NET 8 SDK kurmak), çünkü kurulum gerektirmiyor ve SDK 10 `net8.0` hedefini resmi olarak destekliyor. Bedeli, ekipteki herkesin SDK 10 kurması.

FluentAssertions `7.x`'te sabitlendi; 8 ve sonrası ticari lisans gerektiriyor. Moq'ta SponsorLink içeren `4.20.0` sürümünden kaçınmak için `4.20.72` ve sonrası.

Ortam seçimi için `--dart-define` seçildi (runner up: Android flavor'ları). Flavor'lar ortak `build.gradle` dosyasını değiştirir ve gereksiz karmaşa ekler; üç hazır VS Code profili aynı işi görür.
