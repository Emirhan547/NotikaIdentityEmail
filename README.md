# Notika Identity Email

Notika Identity Email, ASP.NET Core MVC tabanlı, **kurumsal mesajlaşma ve bildirim** senaryolarını hedefleyen backend ağırlıklı bir örnek uygulamadır.  
Proje; **Identity ile kullanıcı/rol yönetimi**, **mesajlaşma altyapısı**, **yorum & moderasyon sistemi**, **SignalR ile gerçek zamanlı bildirimler** ve **Elasticsearch tabanlı merkezi loglama** yapısını tek bir mimari altında toplar.

Bu repository, **katmanlı mimari**, **servis odaklı tasarım** ve **gerçek hayatta karşılaşılan backend problemlerini** modellemek amacıyla geliştirilmiştir.

---

## Öne Çıkanlar

- ASP.NET Core MVC + Identity tabanlı kullanıcı ve rol yönetimi  
- Cookie Authentication ve JWT desteği  
- Gelen / giden mesaj kutuları  
- Okundu / okunmadı mesaj durum takibi  
- Kategori bazlı mesajlaşma yapısı  
- Kullanıcı yorumları ve moderasyon altyapısı  
- SignalR ile gerçek zamanlı bildirim sistemi  
- Elasticsearch + Kibana ile merkezi loglama  
- Genişletilebilir servis ve loglama mimarisi  

---

## Kullanılan Teknolojiler

- .NET 9  
- ASP.NET Core MVC  
- Entity Framework Core  
- ASP.NET Core Identity  
- SignalR  
- Elasticsearch / Kibana  
- SQL Server  

---

## Mimari Yaklaşım

- Katmanlı mimari (Controller → Service → Data / Context)  
- Identity ile entegre tek DbContext yapısı  
- Servis bazlı iş mantığı ayrımı  
- Loglama için özel `ILoggerProvider` implementasyonu  
- Gerçek zamanlı işlemler için SignalR hub yapısı  

---

## Projeye Dair Görseller

<img width="1893" height="862" alt="Ekran görüntüsü 2026-02-08 020026" src="https://github.com/user-attachments/assets/b72b2ac3-4362-447a-b0d1-3bee71f01265" />

<img width="1887" height="865" alt="Ekran görüntüsü 2026-02-08 020414" src="https://github.com/user-attachments/assets/24e94677-ab4b-49dd-b811-fd4427b9f7fb" />

<img width="1114" height="856" alt="Ekran görüntüsü 2026-02-08 034942" src="https://github.com/user-attachments/assets/f365d9ff-8981-47b2-89c3-7df14bde2906" />

<img width="1897" height="860" alt="Ekran görüntüsü 2026-02-08 040017" src="https://github.com/user-attachments/assets/49f4df6c-953a-4b30-bb1e-c586d8560fff" />


