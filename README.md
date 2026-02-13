# HesapTakip (Windows)

Basit tek-kullanıcı hesap takip uygulaması:
- Kurlar: USD/EUR/GBP
- Hesap Takip: tarih, açıklama, gelir/gider, döviz, tutar(FX), otomatik kur ve TRY
- SQLite ile kalıcı kayıt (%LOCALAPPDATA%\HesapTakip\hesap_takip.db)
- CSV dışa aktar

## GitHub Actions ile otomatik .exe
Repo'ya push edince Actions bir Windows .exe build eder.

1) GitHub'da Actions sekmesi -> en son workflow run'ı aç
2) Artifacts bölümünden `HesapTakip-win-x64` indir
3) İçindeki `.exe`'yi çalıştır

## Lokal build (opsiyonel)
.NET 8 SDK yüklüyse:

```bash
dotnet restore
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

Çıktı:
`bin\Release\net8.0-windows\win-x64\publish\HesapTakip.exe`
