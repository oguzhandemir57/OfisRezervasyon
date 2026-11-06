using System.Text.Json;
using RayOfisRezervasyon.Models;

namespace RayOfisRezervasyon.Services;

public class StoreData
{
    public List<User> Users { get; set; } = new();
    public List<WorkspaceItem> Workspaces { get; set; } = new();
    public List<Reservation> Reservations { get; set; } = new();
}

public class ReservationStore
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private StoreData _data = new();

    public ReservationStore(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "store_v3.json");
        LoadOrSeed();
    }

    private void LoadOrSeed()
    {
        lock (_lock)
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var loaded = JsonSerializer.Deserialize<StoreData>(json);
                    if (loaded != null && loaded.Users.Count > 0 && loaded.Workspaces.Count > 0)
                    {
                        _data = loaded;
                        return;
                    }
                }
                catch
                {
                    // Fallback to seed
                }
            }

            SeedDefaultData();
            Save();
        }
    }

    private void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_data, options);
        File.WriteAllText(_filePath, json);
    }

    private void SeedDefaultData()
    {
        _data.Users = new List<User>
        {
            new() { Id = 1, Username = "admin", Password = "123", FullName = "Fatih Yayla", Department = "Yönetim", Title = "Sistem Yöneticisi", Role = "Admin", AvatarInitials = "FY" },
            new() { Id = 2, Username = "ahmet", Password = "123", FullName = "Ahmet Yılmaz", Department = "Yazılım Geliştirme", Title = "Kıdemli .NET Geliştirici", Role = "Employee", AvatarInitials = "AY" },
            new() { Id = 3, Username = "zeynep", Password = "123", FullName = "Zeynep Kaya", Department = "Ürün & Tasarım", Title = "UI/UX Tasarımcısı", Role = "Employee", AvatarInitials = "ZK" },
            new() { Id = 4, Username = "murat", Password = "123", FullName = "Murat Demir", Department = "Veri Analitiği", Title = "Veri Bilimci", Role = "Employee", AvatarInitials = "MD" },
            new() { Id = 5, Username = "elif", Password = "123", FullName = "Elif Şahin", Department = "İnsan Kaynakları", Title = "İK Uzmanı", Role = "Employee", AvatarInitials = "EŞ" },
            new() { Id = 6, Username = "can", Password = "123", FullName = "Caner Özkan", Department = "Pazarlama", Title = "Dijital Pazarlama Uzmanı", Role = "Employee", AvatarInitials = "CÖ" }
        };

        var workspaces = new List<WorkspaceItem>();
        int idCounter = 1;

        // 10 Adet 2 Kişilik Masa (Her masada A ve B koltuğu - toplam 20 koltuk)
        for (int deskNo = 1; deskNo <= 10; deskNo++)
        {
            string zoneName = $"Masa {deskNo}";
            int row = (deskNo - 1) / 2 + 1;
            int side = (deskNo % 2 == 1) ? 1 : 2;

            workspaces.Add(new WorkspaceItem
            {
                Id = idCounter++,
                Code = $"{deskNo}A",
                Name = $"Masa {deskNo} - Koltuk {deskNo}A",
                Type = "Masa",
                Zone = zoneName,
                Capacity = 1,
                Features = new() { "Monitör", "Priz" },
                GridX = side,
                GridY = row,
                GridW = deskNo,
                GridH = 1
            });

            workspaces.Add(new WorkspaceItem
            {
                Id = idCounter++,
                Code = $"{deskNo}B",
                Name = $"Masa {deskNo} - Koltuk {deskNo}B",
                Type = "Masa",
                Zone = zoneName,
                Capacity = 1,
                Features = new() { "Monitör", "Priz" },
                GridX = side,
                GridY = row,
                GridW = deskNo,
                GridH = 2
            });
        }

        // 4 Adet Toplantı Odası
        workspaces.Add(new WorkspaceItem { Id = idCounter++, Code = "ODA-1", Name = "Toplantı Odası 1", Type = "Oda", Zone = "Toplantı Odaları", Capacity = 6, Features = new() { "TV Ekran", "Beyaz Tahta" }, GridX = 1, GridY = 6 });
        workspaces.Add(new WorkspaceItem { Id = idCounter++, Code = "ODA-2", Name = "Toplantı Odası 2", Type = "Oda", Zone = "Toplantı Odaları", Capacity = 8, Features = new() { "Projeksiyon", "Video Konferans" }, GridX = 2, GridY = 6 });
        workspaces.Add(new WorkspaceItem { Id = idCounter++, Code = "ODA-3", Name = "Toplantı Odası 3", Type = "Oda", Zone = "Toplantı Odaları", Capacity = 4, Features = new() { "Akıllı Ekran" }, GridX = 3, GridY = 6 });
        workspaces.Add(new WorkspaceItem { Id = idCounter++, Code = "ODA-4", Name = "Büyük Konferans Odası", Type = "Oda", Zone = "Toplantı Odaları", Capacity = 12, Features = new() { "Ses Sistemi", "75\" Ekran" }, GridX = 4, GridY = 6 });

        _data.Workspaces = workspaces;

        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

        _data.Reservations = new List<Reservation>
        {
            // Bugün ofise gelip Check-In yapmış (doğrulanmış) çalışanlar + henüz yolda olanlar
            new() { Id = 1, WorkspaceItemId = 1, UserId = 2, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Sprint geliştirme", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-3) }, // Ahmet - CheckedIn
            new() { Id = 2, WorkspaceItemId = 4, UserId = 3, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "UI/UX Tasarım", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-2) },     // Zeynep - CheckedIn
            new() { Id = 3, WorkspaceItemId = 5, UserId = 4, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Veri Raporlama", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-2) },   // Murat - CheckedIn
            new() { Id = 4, WorkspaceItemId = 6, UserId = 5, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Mülakatlar", Status = "Active", CreatedAt = DateTime.Now.AddHours(-1) },          // Elif - Henüz Check-In yapmadı
            new() { Id = 5, WorkspaceItemId = 11, UserId = 6, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Kampanya Planı", Status = "CheckedIn", CreatedAt = DateTime.Now.AddMinutes(-45) },// Caner - CheckedIn
            new() { Id = 6, WorkspaceItemId = 21, UserId = 1, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Yönetim Toplantısı", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-4) },

            // Yarın için örnek rezervasyonlar
            new() { Id = 7, WorkspaceItemId = 2, UserId = 2, Date = tomorrow, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Ofis çalışması", Status = "Active", CreatedAt = DateTime.Now.AddMinutes(-30) },
            new() { Id = 8, WorkspaceItemId = 22, UserId = 3, Date = tomorrow, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Tasarım Sunumu", Status = "Active", CreatedAt = DateTime.Now.AddMinutes(-20) }
        };
    }

    public User? Authenticate(string username, string password)
    {
        lock (_lock)
        {
            return _data.Users.FirstOrDefault(u =>
                u.Username.Equals(username?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);
        }
    }

    public List<User> GetUsers()
    {
        lock (_lock)
        {
            return _data.Users.Select(u => new User
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Department = u.Department,
                Title = u.Title,
                Role = u.Role,
                AvatarInitials = u.AvatarInitials
            }).ToList();
        }
    }

    public object GetWorkspacesForDate(string date)
    {
        lock (_lock)
        {
            var dayReservations = _data.Reservations
                .Where(r => r.Date == date && r.Status != "Cancelled")
                .ToList();

            var items = _data.Workspaces.Select(w =>
            {
                var res = dayReservations.FirstOrDefault(r => r.WorkspaceItemId == w.Id);
                var user = res != null ? _data.Users.FirstOrDefault(u => u.Id == res.UserId) : null;

                return new
                {
                    w.Id,
                    w.Code,
                    w.Name,
                    w.Type,
                    w.Zone,
                    w.Capacity,
                    w.Features,
                    w.GridX,
                    w.GridY,
                    w.GridW,
                    w.GridH,
                    w.IsActive,
                    IsReserved = res != null,
                    Reservation = res == null ? null : new
                    {
                        res.Id,
                        res.UserId,
                        UserFullName = user?.FullName ?? "Dolu",
                        UserDepartment = user?.Department ?? "",
                        UserTitle = user?.Title ?? "",
                        UserInitials = user?.AvatarInitials ?? "D",
                        res.Date,
                        res.TimeSlot,
                        res.Note,
                        res.Status,
                        res.CreatedAt
                    }
                };
            }).ToList();

            var totalActive = items.Count(i => i.IsActive);
            var reservedCount = items.Count(i => i.IsActive && i.IsReserved);
            var availableCount = items.Count(i => i.IsActive && !i.IsReserved);
            var checkedInCount = dayReservations.Count(r => r.Status == "CheckedIn");

            return new
            {
                Date = date,
                Summary = new
                {
                    Total = items.Count,
                    ActiveTotal = totalActive,
                    Reserved = reservedCount,
                    Available = availableCount,
                    CheckedIn = checkedInCount,
                    OccupancyRate = totalActive > 0 ? Math.Round((double)reservedCount / totalActive * 100, 0) : 0
                },
                Items = items
            };
        }
    }

    public (bool Success, string Message, object? Data) CreateReservation(CreateReservationRequest req)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(req.Date))
                return (false, "Lütfen geçerli bir tarih seçiniz.", null);

            var workspace = _data.Workspaces.FirstOrDefault(w => w.Id == req.WorkspaceItemId);
            if (workspace == null)
                return (false, "Seçilen koltuk veya oda bulunamadı.", null);

            if (!workspace.IsActive)
                return (false, $"{workspace.Code} şu anda kullanım dışı.", null);

            var user = _data.Users.FirstOrDefault(u => u.Id == req.UserId);
            if (user == null)
                return (false, "Kullanıcı oturumu doğrulanamadı.", null);

            var existingOnWorkspace = _data.Reservations.FirstOrDefault(r =>
                r.WorkspaceItemId == req.WorkspaceItemId &&
                r.Date == req.Date &&
                r.Status != "Cancelled");

            if (existingOnWorkspace != null)
            {
                return (false, $"{workspace.Code} seçilen tarihte ({req.Date}) zaten dolu.", null);
            }

            var nextId = _data.Reservations.Any() ? _data.Reservations.Max(r => r.Id) + 1 : 1;
            var newRes = new Reservation
            {
                Id = nextId,
                WorkspaceItemId = req.WorkspaceItemId,
                UserId = req.UserId,
                Date = req.Date,
                TimeSlot = string.IsNullOrWhiteSpace(req.TimeSlot) ? "Tam Gün (09:00 - 18:00)" : req.TimeSlot,
                Note = string.IsNullOrWhiteSpace(req.Note) ? "Uzaktan / Hibrit Ofis Çalışması" : req.Note.Trim(),
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _data.Reservations.Add(newRes);
            Save();

            return (true, $"{workspace.Name} ayrıldı! Ofise geldiğinizde 'Check-In' sekmesinden doğrulama yapmayı unutmayın.", newRes);
        }
    }

    public (bool Success, string Message) CancelReservation(int reservationId, int requestingUserId)
    {
        lock (_lock)
        {
            var res = _data.Reservations.FirstOrDefault(r => r.Id == reservationId);
            if (res == null)
                return (false, "Rezervasyon kaydı bulunamadı.");

            var user = _data.Users.FirstOrDefault(u => u.Id == requestingUserId);
            if (user == null)
                return (false, "Geçersiz kullanıcı.");

            if (res.UserId != requestingUserId && user.Role != "Admin")
                return (false, "Yalnızca kendi rezervasyonunuzu iptal edebilirsiniz.");

            res.Status = "Cancelled";
            Save();
            return (true, "Rezervasyon iptal edildi, koltuk/oda tekrar boşa çıktı.");
        }
    }

    public (bool Success, string Message) CheckInReservation(int reservationId, int requestingUserId)
    {
        lock (_lock)
        {
            var res = _data.Reservations.FirstOrDefault(r => r.Id == reservationId);
            if (res == null || res.Status == "Cancelled")
                return (false, "Aktif rezervasyon bulunamadı.");

            res.Status = "CheckedIn";
            Save();
            return (true, "Ofis Check-In işleminiz başarıyla doğrulandı! Artık 'Ofistekiler' ekranında görünüyorsunuz.");
        }
    }

    public object GetUserReservations(int userId)
    {
        lock (_lock)
        {
            return _data.Reservations
                .Where(r => r.UserId == userId && r.Status != "Cancelled")
                .OrderBy(r => r.Date)
                .Select(r =>
                {
                    var ws = _data.Workspaces.FirstOrDefault(w => w.Id == r.WorkspaceItemId);
                    return new
                    {
                        r.Id,
                        r.WorkspaceItemId,
                        WorkspaceCode = ws?.Code ?? "",
                        WorkspaceName = ws?.Name ?? "",
                        WorkspaceType = ws?.Type ?? "",
                        WorkspaceZone = ws?.Zone ?? "",
                        r.Date,
                        r.TimeSlot,
                        r.Note,
                        r.Status,
                        r.CreatedAt
                    };
                })
                .ToList();
        }
    }

    public (bool Success, string Message) SaveWorkspace(SaveWorkspaceRequest req)
    {
        lock (_lock)
        {
            if (req.Id > 0)
            {
                var existing = _data.Workspaces.FirstOrDefault(w => w.Id == req.Id);
                if (existing == null)
                    return (false, "Koltuk/Oda bulunamadı.");

                existing.Code = req.Code;
                existing.Name = req.Name;
                existing.Type = req.Type;
                existing.Zone = req.Zone;
                existing.Capacity = req.Capacity;
                existing.IsActive = req.IsActive;
                Save();
                return (true, $"{existing.Code} güncellendi.");
            }
            else
            {
                var nextId = _data.Workspaces.Any() ? _data.Workspaces.Max(w => w.Id) + 1 : 1;
                var newWs = new WorkspaceItem
                {
                    Id = nextId,
                    Code = string.IsNullOrWhiteSpace(req.Code) ? $"ODA-{nextId}" : req.Code.Trim().ToUpper(),
                    Name = string.IsNullOrWhiteSpace(req.Name) ? $"Yeni Alan {nextId}" : req.Name.Trim(),
                    Type = req.Type,
                    Zone = req.Type == "Oda" ? "Toplantı Odaları" : req.Zone,
                    Capacity = req.Capacity > 0 ? req.Capacity : 1,
                    IsActive = req.IsActive
                };
                _data.Workspaces.Add(newWs);
                Save();
                return (true, $"{newWs.Code} eklendi.");
            }
        }
    }

    public object GetAdminDashboard(string date)
    {
        lock (_lock)
        {
            var activeWorkspaces = _data.Workspaces.Where(w => w.IsActive).ToList();
            var dayReservations = _data.Reservations.Where(r => r.Date == date && r.Status != "Cancelled").ToList();

            var totalDesks = _data.Workspaces.Count(w => w.Type == "Masa");
            var totalRooms = _data.Workspaces.Count(w => w.Type == "Oda");

            var reservedDesks = dayReservations.Count(r => _data.Workspaces.FirstOrDefault(w => w.Id == r.WorkspaceItemId)?.Type == "Masa");
            var reservedRooms = dayReservations.Count(r => _data.Workspaces.FirstOrDefault(w => w.Id == r.WorkspaceItemId)?.Type == "Oda");
            var checkedInCount = dayReservations.Count(r => r.Status == "CheckedIn");

            var detailedReservations = _data.Reservations
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.CreatedAt)
                .Select(r =>
                {
                    var u = _data.Users.FirstOrDefault(x => x.Id == r.UserId);
                    var w = _data.Workspaces.FirstOrDefault(x => x.Id == r.WorkspaceItemId);
                    return new
                    {
                        r.Id,
                        r.Date,
                        r.TimeSlot,
                        r.Note,
                        r.Status,
                        r.CreatedAt,
                        UserId = r.UserId,
                        UserFullName = u?.FullName ?? "Bilinmeyen",
                        UserDepartment = u?.Department ?? "-",
                        UserInitials = u?.AvatarInitials ?? "??",
                        WorkspaceId = r.WorkspaceItemId,
                        WorkspaceCode = w?.Code ?? "-",
                        WorkspaceName = w?.Name ?? "-",
                        WorkspaceType = w?.Type ?? "-",
                        WorkspaceZone = w?.Zone ?? "-"
                    };
                }).ToList();

            return new
            {
                SelectedDate = date,
                Kpis = new
                {
                    TotalWorkspaces = _data.Workspaces.Count,
                    ActiveWorkspaces = activeWorkspaces.Count,
                    TotalDesks = totalDesks,
                    TotalRooms = totalRooms,
                    ReservedToday = dayReservations.Count,
                    AvailableToday = Math.Max(0, activeWorkspaces.Count - dayReservations.Count),
                    ReservedDesks = reservedDesks,
                    ReservedRooms = reservedRooms,
                    CheckedInCount = checkedInCount,
                    OccupancyRate = activeWorkspaces.Count > 0 ? Math.Round((double)dayReservations.Count / activeWorkspaces.Count * 100, 0) : 0
                },
                AllReservations = detailedReservations
            };
        }
    }
}
