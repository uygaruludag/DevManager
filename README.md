<p align="center">
  <img src="docs/screenshot.png" alt="DevManager Screenshot" width="800"/>
</p>

<h1 align="center">DevManager</h1>

<p align="center">
  <b>Birden fazla geliştirme sürecini tek bir arayüzden yönetin.</b><br/>
  .NET 8 | WPF | Material Design 3
</p>

<p align="center">
  <a href="#ozellikler">Özellikler</a> •
  <a href="#kurulum">Kurulum</a> •
  <a href="#kullanim">Kullanım</a> •
  <a href="#mimari">Mimari</a> •
  <a href="#yapilandirma">Yapılandırma</a> •
  <a href="#lisans">Lisans</a>
</p>

---

## Nedir?

**DevManager**, birden fazla mikroservis, API, frontend uygulaması veya arka plan hizmeti çalıştıran geliştiriciler için tasarlanmış bir Windows masaüstü uygulamasıdır. Tüm süreçlerinizi proje bazında organize edip, tek bir panelden başlatabilir, durdurabilir, yeniden başlatabilir ve canlı loglarını izleyebilirsiniz.

---

<a id="ozellikler"></a>
## Özellikler

### Süreç Yönetimi
- **Toplu Başlat / Durdur** - Tüm süreçleri veya proje bazında tek tıkla kontrol edin
- **Graceful Shutdown** - Ctrl+C sinyali ile düzgün kapanma, gerekirse force kill
- **Otomatik Yeniden Başlatma** - Crash olan süreçleri yapılandırılabilir politikalarla otomatik yeniden başlatma
- **Orphan Process Algılama** - Önceki oturumdan kalan çalışan süreçleri otomatik tespit edip sahiplenme

### Proje Organizasyonu
- **Proje Grupları** - Süreçlerinizi projelere göre organize edin (renk kodlu)
- **Otomatik Proje Tarama** - Klasör yolundan .csproj ve package.json dosyalarını otomatik tespit
- **Framework Algılama** - React, Vue, Angular, Next.js, Nuxt vb. framework'leri otomatik tanıma

### İzleme
- **Canlı Log Akışı** - stdout/stderr çıktılarını gerçek zamanlı izleme
- **Sağlık Kontrolleri** - HTTP endpoint veya TCP port üzerinden periyodik sağlık kontrolü
- **Süreç Durumu** - PID, uptime, restart sayısı gibi bilgileri anlık görüntüleme

### Arayüz
- **Material Design 3** - Modern, koyu tema tasarım
- **Performanslı Log Görüntüleme** - Sanallaştırılmış liste, toplu güncelleme (100ms batch)
- **Dairesel Log Tamponu** - Süreç başına 5000 satır sınırı ile bellek koruması

---

<a id="kurulum"></a>
## Kurulum

### Gereksinimler
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Derleme ve Çalıştırma

```bash
# Repoyu klonlayın
git clone https://github.com/user/DevManager.git
cd DevManager

# Derleyin
dotnet build DevManager.sln

# Çalıştırın
dotnet run --project src/DevManager.App/DevManager.App.csproj
```

### Release Build

```bash
dotnet publish src/DevManager.App/DevManager.App.csproj -c Release -o ./publish
```

---

<a id="kullanim"></a>
## Kullanım

### Proje Ekleme

1. Sol alt köşedeki **"+ Proje Ekle"** butonuna tıklayın
2. Proje klasör yolunu girin ve **"Tara"** butonuna basın
3. Otomatik tespit edilen süreçlerden istediklerinizi seçin
4. Proje adı, renk ve otomatik başlatma ayarlarını yapın
5. **"Projeyi Ekle"** ile kaydedin

### Süreç Kontrolü

| İşlem | Açıklama |
|-------|----------|
| ▶ Play | Süreci başlat |
| ■ Stop | Süreci durdur (graceful → force) |
| ↻ Restart | Süreci yeniden başlat |
| 🗑 Clear | Log ekranını temizle |
| 📋 Copy | Logları panoya kopyala |

### Toplu İşlemler
- **Üst araç çubuğu**: Tüm projelerdeki tüm süreçleri başlat/durdur
- **Proje başlığı**: Seçili projedeki tüm süreçleri başlat/durdur/yeniden başlat

---

<a id="mimari"></a>
## Mimari

```
DevManager.sln
├── src/
│   ├── DevManager.Core/           # İş mantığı katmanı
│   │   ├── Models/                # Veri modelleri
│   │   │   ├── DevManagerConfig   # Ana yapılandırma
│   │   │   ├── Project            # Proje tanımı
│   │   │   ├── ProcessDefinition  # Süreç tanımı
│   │   │   ├── ProcessInstance    # Çalışma zamanı durumu
│   │   │   ├── HealthCheckConfig  # Sağlık kontrolü ayarları
│   │   │   └── LogEntry           # Log kaydı
│   │   └── Services/              # Servisler
│   │       ├── ProcessManagerService    # Süreç yaşam döngüsü
│   │       ├── LogService               # Dairesel log tamponu
│   │       ├── ConfigurationService     # JSON yapılandırma
│   │       ├── HealthCheckService       # HTTP/TCP sağlık kontrolü
│   │       └── ProjectScanner           # Otomatik proje tarama
│   │
│   ├── DevManager.App/            # WPF Arayüz katmanı
│   │   ├── ViewModels/            # MVVM ViewModel'ler
│   │   ├── Views/                 # XAML görünümler
│   │   └── Resources/             # Dönüştürücüler, stiller
│   │
│   └── DevManager.Infrastructure/ # Platform bağımlı katman
│       ├── SystemTray              # Sistem tepsisi
│       └── JsonConfigStore         # JSON depolama
```

### Teknoloji Yığını

| Teknoloji | Kullanım |
|-----------|----------|
| .NET 8 (WPF) | Masaüstü uygulama framework'ü |
| MaterialDesignThemes 5.1 | Material Design 3 UI |
| CommunityToolkit.Mvvm 8.4 | MVVM altyapısı |
| Microsoft.Extensions.DI | Bağımlılık enjeksiyonu |
| System.Management (WMI) | Orphan process tespiti |
| Hardcodet.NotifyIcon.Wpf | Sistem tepsisi (planlanan) |

### Tasarım Kararları

- **Graceful Shutdown**: Önce stdin üzerinden Ctrl+C, başarısızsa force kill
- **Circular Buffer**: Süreç başına sınırlı log (varsayılan 5000 satır) ile bellek kontrolü
- **Batched UI Updates**: 100ms aralıklarla toplu log güncellemesi, UI donmasını önleme
- **ConcurrentDictionary**: Thread-safe süreç yönetimi
- **Event-Driven**: Durum değişiklikleri olaylarla UI'a iletilir
- **Atomic File Writes**: Yapılandırma temp dosyasına yazılıp taşınır

---

<a id="yapilandirma"></a>
## Yapılandırma

Yapılandırma dosyası `%APPDATA%\DevManager\devmanager-config.json` konumunda saklanır.

### Süreç Ayarları

```json
{
  "name": "API Backend",
  "command": "dotnet",
  "arguments": "run --project ./src/Api.csproj",
  "workingDirectory": "D:\\source\\project",
  "autoRestartOnCrash": true,
  "maxRestartAttempts": 3,
  "restartDelaySeconds": 5,
  "healthCheck": {
    "type": "httpEndpoint",
    "url": "http://localhost:5000/health",
    "intervalSeconds": 30,
    "timeoutSeconds": 5,
    "unhealthyThreshold": 3
  }
}
```

### Uygulama Ayarları

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `maxLogLinesPerProcess` | 5000 | Süreç başına maksimum log satırı |
| `theme` | Dark | Tema (Dark / Light) |
| `confirmBeforeStopAll` | true | Toplu durdurmada onay iste |
| `minimizeToTrayOnClose` | true | Kapatma yerine tepside küçült |
| `startMinimized` | false | Küçültülmüş başlat |

---

## Yol Haritası

- [x] Süreç yönetimi (başlat/durdur/yeniden başlat)
- [x] Proje organizasyonu
- [x] Canlı log izleme
- [x] Otomatik yeniden başlatma
- [x] Orphan process algılama
- [x] Otomatik proje tarama
- [ ] Sistem tepsisi entegrasyonu
- [ ] Ayarlar arayüzü
- [ ] Yapılandırma dışa/içe aktarma
- [ ] Tek örnek (single-instance) kontrolü

---

<a id="lisans"></a>
## Lisans

Bu proje [Istech Yazılım ve Danışmanlık](https://www.istechlabs.com) tarafından geliştirilmektedir.

📧 info@istechlabs.com
🌐 [www.istechlabs.com](https://www.istechlabs.com)
