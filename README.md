# RAY OFİS - Masa & Oda Rezervasyon Sistemi

**.NET Core Web API** ve **AngularJS (1.8.2)** kullanılarak geliştirilmiş, otobüs koltuk seçimi mantığında çalışan interaktif ofis masa ve toplantı odası rezervasyon uygulaması.

## Özellikler
- **Otobüs Koltuk Seçimi Tarzı Ofis Düzeni:** Her masa 2 kişilik (`1A-1B` ... `10A-10B`) olacak şekilde koridorlu oturma düzeni ve toplantı odaları (`ODA-1` .. `ODA-4`).
- **Renk Kodları:**
  - `#FF0025` (Ray Kırmızısı) — Dolu/Rezervasyonlu koltuklar, logo ve aksiyon butonları
  - `#0135EB` (Ray Mavisi) — Seçili koltuk, logo ve birincil rezervasyon butonları
- **Günlere Göre Listeleme:** 7 günlük hızlı takvim şeridi ve tarih seçici ile gün bazlı müsaitlik görüntüleme.
- **Kullanıcı & Admin Girişi:** Çalışan ve yönetici (Admin) rolleri.
- **Admin Dashboard:** Doluluk oranı istatistikleri ve tüm rezervasyonları görüntüleme/iptal etme paneli.

## Çalıştırma
```bash
dotnet run --urls "http://localhost:5055"
```

### Demo Giriş Bilgileri
- **Admin:** Kullanıcı Adı: `admin` | Şifre: `123`
- **Çalışanlar:** `ahmet`, `zeynep`, `murat`, `elif`, `can` | Şifre: `123`
