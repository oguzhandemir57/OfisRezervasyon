using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RayOfisRezervasyon.Models;

namespace RayOfisRezervasyon.Services;

public class StoreData
{
    public List<User> Users { get; set; } = new();
    public List<OfficeFloor> Floors { get; set; } = new();
    public List<WorkspaceItem> Workspaces { get; set; } = new();
    public List<Reservation> Reservations { get; set; } = new();
}

public class ReservationStore
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private StoreData _data = new();
    private static readonly char[] SeatLetters = new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };

    public ReservationStore(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "store_v5.json");
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
                    if (loaded != null && loaded.Users.Count > 0 && loaded.Workspaces.Count > 0 && loaded.Floors.Count > 0)
                    {
                        _data = loaded;
                        bool migratedCoords = false;
                        foreach (var w in _data.Workspaces.Where(x => x.Type == "Masa"))
                        {
                            if (w.CanvasX < 0 || w.CanvasY < 0)
                            {
                                int row = w.GridY > 0 ? w.GridY : ((w.GridW - 1) / 2 + 1);
                                int side = w.GridX == 2 ? 2 : 1;
                                w.CanvasX = side == 1 ? 6 : 54;
                                w.CanvasY = Math.Min(78, 6 + (row - 1) * 18);
                                migratedCoords = true;
                            }
                        }
                        var todayIso = DateTime.Today.ToString("yyyy-MM-dd");
                        if (!_data.Reservations.Any(r => r.Date == todayIso && r.Status == "CheckedIn"))
                        {
                            var floor2Seat = _data.Workspaces.FirstOrDefault(w => w.FloorId == 2 && w.Type == "Masa" && w.IsActive);
                            int f2Id = floor2Seat?.Id ?? 25;
                            int nextResId = _data.Reservations.Any() ? _data.Reservations.Max(r => r.Id) + 1 : 1;
                            _data.Reservations.Add(new Reservation { Id = nextResId++, WorkspaceItemId = 1, UserId = 2, Date = todayIso, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Sprint geliştirme", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-2) });
                            _data.Reservations.Add(new Reservation { Id = nextResId++, WorkspaceItemId = f2Id, UserId = 3, Date = todayIso, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "2. Kat UI/UX Tasarım", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-2) });
                            _data.Reservations.Add(new Reservation { Id = nextResId++, WorkspaceItemId = 5, UserId = 4, Date = todayIso, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Veri Raporlama", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-1) });
                            _data.Reservations.Add(new Reservation { Id = nextResId++, WorkspaceItemId = 11, UserId = 6, Date = todayIso, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Kampanya Planı", Status = "CheckedIn", CreatedAt = DateTime.Now.AddMinutes(-45) });
                            migratedCoords = true;
                        }
                        if (migratedCoords)
                        {
                            Save();
                        }
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

    private static List<string> GetDeskFeatures(int deskNo, bool isWindowSide)
    {
        var list = new List<string>();
        if (isWindowSide)
            list.Add("Cam Kenarı Masa (Gün Işığı)");
        else
            list.Add("Koridor Yanı (Kolay Erişim)");

        if (deskNo <= 4)
            list.Add("Çift Monitör (2x27\" IPS)");
        else if (deskNo <= 7)
            list.Add("34\" UltraWide Kavisli Monitör");
        else
            list.Add("27\" 4K Monitör + Laptop Standı");

        if (deskNo % 2 == 1)
            list.Add("Yükseklik Ayarlanabilen Motorlu Masa");
        else
            list.Add("Geniş Ahşap Çalışma Yüzeyi");

        list.Add("Type-C Hızlı Şarj Dock & Ethernet");
        list.Add("Ergonomik Bel Destekli Koltuk");
        return list;
    }

    private static string BuildSeatCode(int floorId, int deskNo, int seatIdx)
    {
        char letter = SeatLetters[Math.Clamp(seatIdx - 1, 0, SeatLetters.Length - 1)];
        return floorId == 1 ? $"{deskNo}{letter}" : $"K{floorId}-{deskNo}{letter}";
    }

    private static string BuildSeatName(int floorId, int deskNo, int seatIdx, string code)
    {
        string prefix = floorId == 1 ? "" : $"Kat {floorId} • ";
        return $"{prefix}Masa {deskNo} - {seatIdx}. Koltuk ({code})";
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

        _data.Floors = new List<OfficeFloor>
        {
            new() { Id = 1, Code = "KAT-1", Name = "1. Kat — A Blok Açık Ofis", Description = "Panoramik Cam Cephe & Ana Çalışma Alanı", IsActive = true },
            new() { Id = 2, Code = "KAT-2", Name = "2. Kat — B Blok Ar-Ge & Tasarım", Description = "Çoklu Takım Masaları (3-4 Kişilik) & Dikine/Yatay Hibrit Düzen", IsActive = true },
            new() { Id = 3, Code = "KAT-3", Name = "3. Kat — VIP & Proje Katı", Description = "Özel Proje Takımları Katı (Admin Tarafından Açılıp Kapatılabilir)", IsActive = false }
        };

        var workspaces = new List<WorkspaceItem>();
        int idCounter = 1;

        // FLOOR 1: 10 Desks (2 seats each by default, horizontal) + 4 Meeting Rooms
        for (int deskNo = 1; deskNo <= 10; deskNo++)
        {
            string zoneName = $"Masa {deskNo}";
            int row = (deskNo - 1) / 2 + 1;
            int side = (deskNo % 2 == 1) ? 1 : 2;

            for (int s = 1; s <= 2; s++)
            {
                string code = BuildSeatCode(1, deskNo, s);
                bool isWin = (side == 1 && s == 1) || (side == 2 && s == 2);
                workspaces.Add(new WorkspaceItem
                {
                    Id = idCounter++,
                    FloorId = 1,
                    Code = code,
                    Name = BuildSeatName(1, deskNo, s, code),
                    Type = "Masa",
                    Zone = zoneName,
                    Capacity = 1,
                    Orientation = "horizontal",
                    Features = GetDeskFeatures(deskNo, isWin),
                    GridX = side,
                    GridY = row,
                    GridW = deskNo,
                    GridH = s,
                    CanvasX = side == 1 ? 6 : 54,
                    CanvasY = Math.Min(78, 6 + (row - 1) * 18),
                    IsActive = true
                });
            }
        }

        workspaces.Add(new WorkspaceItem
        {
            Id = idCounter++,
            FloorId = 1,
            Code = "ODA-1",
            Name = "Toplantı Odası 1 (Sprint)",
            Type = "Oda",
            Zone = "Toplantı Odaları",
            Capacity = 6,
            Orientation = "horizontal",
            Features = new() { "65\" 4K Akıllı Ekran", "Cam Yazı Tahtası", "Ses Yalıtımlı Kabin", "Kablosuz HDMI Sunum" },
            GridX = 1,
            GridY = 6
        });
        workspaces.Add(new WorkspaceItem
        {
            Id = idCounter++,
            FloorId = 1,
            Code = "ODA-2",
            Name = "Toplantı Odası 2 (İnovasyon)",
            Type = "Oda",
            Zone = "Toplantı Odaları",
            Capacity = 8,
            Orientation = "horizontal",
            Features = new() { "Polycom Video Konferans", "Çift Projeksiyon", "Cam Kenarı Manzaralı", "Akustik Panel" },
            GridX = 2,
            GridY = 6
        });
        workspaces.Add(new WorkspaceItem
        {
            Id = idCounter++,
            FloorId = 1,
            Code = "ODA-3",
            Name = "Toplantı Odası 3 (Hızlı Görüşme)",
            Type = "Oda",
            Zone = "Toplantı Odaları",
            Capacity = 4,
            Orientation = "horizontal",
            Features = new() { "55\" LED TV", "Yükseklik Ayarlanabilir Masa", "Hızlı Şarj İstasyonu" },
            GridX = 3,
            GridY = 6
        });
        workspaces.Add(new WorkspaceItem
        {
            Id = idCounter++,
            FloorId = 1,
            Code = "ODA-4",
            Name = "Büyük Konferans Odası",
            Type = "Oda",
            Zone = "Toplantı Odaları",
            Capacity = 12,
            Orientation = "horizontal",
            Features = new() { "85\" İnteraktif Ekran", "360° Mikrofon & Ses Sistemi", "Panoramik Cam Cephe", "İkram & Kahve Köşesi" },
            GridX = 4,
            GridY = 6
        });

        // FLOOR 2: Showcase 3-person, 4-person, and Vertical ("vertical") desks!
        var floor2Configs = new[]
        {
            new { DeskNo = 1, Seats = 3, Orientation = "horizontal", Side = 1, Row = 1 },
            new { DeskNo = 2, Seats = 3, Orientation = "vertical",   Side = 2, Row = 1 },
            new { DeskNo = 3, Seats = 4, Orientation = "horizontal", Side = 1, Row = 2 },
            new { DeskNo = 4, Seats = 2, Orientation = "vertical",   Side = 2, Row = 2 }
        };
        foreach (var cfg in floor2Configs)
        {
            for (int s = 1; s <= cfg.Seats; s++)
            {
                string code = BuildSeatCode(2, cfg.DeskNo, s);
                workspaces.Add(new WorkspaceItem
                {
                    Id = idCounter++,
                    FloorId = 2,
                    Code = code,
                    Name = BuildSeatName(2, cfg.DeskNo, s, code),
                    Type = "Masa",
                    Zone = $"Masa {cfg.DeskNo}",
                    Capacity = 1,
                    Orientation = cfg.Orientation,
                    Features = GetDeskFeatures(cfg.DeskNo, s == 1),
                    GridX = cfg.Side,
                    GridY = cfg.Row,
                    GridW = cfg.DeskNo,
                    GridH = s,
                    CanvasX = cfg.Side == 1 ? 8 : 54,
                    CanvasY = cfg.Row == 1 ? 10 : 48,
                    IsActive = true
                });
            }
        }
        workspaces.Add(new WorkspaceItem
        {
            Id = idCounter++,
            FloorId = 2,
            Code = "ODA-5",
            Name = "Ar-Ge Beyin Fırtınası Odası",
            Type = "Oda",
            Zone = "Toplantı Odaları",
            Capacity = 10,
            Orientation = "horizontal",
            Features = new() { "75\" Akıllı Tahta", "Çift Monitör Sunum Masası", "Ses Yalıtımlı Akustik Duvar" },
            GridX = 1,
            GridY = 3
        });

        // FLOOR 3: Initially Closed (IsActive = false on Floor 3) with 2 3-person desks
        for (int deskNo = 1; deskNo <= 2; deskNo++)
        {
            for (int s = 1; s <= 3; s++)
            {
                string code = BuildSeatCode(3, deskNo, s);
                workspaces.Add(new WorkspaceItem
                {
                    Id = idCounter++,
                    FloorId = 3,
                    Code = code,
                    Name = BuildSeatName(3, deskNo, s, code),
                    Type = "Masa",
                    Zone = $"Masa {deskNo}",
                    Capacity = 1,
                    Orientation = deskNo == 1 ? "horizontal" : "vertical",
                    Features = GetDeskFeatures(deskNo, s == 1),
                    GridX = deskNo,
                    GridY = 1,
                    GridW = deskNo,
                    GridH = s,
                    CanvasX = deskNo == 1 ? 10 : 55,
                    CanvasY = 18,
                    IsActive = true
                });
            }
        }

        _data.Workspaces = workspaces;

        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

        _data.Reservations = new List<Reservation>
        {
            new() { Id = 1, WorkspaceItemId = 1, UserId = 2, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Sprint geliştirme", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-3) },
            new() { Id = 2, WorkspaceItemId = 4, UserId = 3, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "UI/UX Tasarım", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-2) },
            new() { Id = 3, WorkspaceItemId = 5, UserId = 4, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Veri Raporlama", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-2) },
            new() { Id = 4, WorkspaceItemId = 6, UserId = 5, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Mülakatlar", Status = "Active", CreatedAt = DateTime.Now.AddHours(-1) },
            new() { Id = 5, WorkspaceItemId = 11, UserId = 6, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Kampanya Planı", Status = "CheckedIn", CreatedAt = DateTime.Now.AddMinutes(-45) },
            new() { Id = 6, WorkspaceItemId = 21, UserId = 1, Date = today, TimeSlot = "Tam Gün (09:00 - 18:00)", Note = "Yönetim Toplantısı", Status = "CheckedIn", CreatedAt = DateTime.Now.AddHours(-4) },
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

    public object GetFloors()
    {
        lock (_lock)
        {
            return _data.Floors.Select(f =>
            {
                var floorWs = _data.Workspaces.Where(w => w.FloorId == f.Id && w.IsActive).ToList();
                var deskCount = floorWs.Where(w => w.Type == "Masa").Select(w => w.GridW).Distinct().Count();
                var seatCount = floorWs.Count(w => w.Type == "Masa");
                var roomCount = floorWs.Count(w => w.Type == "Oda");
                return new
                {
                    f.Id,
                    f.Code,
                    f.Name,
                    f.Description,
                    f.IsActive,
                    DeskCount = deskCount,
                    SeatCount = seatCount,
                    RoomCount = roomCount
                };
            }).ToList();
        }
    }

    public object GetWorkspacesForDate(string date, int? floorId = null)
    {
        lock (_lock)
        {
            int targetFloorId = floorId ?? (_data.Floors.FirstOrDefault(f => f.IsActive)?.Id ?? 1);
            var selectedFloor = _data.Floors.FirstOrDefault(f => f.Id == targetFloorId) ?? _data.Floors.First();

            var dayReservations = _data.Reservations
                .Where(r => r.Date == date && r.Status != "Cancelled")
                .ToList();

            var items = _data.Workspaces
                .Where(w => w.IsActive && w.FloorId == selectedFloor.Id)
                .OrderBy(w => w.GridW)
                .ThenBy(w => w.GridH)
                .Select(w =>
                {
                    var res = dayReservations.FirstOrDefault(r => r.WorkspaceItemId == w.Id);
                    var user = res != null ? _data.Users.FirstOrDefault(u => u.Id == res.UserId) : null;

                    return new
                    {
                        w.Id,
                        w.FloorId,
                        w.Code,
                        w.Name,
                        w.Type,
                        w.Zone,
                        w.Capacity,
                        w.Features,
                        w.Orientation,
                        w.GridX,
                        w.GridY,
                        w.GridW,
                        w.GridH,
                        w.CanvasX,
                        w.CanvasY,
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

            var allFloorsPresence = dayReservations
                .Select(r =>
                {
                    var ws = _data.Workspaces.FirstOrDefault(w => w.Id == r.WorkspaceItemId && w.IsActive);
                    if (ws == null) return null;
                    var fl = _data.Floors.FirstOrDefault(f => f.Id == ws.FloorId);
                    var usr = _data.Users.FirstOrDefault(u => u.Id == r.UserId);
                    var fullName = usr?.FullName ?? "Çalışan";
                    return new
                    {
                        FloorId = ws.FloorId,
                        FloorCode = fl?.Code ?? $"KAT-{ws.FloorId}",
                        FloorName = fl?.Name ?? $"{ws.FloorId}. Kat",
                        WorkspaceCode = ws.Code,
                        WorkspaceName = ws.Name,
                        WorkspaceType = ws.Type,
                        UserId = r.UserId,
                        FullName = fullName,
                        Department = usr?.Department ?? "Ofis",
                        Title = usr?.Title ?? "",
                        Initials = !string.IsNullOrWhiteSpace(usr?.AvatarInitials)
                            ? usr!.AvatarInitials
                            : (fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpperInvariant() : "Ç"),
                        TimeSlot = r.TimeSlot,
                        Status = r.Status
                    };
                })
                .Where(x => x != null)
                .ToList();

            var totalActive = items.Count(i => i.IsActive);
            var reservedCount = items.Count(i => i.IsActive && i.IsReserved);
            var availableCount = items.Count(i => i.IsActive && !i.IsReserved);
            var checkedInCount = allFloorsPresence.Count(p => p != null && p.Status == "CheckedIn");

            return new
            {
                Date = date,
                FloorId = selectedFloor.Id,
                Floor = selectedFloor,
                Floors = GetFloors(),
                AllFloorsPresence = allFloorsPresence,
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

    public object CreateReservation(CreateReservationRequest req)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(req.Date))
                return new { success = false, message = "Lütfen geçerli bir tarih seçiniz." };

            var workspace = _data.Workspaces.FirstOrDefault(w => w.Id == req.WorkspaceItemId);
            if (workspace == null)
                return new { success = false, message = "Seçilen koltuk veya oda bulunamadı." };

            if (!workspace.IsActive)
                return new { success = false, message = $"{workspace.Code} şu anda kullanım dışı." };

            var floor = _data.Floors.FirstOrDefault(f => f.Id == workspace.FloorId);
            if (floor != null && !floor.IsActive)
                return new { success = false, message = $"{floor.Name} şu anda yönetici tarafından kullanıma kapatılmıştır." };

            var user = _data.Users.FirstOrDefault(u => u.Id == req.UserId);
            if (user == null)
                return new { success = false, message = "Kullanıcı oturumu doğrulanamadı." };

            var existingOnWorkspace = _data.Reservations.FirstOrDefault(r =>
                r.WorkspaceItemId == req.WorkspaceItemId &&
                r.Date == req.Date &&
                r.Status != "Cancelled");

            if (existingOnWorkspace != null)
            {
                return new { success = false, message = $"{workspace.Code} seçilen tarihte ({req.Date}) zaten dolu." };
            }

            // RULE: 1 user can reserve at most 1 item of the same type ("Masa" or "Oda") per day.
            // If they already have one on that day, ask to swap, and when confirmed, automatically cancel ALL previous active ones of that type on that day!
            string? replacedOldCode = null;
            var userSameDayItems = _data.Reservations
                .Where(r => r.UserId == req.UserId && r.Date == req.Date && r.Status != "Cancelled")
                .Select(r => new { Res = r, Ws = _data.Workspaces.FirstOrDefault(w => w.Id == r.WorkspaceItemId) })
                .Where(x => x.Ws != null && x.Ws.Type == workspace.Type)
                .ToList();

            if (userSameDayItems.Count > 0)
            {
                var oldCodes = string.Join(", ", userSameDayItems.Select(x => x.Ws!.Code));
                string typeText = workspace.Type == "Oda" ? "toplantı odası" : "masa";

                if (!req.ReplaceExisting)
                {
                    return new
                    {
                        success = false,
                        requiresChangeConfirmation = true,
                        existingReservationId = userSameDayItems[0].Res.Id,
                        existingCode = oldCodes,
                        existingName = userSameDayItems[0].Ws!.Name,
                        newWorkspaceId = workspace.Id,
                        newCode = workspace.Code,
                        newName = workspace.Name,
                        itemType = workspace.Type,
                        date = req.Date,
                        timeSlot = req.TimeSlot,
                        message = $"{req.Date} tarihinde zaten {oldCodes} ({typeText}) rezervasyonunuz bulunuyor. Eski rezervasyonunuzu otomatik iptal edip {workspace.Code} ile değiştirmek ister misiniz?"
                    };
                }
                else
                {
                    replacedOldCode = oldCodes;
                    foreach (var oldItem in userSameDayItems)
                    {
                        oldItem.Res.Status = "Cancelled";
                    }
                }
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

            string msg = replacedOldCode != null
                ? $"Eski rezervasyonunuz ({replacedOldCode}) otomatik iptal edildi ve {req.Date} günü için {workspace.Code} ({workspace.Name}) adınıza tanımlandı!"
                : $"{workspace.Code} ({workspace.Name}) için {req.Date} tarihli rezervasyonunuz başarıyla tamamlandı!";

            return new
            {
                success = true,
                replaced = replacedOldCode != null,
                replacedOldCode = replacedOldCode,
                message = msg,
                reservation = new
                {
                    newRes.Id,
                    newRes.Date,
                    newRes.TimeSlot,
                    newRes.Note,
                    newRes.Status,
                    WorkspaceCode = workspace.Code,
                    WorkspaceName = workspace.Name
                }
            };
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

            var today = DateTime.Today.ToString("yyyy-MM-dd");
            if (res.Date != today)
            {
                return (false, $"Check-In işlemi yalnızca rezervasyon gününde ({res.Date}) yapılabilir. Yarın veya ileri tarihli masalar önceden Check-In yapılamaz.");
            }

            res.Status = "CheckedIn";
            Save();
            var ws = _data.Workspaces.FirstOrDefault(w => w.Id == res.WorkspaceItemId);
            return (true, $"{ws?.Code ?? "Masa"} için bugünkü Ofis Check-In doğrulamanız tamamlandı! Artık Ofistekiler sayfasında görünüyorsunuz.");
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

    // ==================== ADMIN FLOOR & DESK LAYOUT DESIGNER ENGINE ====================
    public object CreateFloor(CreateFloorRequest req)
    {
        lock (_lock)
        {
            int nextFloorId = _data.Floors.Any() ? _data.Floors.Max(f => f.Id) + 1 : 1;
            string floorName = string.IsNullOrWhiteSpace(req.Name)
                ? $"{nextFloorId}. Kat — Yeni Ofis Katı"
                : req.Name.Trim();
            string floorDesc = string.IsNullOrWhiteSpace(req.Description)
                ? "Özel Tasarım Çalışma Alanı"
                : req.Description.Trim();

            var newFloor = new OfficeFloor
            {
                Id = nextFloorId,
                Code = $"KAT-{nextFloorId}",
                Name = floorName,
                Description = floorDesc,
                IsActive = req.IsActive
            };
            _data.Floors.Add(newFloor);

            int deskCount = Math.Clamp(req.InitialDeskCount, 1, 20);
            int seatsPerDesk = Math.Clamp(req.SeatsPerDesk, 1, 8);
            string orientation = (req.DefaultOrientation == "vertical") ? "vertical" : "horizontal";
            int nextWsId = _data.Workspaces.Any() ? _data.Workspaces.Max(w => w.Id) + 1 : 1;

            for (int d = 1; d <= deskCount; d++)
            {
                int row = (d - 1) / 2 + 1;
                int side = (d % 2 == 1) ? 1 : 2;
                for (int s = 1; s <= seatsPerDesk; s++)
                {
                    string code = BuildSeatCode(nextFloorId, d, s);
                    _data.Workspaces.Add(new WorkspaceItem
                    {
                        Id = nextWsId++,
                        FloorId = nextFloorId,
                        Code = code,
                        Name = BuildSeatName(nextFloorId, d, s, code),
                        Type = "Masa",
                        Zone = $"Masa {d}",
                        Capacity = 1,
                        Orientation = orientation,
                        Features = GetDeskFeatures(d, s == 1),
                        GridX = side,
                        GridY = row,
                        GridW = d,
                        GridH = s,
                        CanvasX = side == 1 ? 6 : 54,
                        CanvasY = Math.Min(78, 6 + (row - 1) * 18),
                        IsActive = true
                    });
                }
            }

            if (req.IncludeMeetingRoom)
            {
                int roomCount = _data.Workspaces.Count(w => w.Type == "Oda") + 1;
                _data.Workspaces.Add(new WorkspaceItem
                {
                    Id = nextWsId++,
                    FloorId = nextFloorId,
                    Code = $"ODA-{roomCount}",
                    Name = $"Kat {nextFloorId} Toplantı Odası (ODA-{roomCount})",
                    Type = "Oda",
                    Zone = "Toplantı Odaları",
                    Capacity = 8,
                    Orientation = "horizontal",
                    Features = new() { "75\" 4K Sunum Ekranı", "Video Konferans Sistemi", "Ses Yalıtımlı Cam Cephe" },
                    GridX = 1,
                    GridY = (deskCount + 1) / 2 + 1,
                    IsActive = true
                });
            }

            Save();
            return new
            {
                success = true,
                message = $"{floorName} başarıyla oluşturuldu ({deskCount} masa x {seatsPerDesk} kişilik)!",
                floor = newFloor,
                floors = GetFloors()
            };
        }
    }

    public object ToggleFloorStatus(ToggleFloorStatusRequest req)
    {
        lock (_lock)
        {
            var floor = _data.Floors.FirstOrDefault(f => f.Id == req.FloorId);
            if (floor == null)
                return new { success = false, message = "Ofis katı bulunamadı." };

            floor.IsActive = req.IsActive;
            Save();

            string statusText = floor.IsActive ? "kullanıma AÇILDI" : "kullanıma KAPATILDI";
            return new
            {
                success = true,
                message = $"{floor.Name} başarıyla {statusText}.",
                floor = floor,
                floors = GetFloors()
            };
        }
    }

    public object UpdateDeskGroup(UpdateDeskGroupRequest req)
    {
        lock (_lock)
        {
            int targetSeats = Math.Clamp(req.SeatCount, 1, 8);
            string orientation = (req.Orientation == "vertical") ? "vertical" : "horizontal";
            int side = req.GridX == 2 ? 2 : 1;
            int row = Math.Max(1, req.GridY);

            var allDeskSeats = _data.Workspaces
                .Where(w => w.FloorId == req.FloorId && w.Type == "Masa" && w.GridW == req.DeskNo)
                .OrderBy(w => w.GridH)
                .ToList();

            int existingCanvasX = allDeskSeats.FirstOrDefault()?.CanvasX ?? (side == 1 ? 6 : 54);
            int existingCanvasY = allDeskSeats.FirstOrDefault()?.CanvasY ?? Math.Min(78, 6 + (row - 1) * 18);
            int canvasX = (req.CanvasX.HasValue && req.CanvasX.Value >= 0) ? req.CanvasX.Value : existingCanvasX;
            int canvasY = (req.CanvasY.HasValue && req.CanvasY.Value >= 0) ? req.CanvasY.Value : existingCanvasY;

            int nextWsId = _data.Workspaces.Any() ? _data.Workspaces.Max(w => w.Id) + 1 : 1;

            for (int s = 1; s <= targetSeats; s++)
            {
                var existingSeat = allDeskSeats.FirstOrDefault(w => w.GridH == s);
                string code = BuildSeatCode(req.FloorId, req.DeskNo, s);
                if (existingSeat != null)
                {
                    existingSeat.IsActive = true;
                    existingSeat.Orientation = orientation;
                    existingSeat.GridX = side;
                    existingSeat.GridY = row;
                    existingSeat.CanvasX = canvasX;
                    existingSeat.CanvasY = canvasY;
                    existingSeat.Code = code;
                    existingSeat.Name = BuildSeatName(req.FloorId, req.DeskNo, s, code);
                    if (req.Features != null && req.Features.Count > 0)
                    {
                        existingSeat.Features = new List<string>(req.Features);
                    }
                }
                else
                {
                    var baseFeatures = (req.Features != null && req.Features.Count > 0)
                        ? new List<string>(req.Features)
                        : (allDeskSeats.FirstOrDefault()?.Features ?? GetDeskFeatures(req.DeskNo, s == 1));

                    _data.Workspaces.Add(new WorkspaceItem
                    {
                        Id = nextWsId++,
                        FloorId = req.FloorId,
                        Code = code,
                        Name = BuildSeatName(req.FloorId, req.DeskNo, s, code),
                        Type = "Masa",
                        Zone = $"Masa {req.DeskNo}",
                        Capacity = 1,
                        Orientation = orientation,
                        Features = new List<string>(baseFeatures),
                        GridX = side,
                        GridY = row,
                        GridW = req.DeskNo,
                        GridH = s,
                        CanvasX = canvasX,
                        CanvasY = canvasY,
                        IsActive = true
                    });
                }
            }

            // Deactivate any seats beyond targetSeats and cancel their active reservations
            foreach (var extraSeat in allDeskSeats.Where(w => w.GridH > targetSeats))
            {
                extraSeat.IsActive = false;
                foreach (var r in _data.Reservations.Where(r => r.WorkspaceItemId == extraSeat.Id && r.Status != "Cancelled"))
                {
                    r.Status = "Cancelled";
                }
            }

            Save();
            string orientText = orientation == "vertical" ? "Dikine" : "Yatay";
            return new
            {
                success = true,
                message = $"Masa {req.DeskNo} güncellendi: {targetSeats} Kişilik ({orientText} düzen)!"
            };
        }
    }

    public object AddDeskGroup(AddDeskGroupRequest req)
    {
        lock (_lock)
        {
            var floorDesks = _data.Workspaces
                .Where(w => w.FloorId == req.FloorId && w.Type == "Masa")
                .ToList();

            int nextDeskNo = floorDesks.Any() ? floorDesks.Max(w => w.GridW) + 1 : 1;
            int seatCount = Math.Clamp(req.SeatCount, 1, 8);
            string orientation = (req.Orientation == "vertical") ? "vertical" : "horizontal";
            int side = (req.GridX == 1 || req.GridX == 2) ? req.GridX : ((nextDeskNo % 2 == 1) ? 1 : 2);
            int row = (nextDeskNo - 1) / 2 + 1;
            int canvasX = req.CanvasX ?? (side == 1 ? 6 : 54);
            int canvasY = req.CanvasY ?? Math.Min(82, (row - 1) * 18 + 6);
            int nextWsId = _data.Workspaces.Any() ? _data.Workspaces.Max(w => w.Id) + 1 : 1;

            for (int s = 1; s <= seatCount; s++)
            {
                string code = BuildSeatCode(req.FloorId, nextDeskNo, s);
                var feats = (req.Features != null && req.Features.Count > 0)
                    ? new List<string>(req.Features)
                    : GetDeskFeatures(nextDeskNo, s == 1);

                _data.Workspaces.Add(new WorkspaceItem
                {
                    Id = nextWsId++,
                    FloorId = req.FloorId,
                    Code = code,
                    Name = BuildSeatName(req.FloorId, nextDeskNo, s, code),
                    Type = "Masa",
                    Zone = $"Masa {nextDeskNo}",
                    Capacity = 1,
                    Orientation = orientation,
                    Features = feats,
                    GridX = side,
                    GridY = row,
                    GridW = nextDeskNo,
                    GridH = s,
                    CanvasX = canvasX,
                    CanvasY = canvasY,
                    IsActive = true
                });
            }

            Save();
            return new
            {
                success = true,
                deskNo = nextDeskNo,
                message = $"Yeni Masa {nextDeskNo} ({seatCount} Kişilik, {(orientation == "vertical" ? "Dikine" : "Yatay")}) kata eklendi!"
            };
        }
    }

    public object DeleteDeskGroup(int floorId, int deskNo)
    {
        lock (_lock)
        {
            var seats = _data.Workspaces
                .Where(w => w.FloorId == floorId && w.Type == "Masa" && w.GridW == deskNo)
                .ToList();

            if (seats.Count == 0)
                return new { success = false, message = "Masa bulunamadı." };

            foreach (var seat in seats)
            {
                seat.IsActive = false;
                foreach (var r in _data.Reservations.Where(r => r.WorkspaceItemId == seat.Id && r.Status != "Cancelled"))
                {
                    r.Status = "Cancelled";
                }
            }

            Save();
            return new
            {
                success = true,
                message = $"Masa {deskNo} kaldırıldı."
            };
        }
    }

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    // ==================== GEMINI FREE + CONVERSATIONAL AI CHATBOT ENGINE ====================
    public async Task<object> ProcessAiChatAsync(AiChatRequest req)
    {
        User user;
        string text;
        string lower;
        string todayStr;
        string targetDate;
        string timeSlot;
        HashSet<int> reservedIds;

        lock (_lock)
        {
            user = _data.Users.FirstOrDefault(u => u.Id == req.UserId) ?? _data.Users.First();
            text = (req.Message ?? "").Trim();
            lower = text.ToLower(new System.Globalization.CultureInfo("tr-TR"));
            todayStr = DateTime.Today.ToString("yyyy-MM-dd");

            targetDate = string.IsNullOrWhiteSpace(req.SelectedDate)
                ? todayStr
                : req.SelectedDate;

            if (lower.Contains("bugün") || lower.Contains("bugun"))
                targetDate = todayStr;
            else if (lower.Contains("öbür gün") || lower.Contains("obur gun") || lower.Contains("2 gün sonra"))
                targetDate = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");
            else if (lower.Contains("yarın") || lower.Contains("yarin"))
                targetDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            else
            {
                var dateMatch = Regex.Match(lower, @"\b(202\d-\d{2}-\d{2})\b");
                if (dateMatch.Success)
                    targetDate = dateMatch.Groups[1].Value;
            }

            timeSlot = "Tam Gün (09:00 - 18:00)";
            if (lower.Contains("öğleden önce") || lower.Contains("sabah"))
                timeSlot = "Öğleden Önce (09:00 - 13:00)";
            else if (lower.Contains("öğleden sonra"))
                timeSlot = "Öğleden Sonra (13:30 - 18:00)";

            if (lower.Contains("vazgeç") && !lower.Contains("iptal"))
            {
                return new
                {
                    replyHtml = "İşlemden vazgeçildi, mevcut rezervasyonunuz korundu.",
                    action = "NONE",
                    targetDate = targetDate,
                    buttons = new List<AiActionButton>
                    {
                        new() { Label = "📅 Rezervasyonlarımı Göster", Command = "Rezervasyonlarım neler?", Style = "outline" },
                        new() { Label = "🏢 Ofiste Kimler Var?", Command = "Bugün ofiste kimler var?", Style = "outline" }
                    }
                };
            }

            reservedIds = _data.Reservations
                .Where(r => r.Date == targetDate && r.Status != "Cancelled")
                .Select(r => r.WorkspaceItemId)
                .ToHashSet();

            bool isQuestionAboutRulesOrHowTo =
                lower.Contains("nasıl") || lower.Contains("nasil") ||
                lower.Contains("nedir") || lower.Contains("kural") ||
                lower.Contains("ne işe") || lower.Contains("hakkında");

            // INTENT A: DIRECT CHECK-IN ACTION (ONLY TODAY ALLOWED!)
            if (!isQuestionAboutRulesOrHowTo &&
                (lower.Contains("check") || lower.Contains("ofisteyim") || lower.Contains("ofise geldim") || lower.Contains("doğrula") || lower.Contains("dogrula")))
            {
                var pendingToday = _data.Reservations
                    .FirstOrDefault(r => r.UserId == user.Id && r.Date == todayStr && r.Status == "Active");

                if (pendingToday != null)
                {
                    pendingToday.Status = "CheckedIn";
                    Save();
                    var ws = _data.Workspaces.FirstOrDefault(w => w.Id == pendingToday.WorkspaceItemId);
                    return new
                    {
                        replyHtml = $"✅ Harika <strong>{user.FullName}</strong>! Bugünkü (<strong>{todayStr}</strong>) <strong>{ws?.Code} ({ws?.Name})</strong> rezervasyonunuz için <strong>Ofis Check-In</strong> doğrulamanızı tamamladım.",
                        action = "CHECKED_IN",
                        targetDate = todayStr,
                        showPopup = true,
                        popupTitle = "Ofis Check-In Doğrulandı!",
                        popupMessage = $"{ws?.Code} ({ws?.Name}) için bugün ofiste olduğunuz doğrulandı.",
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = "🏢 Ofiste Kimler Var?", Command = "Bugün ofiste kimler var?", Style = "blue" },
                            new() { Label = "📅 Rezervasyonlarım", Command = "Rezervasyonlarım neler?", Style = "outline" }
                        }
                    };
                }

                var alreadyCheckedToday = _data.Reservations
                    .FirstOrDefault(r => r.UserId == user.Id && r.Date == todayStr && r.Status == "CheckedIn");

                if (alreadyCheckedToday != null)
                {
                    var ws = _data.Workspaces.FirstOrDefault(w => w.Id == alreadyCheckedToday.WorkspaceItemId);
                    return new
                    {
                        replyHtml = $"Bugün (<strong>{todayStr}</strong>) için <strong>{ws?.Code}</strong> masasında Check-In doğrulamanız zaten yapılmış durumda!",
                        action = "NONE",
                        targetDate = todayStr,
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = "📅 Yarın İçin Boş Masa Ayırt", Command = "Yarın için bana boş masa ayırt", Style = "blue" },
                            new() { Label = "🏢 Ofiste Kimler Var?", Command = "Bugün ofiste kimler var?", Style = "outline" }
                        }
                    };
                }

                var futureRes = _data.Reservations
                    .Where(r => r.UserId == user.Id && r.Status == "Active" && string.Compare(r.Date, todayStr) > 0)
                    .OrderBy(r => r.Date)
                    .FirstOrDefault();

                if (futureRes != null)
                {
                    var ws = _data.Workspaces.FirstOrDefault(w => w.Id == futureRes.WorkspaceItemId);
                    return new
                    {
                        replyHtml = $"⚠️ <strong>{futureRes.Date}</strong> tarihli <strong>{ws?.Code}</strong> rezervasyonunuz ileri bir tarihe ait. <strong>Check-In işlemi yalnızca aynı gün içinde ({todayStr}) yapılabilir.</strong><br>Bugün için masa ayırtmak ister misiniz?",
                        action = "NONE",
                        targetDate = todayStr,
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = "✨ Bugün İçin Boş Masa Ayırt", Command = "Bana bugün boş masa ayırt", Style = "blue" }
                        }
                    };
                }

                return new
                {
                    replyHtml = $"Bugün (<strong>{todayStr}</strong>) için aktif bir masa rezervasyonunuz bulunmuyor. Önce bugün için boş bir masa seçmek ister misiniz?",
                    action = "NONE",
                    targetDate = todayStr,
                    buttons = new List<AiActionButton>
                    {
                        new() { Label = "✨ Bugün Boş Masaları Göster", Command = "Bana bugün boş masa ayırt", Style = "blue" }
                    }
                };
            }

            // INTENT B: DIRECT CANCEL RESERVATION
            if (!isQuestionAboutRulesOrHowTo && (lower.Contains("iptal") || lower.Contains("rezervasyonumu sil")))
            {
                var activeUserRes = _data.Reservations
                    .Where(r => r.UserId == user.Id && r.Status != "Cancelled")
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefault();

                if (activeUserRes != null)
                {
                    activeUserRes.Status = "Cancelled";
                    Save();
                    var ws = _data.Workspaces.FirstOrDefault(w => w.Id == activeUserRes.WorkspaceItemId);
                    return new
                    {
                        replyHtml = $"🗑️ <strong>{activeUserRes.Date}</strong> tarihli <strong>{ws?.Code} ({ws?.Name})</strong> rezervasyonunuz iptal edildi.",
                        action = "CANCELLED",
                        targetDate = activeUserRes.Date,
                        showPopup = true,
                        popupTitle = "Rezervasyon İptal Edildi",
                        popupMessage = $"{activeUserRes.Date} tarihli {ws?.Code} rezervasyonunuz iptal edildi ve boşa çıkarıldı.",
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = "✨ Yeni Masa Seç", Command = "Bana bugün boş masa ayırt", Style = "blue" }
                        }
                    };
                }

                return new
                {
                    replyHtml = "İptal edilebilecek aktif bir rezervasyonunuz bulunmuyor.",
                    action = "NONE",
                    targetDate = targetDate,
                    buttons = new List<AiActionButton>
                    {
                        new() { Label = "✨ Bana Boş Masa Ayırt", Command = "Bana bugün boş masa ayırt", Style = "blue" }
                    }
                };
            }

            // INTENT C: WHO IS IN THE OFFICE
            if (lower.Contains("kimler var") || lower.Contains("kim var") || lower.Contains("ofistekiler"))
            {
                var checkedInPeople = _data.Reservations
                    .Where(r => r.Date == targetDate && r.Status == "CheckedIn")
                    .Select(r =>
                    {
                        var u = _data.Users.FirstOrDefault(x => x.Id == r.UserId);
                        var w = _data.Workspaces.FirstOrDefault(x => x.Id == r.WorkspaceItemId);
                        return $"<strong>{u?.FullName}</strong> (<strong>{w?.Code}</strong> - {u?.Department})";
                    })
                    .ToList();

                if (checkedInPeople.Count == 0)
                {
                    return new
                    {
                        replyHtml = $"<strong>{targetDate}</strong> tarihinde henüz ofise gelip Check-In yapmış kimse görünmüyor.",
                        action = "NONE",
                        targetDate = targetDate,
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = "✨ Bugün Masa Ayırt", Command = "Bana bugün boş masa ayırt", Style = "blue" }
                        }
                    };
                }

                return new
                {
                    replyHtml = $"<strong>{targetDate}</strong> tarihinde ofiste Check-In yapmış <strong>{checkedInPeople.Count} kişi</strong> var:<br>• " + string.Join("<br>• ", checkedInPeople),
                    action = "SHOW_PRESENCE",
                    targetDate = targetDate,
                    buttons = new List<AiActionButton>
                    {
                        new() { Label = "✨ Bana Boş Masa Ayırt", Command = "Bana bugün boş masa ayırt", Style = "blue" },
                        new() { Label = "📍 Check-In Yap", Command = "Ofise geldim, Check-In yap", Style = "outline" }
                    }
                };
            }

            // INTENT D: MY RESERVATIONS
            if (lower.Contains("rezervasyonlarım") || lower.Contains("masam ne") || lower.Contains("nerede oturuyorum"))
            {
                var myList = _data.Reservations
                    .Where(r => r.UserId == user.Id && r.Status != "Cancelled")
                    .OrderBy(r => r.Date)
                    .Select(r =>
                    {
                        var w = _data.Workspaces.FirstOrDefault(x => x.Id == r.WorkspaceItemId);
                        var st = r.Status == "CheckedIn" ? "✅ Check-In Doğrulandı" : "⏳ Check-In Bekleniyor";
                        return $"<strong>{r.Date}</strong>: <strong>{w?.Code}</strong> ({st})";
                    })
                    .ToList();

                if (myList.Count == 0)
                {
                    return new
                    {
                        replyHtml = "Henüz aktif bir masa veya oda rezervasyonunuz bulunmuyor.",
                        action = "NONE",
                        targetDate = targetDate,
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = "✨ Bugün İçin Masa Seç", Command = "Bana bugün boş masa ayırt", Style = "blue" },
                            new() { Label = "📅 Yarın İçin Masa Seç", Command = "Yarın için bana boş masa ayırt", Style = "outline" }
                        }
                    };
                }

                return new
                {
                    replyHtml = "Aktif rezervasyonlarınız:<br>• " + string.Join("<br>• ", myList),
                    action = "NONE",
                    targetDate = targetDate,
                    buttons = new List<AiActionButton>
                    {
                        new() { Label = "📍 Ofisteyim — Check-In Yap", Command = "Ofise geldim, Check-In yap", Style = "blue" },
                        new() { Label = "🗑️ Son Rezervasyonumu İptal Et", Command = "Son rezervasyonumu iptal et", Style = "red" }
                    }
                };
            }

            // DETECT SPECIFIC WORKSPACE CODE (1A..20H, K2-1A..K9-20H, or ODA-1..ODA-20)
            WorkspaceItem? matchedWorkspace = null;
            var roomMatch = Regex.Match(lower, @"oda[\s\-]*(\d+)");
            if (roomMatch.Success)
            {
                string code = "ODA-" + roomMatch.Groups[1].Value;
                matchedWorkspace = _data.Workspaces.FirstOrDefault(w => w.IsActive && w.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var floorSeatMatch = Regex.Match(lower, @"\bk(\d+)[\s\-]*(\d+)\s*([a-h])\b");
                if (floorSeatMatch.Success)
                {
                    string code = $"K{floorSeatMatch.Groups[1].Value}-{floorSeatMatch.Groups[2].Value}{floorSeatMatch.Groups[3].Value.ToUpper()}";
                    matchedWorkspace = _data.Workspaces.FirstOrDefault(w => w.IsActive && w.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    var seatMatch = Regex.Match(lower, @"\b(20|1\d|[1-9])\s*([a-h])\b");
                    if (seatMatch.Success)
                    {
                        string code = (seatMatch.Groups[1].Value + seatMatch.Groups[2].Value).ToUpper();
                        matchedWorkspace = _data.Workspaces.FirstOrDefault(w => w.IsActive && w.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }

            bool isAskingAboutFeaturesOrStatus =
                lower.Contains("özellik") || lower.Contains("ozellik") ||
                lower.Contains("ne var") || lower.Contains("nedir") ||
                lower.Contains("boş mu") || lower.Contains("dolu mu") ||
                lower.Contains("kaç kişilik") || lower.Contains("monitör") ||
                lower.Contains("monitor") || lower.Contains("cam kenar") ||
                lower.Contains("yükseklik");

            bool hasBookingVerb =
                lower.Contains("ayırt") || lower.Contains("ayirt") ||
                lower.Contains("rezerv") || lower.Contains("seç") ||
                lower.Contains("değiştir") || lower.Contains("degistir") ||
                lower.Contains("isterim") || lower.Contains("istiyorum") ||
                lower.Trim().Length <= 4; // e.g., user just typed "1A" or "ODA-2"

            // INTENT E: DIRECT BOOKING OF A SPECIFIC SEAT/ROOM
            if (matchedWorkspace != null && (!isAskingAboutFeaturesOrStatus || hasBookingVerb))
            {
                if (reservedIds.Contains(matchedWorkspace.Id))
                {
                    var alts = _data.Workspaces
                        .Where(w => w.IsActive && w.Type == matchedWorkspace.Type && !reservedIds.Contains(w.Id))
                        .ToList();

                    var altBold = string.Join(", ", alts.Select(a => $"<strong>{a.Code}</strong>"));
                    return new
                    {
                        replyHtml = $"Üzgünüm, <strong>{targetDate}</strong> tarihinde <strong>{matchedWorkspace.Code}</strong> dolu.<br>Boş olanlar: {altBold}<br>Hangisini istersiniz?",
                        action = "NONE",
                        targetDate = targetDate,
                        buttons = alts.Take(8).Select(a => new AiActionButton
                        {
                            Label = $"🪑 {a.Code} Seç",
                            Command = $"{targetDate} için {a.Code} koltuğunu ayırt",
                            Style = "blue"
                        }).ToList()
                    };
                }

                var existingSameDayItems = _data.Reservations
                    .Where(r => r.UserId == user.Id && r.Date == targetDate && r.Status != "Cancelled")
                    .Select(r => new { Res = r, Ws = _data.Workspaces.FirstOrDefault(w => w.Id == r.WorkspaceItemId) })
                    .Where(x => x.Ws != null && x.Ws.Type == matchedWorkspace.Type)
                    .ToList();

                bool shouldReplace = req.ReplaceExisting || lower.Contains("değiştir") || lower.Contains("degistir");
                string typeLabel = matchedWorkspace.Type == "Oda" ? "toplantı odası" : "masa";

                if (existingSameDayItems.Count > 0 && !shouldReplace)
                {
                    var oldCodes = string.Join(", ", existingSameDayItems.Select(x => x.Ws!.Code));
                    return new
                    {
                        replyHtml = $"⚠️ <strong>{targetDate}</strong> tarihinde zaten <strong>{oldCodes}</strong> ({typeLabel}) rezervasyonunuz var.<br>Eski rezervasyonunuzu otomatik iptal edip <strong>{matchedWorkspace.Code}</strong> ile <strong>değiştirmek ister misiniz?</strong>",
                        action = "ASK_CHANGE",
                        targetDate = targetDate,
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = $"🔄 Evet, {matchedWorkspace.Code} İle Değiştir", Command = $"{targetDate} için {matchedWorkspace.Code} koltuğunu değiştir", Style = "blue", ReplaceExisting = true },
                            new() { Label = $"✕ Hayır, {oldCodes} Kalsın", Command = "Değiştirmekten vazgeç", Style = "outline" }
                        }
                    };
                }

                string? cancelledOldCodes = null;
                if (existingSameDayItems.Count > 0 && shouldReplace)
                {
                    cancelledOldCodes = string.Join(", ", existingSameDayItems.Select(x => x.Ws!.Code));
                    foreach (var old in existingSameDayItems)
                    {
                        old.Res.Status = "Cancelled";
                    }
                }

                var nextId = _data.Reservations.Any() ? _data.Reservations.Max(r => r.Id) + 1 : 1;
                var newRes = new Reservation
                {
                    Id = nextId,
                    WorkspaceItemId = matchedWorkspace.Id,
                    UserId = user.Id,
                    Date = targetDate,
                    TimeSlot = timeSlot,
                    Note = "RAY AI Asistan ile ayrıldı",
                    Status = "Active",
                    CreatedAt = DateTime.Now
                };
                _data.Reservations.Add(newRes);
                Save();

                string replyText = cancelledOldCodes != null
                    ? $"🔄 Eski rezervasyonunuz (<strong>{cancelledOldCodes}</strong>) otomatik iptal edildi ve <strong>{targetDate}</strong> günü için <strong>{matchedWorkspace.Code} — {matchedWorkspace.Name}</strong> adınıza ayrıldı!"
                    : $"🎉 Tamamdır <strong>{user.FullName}</strong>! <strong>{targetDate}</strong> tarihi için <strong>{matchedWorkspace.Code} — {matchedWorkspace.Name}</strong> sizin adınıza ayrıldı!";

                var actionBtns = new List<AiActionButton>();
                if (targetDate == todayStr)
                {
                    actionBtns.Add(new AiActionButton { Label = "📍 Ofisteyim — Check-In Yap", Command = "Ofise geldim, Check-In yap", Style = "blue" });
                }
                actionBtns.Add(new AiActionButton { Label = "🗑️ Rezervasyonu İptal Et", Command = "Son rezervasyonumu iptal et", Style = "red" });

                return new
                {
                    replyHtml = replyText,
                    action = "RESERVED",
                    targetDate = targetDate,
                    showPopup = true,
                    popupTitle = cancelledOldCodes != null ? "Rezervasyon Değiştirildi!" : "Rezervasyon Tamamlandı!",
                    popupMessage = cancelledOldCodes != null
                        ? $"Eski rezervasyonunuz ({cancelledOldCodes}) iptal edildi ve {targetDate} günü için {matchedWorkspace.Code} ({matchedWorkspace.Name}) adınıza tanımlandı."
                        : $"{targetDate} günü ({timeSlot}) için {matchedWorkspace.Code} ({matchedWorkspace.Name}) adınıza ayrıldı.",
                    reservedItem = new
                    {
                        code = matchedWorkspace.Code,
                        name = matchedWorkspace.Name,
                        date = targetDate,
                        timeSlot = timeSlot
                    },
                    buttons = actionBtns
                };
            }

            // INTENT F: DIRECT COMMAND TO LIST AVAILABLE DESKS / ROOMS IN BOLD WITH SELECTION BUTTONS
            bool isDirectListOrBookCommand =
                (lower.Contains("boş masa") || lower.Contains("bos masa") ||
                 lower.Contains("boş oda") || lower.Contains("bos oda") ||
                 lower.Contains("masa ayırt") || lower.Contains("masa ayirt") ||
                 lower.Contains("oda ayırt") || lower.Contains("oda ayirt") ||
                 lower.Contains("boş koltuk")) &&
                !isAskingAboutFeaturesOrStatus;

            if (isDirectListOrBookCommand)
            {
                bool wantsRoom = lower.Contains("oda") || lower.Contains("toplantı");
                var freeItems = _data.Workspaces
                    .Where(w => w.IsActive && w.Type == (wantsRoom ? "Oda" : "Masa") && !reservedIds.Contains(w.Id))
                    .ToList();

                if (freeItems.Count == 0)
                {
                    return new
                    {
                        replyHtml = $"<strong>{targetDate}</strong> tarihinde boş {(wantsRoom ? "oda" : "masa")} kalmamış. Yarın için bakmamı ister misiniz?",
                        action = "NONE",
                        targetDate = targetDate,
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = "📅 Yarın Boş Masaları Göster", Command = "Yarın için bana boş masa ayırt", Style = "blue" }
                        }
                    };
                }

                var boldList = string.Join(", ", freeItems.Select(w => $"<strong>{w.Code}</strong>"));
                var seatButtons = freeItems.Select(w => new AiActionButton
                {
                    Label = $"🪑 {w.Code} Seç",
                    Command = $"{targetDate} için {w.Code} koltuğunu ayırt",
                    Style = "blue"
                }).ToList();

                return new
                {
                    replyHtml = $"📅 <strong>{targetDate}</strong> için boş {(wantsRoom ? "oda" : "masa")} numaraları:<br>{boldList}<br><strong>Hangisini istersiniz? Seçin:</strong>",
                    action = "LIST_OPTIONS",
                    targetDate = targetDate,
                    buttons = seatButtons
                };
            }
        }

        // ==================== INTENT G: GEMINI FREE AI (SITE KNOWLEDGE + GENERAL QUESTIONS) ====================
        return await GenerateGeminiResponseAsync(req, user, text, lower, targetDate, todayStr);
    }

    private string BuildSiteSystemPrompt(User user, string targetDate, string todayStr)
    {
        lock (_lock)
        {
            var reservedMap = _data.Reservations
                .Where(r => r.Date == targetDate && r.Status != "Cancelled")
                .ToDictionary(r => r.WorkspaceItemId, r => r);

            var deskLines = _data.Workspaces
                .Where(w => w.IsActive)
                .Select(w =>
                {
                    bool isBusy = reservedMap.ContainsKey(w.Id);
                    string status = isBusy ? "DOLU" : "BOŞ";
                    string feats = string.Join(", ", w.Features);
                    return $"- {w.Code} ({w.Name}, Tip: {w.Type}, Kapasite: {w.Capacity}): Durum={status} | Özellikler: [{feats}]";
                });

            var myActive = _data.Reservations
                .Where(r => r.UserId == user.Id && r.Status != "Cancelled")
                .Select(r =>
                {
                    var w = _data.Workspaces.FirstOrDefault(x => x.Id == r.WorkspaceItemId);
                    return $"{r.Date} -> {w?.Code} ({r.Status})";
                }).ToList();

            var checkedInToday = _data.Reservations
                .Where(r => r.Date == todayStr && r.Status == "CheckedIn")
                .Select(r =>
                {
                    var u = _data.Users.FirstOrDefault(x => x.Id == r.UserId);
                    var w = _data.Workspaces.FirstOrDefault(x => x.Id == r.WorkspaceItemId);
                    return $"{u?.FullName} ({w?.Code})";
                }).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Sen 'RAY Yapay Zeka Asistanı'sın (Google Gemini altyapısıyla çalışıyorsun).");
            sb.AppendLine("Hem içinde bulunduğun 'RAY OFİS — Masa & Toplantı Odası Rezervasyon Sistemi' web uygulamasını eksiksiz biliyorsun, hem de kullanıcının masa/ofis dışındaki her türlü genel kültür, yazılım, günlük hayat, matematik, yemek tarifi, tavsiye veya sohbet sorularına zekice, samimi ve doğru Türkçe yanıtlar veriyorsun.");
            sb.AppendLine();
            sb.AppendLine("=== KULLANICI VE TARİH BİLGİSİ ===");
            sb.AppendLine($"- Konuştuğun Kullanıcı: {user.FullName} ({user.Department} - {user.Title}, Rol: {user.Role})");
            sb.AppendLine($"- Bugünün Tarihi: {todayStr} | Seçili Tarih: {targetDate}");
            sb.AppendLine($"- Kullanıcının Aktif Rezervasyonları: {(myActive.Count > 0 ? string.Join("; ", myActive) : "Yok")}");
            sb.AppendLine($"- Bugün Ofiste Check-In Yapmış Olanlar: {(checkedInToday.Count > 0 ? string.Join(", ", checkedInToday) : "Henüz kimse Check-In yapmadı")}");
            sb.AppendLine();
            sb.AppendLine("=== SİTE KURALLARI VE ÖZELLİKLERİ (RAY OFİS) ===");
            sb.AppendLine("1. Ofiste 10 adet 2 kişilik masa modülü (1A, 1B ... 10A, 10B toplam 20 koltuk) ve 4 adet Toplantı Odası (ODA-1, ODA-2, ODA-3, ODA-4) vardır.");
            sb.AppendLine("2. Bir kullanıcı aynı gün içinde en fazla 1 masa ve 1 toplantı odası rezerve edebilir. Aynı güne 2. bir masa veya oda seçerse sistem eski rezervasyonunu otomatik iptal edip yenisiyle değiştirir.");
            sb.AppendLine("3. Check-In (Ofisteyim doğrulaması) SADECE rezervasyonun olduğu gün (bugün) yapılabilir. Yarın veya ileri tarihli masalar önceden Check-In yapılamaz.");
            sb.AppendLine("4. Gizlilik gereği normal kullanıcılar rezervasyon ekranında kırmızı (dolu) koltukları kimin aldığını göremez; yalnızca ofise gelip Check-In yapanları 'Ofistekiler' sekmesinde görebilir. Admin ise tüm detayları görür.");
            sb.AppendLine("5. Rezervasyon yapıldığında Google Takvim ve Outlook Takvim'e ekleme butonları çıkar.");
            sb.AppendLine();
            sb.AppendLine($"=== {targetDate} TARİHİ İÇİN MASALAR, ODALAR VE DONANIM ÖZELLİKLERİ ===");
            sb.AppendLine(string.Join("\n", deskLines));
            sb.AppendLine();
            sb.AppendLine("=== YANIT FORMATI KURALLARI ===");
            sb.AppendLine("- Yanıtlarını her zaman Türkçe, kısa, net ve okunaklı ver (en fazla 3-5 cümle veya maddeli liste).");
            sb.AppendLine("- Önemli kelimeleri ve masa numaralarını (örn. **1A**, **3B**, **ODA-1**) kalın (**...**) yaz.");
            sb.AppendLine("- Eğer kullanıcı masa/ofis dışında genel bir soru sorarsa (örn. hava durumu, yemek, kodlama, tarih, sohbet vb.), doğrudan o soruyu en iyi şekilde yanıtla.");
            return sb.ToString();
        }
    }

    private async Task<object> GenerateGeminiResponseAsync(AiChatRequest req, User user, string userMessage, string lower, string targetDate, string todayStr)
    {
        string systemPrompt = BuildSiteSystemPrompt(user, targetDate, todayStr);
        string? apiKey = !string.IsNullOrWhiteSpace(req.GeminiApiKey)
            ? req.GeminiApiKey!.Trim()
            : Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        string? aiText = null;

        // 1. Official Google Gemini Free Tier API (if API key provided)
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            aiText = await TryCallGoogleGeminiApiAsync(apiKey!, systemPrompt, userMessage);
        }

        // 2. Zero-Key Free Gemini/OpenAI-Compatible LLM Endpoint (works immediately without requiring user setup)
        if (string.IsNullOrWhiteSpace(aiText))
        {
            aiText = await TryCallFreeKeylessLlmAsync(systemPrompt, userMessage);
        }

        bool usedOffline = false;

        // 3. Built-in Smart Site & General Responder (offline fallback)
        if (string.IsNullOrWhiteSpace(aiText))
        {
            usedOffline = true;
            aiText = BuildSmartLocalFallbackAnswer(user, userMessage, lower, targetDate, todayStr);
        }

        string htmlReply = FormatMarkdownToHtml(aiText!);
        var dynamicButtons = BuildContextualButtons(lower, htmlReply, targetDate, todayStr);

        return new
        {
            replyHtml = htmlReply,
            action = "GEMINI_REPLY",
            usedOfflineFallback = usedOffline,
            systemPrompt = usedOffline ? systemPrompt : null,
            targetDate = targetDate,
            buttons = dynamicButtons
        };
    }

    private async Task<string?> TryCallGoogleGeminiApiAsync(string apiKey, string systemPrompt, string userMessage)
    {
        string[] models = new[] { "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-flash" };
        foreach (var model in models)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
                var payload = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = userMessage } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 500
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync(url, content);
                if (!resp.IsSuccessStatusCode) continue;

                var respStr = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(respStr);
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var first = candidates[0];
                    if (first.TryGetProperty("content", out var cObj) &&
                        cObj.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0 &&
                        parts[0].TryGetProperty("text", out var textProp))
                    {
                        var textVal = textProp.GetString();
                        if (!string.IsNullOrWhiteSpace(textVal))
                            return textVal.Trim();
                    }
                }
            }
            catch
            {
                // Try next model
            }
        }
        return null;
    }

    private async Task<string?> TryCallFreeKeylessLlmAsync(string systemPrompt, string userMessage)
    {
        try
        {
            var payload = new
            {
                model = "openai",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync("https://text.pollinations.ai/openai", content);
            if (resp.IsSuccessStatusCode)
            {
                var respStr = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(respStr);
                if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out var msgObj) &&
                    msgObj.TryGetProperty("content", out var contentProp))
                {
                    var textVal = contentProp.GetString();
                    if (!string.IsNullOrWhiteSpace(textVal))
                        return textVal.Trim();
                }
            }
        }
        catch
        {
            // Ignore network errors and fall back cleanly
        }
        return null;
    }

    private string BuildSmartLocalFallbackAnswer(User user, string userMessage, string lower, string targetDate, string todayStr)
    {
        lock (_lock)
        {
            var reservedIds = _data.Reservations
                .Where(r => r.Date == targetDate && r.Status != "Cancelled")
                .Select(r => r.WorkspaceItemId)
                .ToHashSet();

            // Check if asking about specific desk features (dual monitor, window, standing desk, ultrawide)
            if (lower.Contains("çift monitör") || lower.Contains("cift monitor") || lower.Contains("monitör") ||
                lower.Contains("cam kenar") || lower.Contains("yükseklik") || lower.Contains("yukseklik") ||
                lower.Contains("ultrawide") || lower.Contains("özellik") || lower.Contains("ozellik"))
            {
                var matchingFree = _data.Workspaces
                    .Where(w => w.IsActive && !reservedIds.Contains(w.Id))
                    .Where(w =>
                        (lower.Contains("çift") && w.Features.Any(f => f.Contains("Çift Monitör", StringComparison.OrdinalIgnoreCase))) ||
                        (lower.Contains("cam") && w.Features.Any(f => f.Contains("Cam Kenarı", StringComparison.OrdinalIgnoreCase))) ||
                        ((lower.Contains("yükseklik") || lower.Contains("yukseklik")) && w.Features.Any(f => f.Contains("Yükseklik", StringComparison.OrdinalIgnoreCase))) ||
                        (lower.Contains("ultrawide") && w.Features.Any(f => f.Contains("UltraWide", StringComparison.OrdinalIgnoreCase))))
                    .ToList();

                if (matchingFree.Count > 0)
                {
                    var codes = string.Join(", ", matchingFree.Select(w => $"**{w.Code}**"));
                    var sampleFeats = string.Join(", ", matchingFree[0].Features.Take(3));
                    return $"📅 **{targetDate}** tarihinde aradığınız özelliklere ({sampleFeats}) sahip boş masalarımız: {codes}. Aşağıdaki butonlardan hemen seçip rezerve edebilirsiniz!";
                }
            }

            if (lower.Contains("nasıl") || lower.Contains("kural") || lower.Contains("site") || lower.Contains("ne işe"))
            {
                return "**RAY OFİS Rezervasyon Sistemi** ile uzaktan çalışırken ofise geleceğiniz günü seçip **2 boyutlu masa haritasından** (çift monitörlü, cam kenarı, yükseklik ayarlı) masanızı veya toplantı odanızı ayırtabilirsiniz.\n• Aynı gün için en fazla **1 masa** ve **1 toplantı odası** alabilirsiniz (değiştirirseniz eskisi otomatik iptal olur).\n• Ofise geldiğiniz gün **Check-In** yaparak yerinizi doğrularsınız ve **Ofistekiler** sayfasında görünürsünüz.";
            }

            return $"Merhaba **{user.FullName}**! Ben Gemini destekli **RAY Yapay Zeka Asistanıyım**. Size hem ofis masa/oda rezervasyonları, masa donanım özellikleri (**Çift Monitör**, **Cam Kenarı**, **Yükseklik Ayarlı Masa**) ve **Check-In** işlemlerinde, hem de merak ettiğiniz diğer tüm konularda yardımcı olabilirim.";
        }
    }

    private List<AiActionButton> BuildContextualButtons(string lower, string htmlReply, string targetDate, string todayStr)
    {
        lock (_lock)
        {
            var reservedIds = _data.Reservations
                .Where(r => r.Date == targetDate && r.Status != "Cancelled")
                .Select(r => r.WorkspaceItemId)
                .ToHashSet();

            var buttons = new List<AiActionButton>();

            // Extract any free workspace codes mentioned in the AI reply (e.g. 1A, 1C, K2-1A, ODA-1) so user can book them in 1 click!
            var matches = Regex.Matches(htmlReply, @"\b(K\d+-\d+[A-H]|(?:20|1\d|[1-9])[A-H]|ODA-\d+)\b");
            var addedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in matches)
            {
                var code = m.Value.ToUpper();
                if (addedCodes.Contains(code)) continue;
                var ws = _data.Workspaces.FirstOrDefault(w => w.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
                if (ws != null && ws.IsActive && !reservedIds.Contains(ws.Id))
                {
                    addedCodes.Add(code);
                    buttons.Add(new AiActionButton
                    {
                        Label = $"🪑 {ws.Code} Seç",
                        Command = $"{targetDate} için {ws.Code} koltuğunu ayırt",
                        Style = "blue"
                    });
                    if (buttons.Count >= 6) break;
                }
            }

            if (buttons.Count == 0)
            {
                buttons.Add(new AiActionButton { Label = "✨ Bugün Boş Masa Seç", Command = "Bana bugün boş masa ayırt", Style = "blue" });
                buttons.Add(new AiActionButton { Label = "📍 Check-In Yap", Command = "Ofise geldim, Check-In yap", Style = "outline" });
                buttons.Add(new AiActionButton { Label = "👥 Ofiste Kimler Var?", Command = "Bugün ofiste kimler var?", Style = "outline" });
            }

            return buttons;
        }
    }

    private static string FormatMarkdownToHtml(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var encoded = System.Net.WebUtility.HtmlEncode(text.Trim());
        encoded = Regex.Replace(encoded, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        encoded = Regex.Replace(encoded, @"`([^`]+)`", "<code style=\"background:rgba(1,53,235,0.08);padding:1px 5px;border-radius:4px;font-weight:700;\">$1</code>");
        encoded = encoded.Replace("\r\n", "\n").Replace("\n", "<br>");
        return encoded;
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
