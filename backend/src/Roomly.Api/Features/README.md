# Features

Her modül kendi klasöründe çalışır: `Features/<Modul>/`

```
Features/<Modul>/
  <Modul>Controller.cs   // [Route("api/...")], sonucu ToActionResult ile döner
  <Modul>Service.cs      // iş kuralları, Result<T> döner
  <Modul>Repository.cs   // AppDbContext erişimi
  Dtos/
  Validators/
  README.md              // varsayımlar ve eklenen paketler
```

Ortak parçalar `Common/` ve `Infrastructure/` altındadır; onlara dokunan değişiklikler onaya bağlıdır.
