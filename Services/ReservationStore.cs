using System.Text.Json;
using System.Text.RegularExpressions;
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

        workspaces.Add(new WorkspaceItem { Id = idCounter++, Code = "ODA-1", Name = "Toplantı Odası 1", Type = "Oda", Zone = "Toplantı Odaları", Capacity = 6, Features = new() { "TV Ekran", "Beyaz Tahta" }, GridX = 1, GridY = 6 });
        workspaces.Add(new WorkspaceItem { Id = idCounter++, Code = "ODA-2", Name = "Toplantı Odası 2", Type = "Oda", Zone = "Toplantı Odaları", Capacity = 8, Features = new() { "Projeksiyon", "Video Konferans" }, GridX = 2, GridY = 6 });
        workspaces.Add(new WorkspaceItem { Id = idCounter++, Code = "ODA-3", Name = "Toplantı Odası 3", Type = "Oda", Zone = "Toplantı Odaları", Capacity = 4, Features = new() { "Akıllı Ekran" }, GridX = 3, GridY = 6 });
        workspaces.Add(new WorkspaceItem { Id = idCounter++, Code = "ODA-4", Name = "Büyük Konferans Odası", Type = "Oda", Zone = "Toplantı Odaları", Capacity = 12, Features = new() { "Ses Sistemi", "75\" Ekran" }, GridX = 4, GridY = 6 });

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

    // ==================== CONVERSATIONAL AI CHATBOT ENGINE ====================
    public object ProcessAiChat(AiChatRequest req)
    {
        lock (_lock)
        {
            var user = _data.Users.FirstOrDefault(u => u.Id == req.UserId) ?? _data.Users.First();
            var text = (req.Message ?? "").Trim();
            var lower = text.ToLower(new System.Globalization.CultureInfo("tr-TR"));

            // 1. Resolve Target Date from message ("bugün", "yarın", "öbür gün", YYYY-MM-DD, or fallback to selectedDate)
            string targetDate = string.IsNullOrWhiteSpace(req.SelectedDate)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : req.SelectedDate;

            if (lower.Contains("bugün") || lower.Contains("bugun"))
                targetDate = DateTime.Today.ToString("yyyy-MM-dd");
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

            // 2. Resolve TimeSlot
            string timeSlot = "Tam Gün (09:00 - 18:00)";
            if (lower.Contains("öğleden önce") || lower.Contains("sabah"))
                timeSlot = "Öğleden Önce (09:00 - 13:00)";
            else if (lower.Contains("öğleden sonra"))
                timeSlot = "Öğleden Sonra (13:30 - 18:00)";

            // 3. Detect Specific Seat/Room Code (e.g. "1A", "10B", "ODA-1", "oda 2")
            WorkspaceItem? matchedWorkspace = null;
            var roomMatch = Regex.Match(lower, @"oda[\s\-]*([1-4])");
            if (roomMatch.Success)
            {
                string code = "ODA-" + roomMatch.Groups[1].Value;
                matchedWorkspace = _data.Workspaces.FirstOrDefault(w => w.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var seatMatch = Regex.Match(lower, @"\b(10|[1-9])\s*([ab])\b");
                if (seatMatch.Success)
                {
                    string code = (seatMatch.Groups[1].Value + seatMatch.Groups[2].Value).ToUpper();
                    matchedWorkspace = _data.Workspaces.FirstOrDefault(w => w.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    var deskMatch = Regex.Match(lower, @"masa\s*(10|[1-9])\b");
                    if (deskMatch.Success)
                    {
                        int deskNo = int.Parse(deskMatch.Groups[1].Value);
                        var busyIds = _data.Reservations
                            .Where(r => r.Date == targetDate && r.Status != "Cancelled")
                            .Select(r => r.WorkspaceItemId)
                            .ToHashSet();
                        matchedWorkspace = _data.Workspaces
                            .FirstOrDefault(w => w.GridW == deskNo && !busyIds.Contains(w.Id))
                            ?? _data.Workspaces.FirstOrDefault(w => w.GridW == deskNo);
                    }
                }
            }

            // INTENT A: CHECK-IN / VERIFICATION ("check in", "check-in", "ofisteyim", "ofise geldim", "doğrula")
            if (lower.Contains("check") || lower.Contains("ofisteyim") || lower.Contains("ofise geldim") || lower.Contains("doğrula") || lower.Contains("dogrula"))
            {
                var pending = _data.Reservations
                    .Where(r => r.UserId == user.Id && r.Status == "Active")
                    .OrderBy(r => r.Date)
                    .FirstOrDefault();

                if (pending != null)
                {
                    pending.Status = "CheckedIn";
                    Save();
                    var ws = _data.Workspaces.FirstOrDefault(w => w.Id == pending.WorkspaceItemId);
                    return new
                    {
                        reply = $"Harika {user.FullName}! **{pending.Date}** tarihli **{ws?.Code} ({ws?.Name})** rezervasyonunuz için **Ofis Check-In** doğrulamanızı yaptım. Artık 'Ofistekiler' ekranında aktif olarak görünüyorsunuz. İyi çalışmalar!",
                        action = "CHECKED_IN",
                        targetDate = pending.Date,
                        suggestions = new[] { "Ofiste kimler var?", "Rezervasyonlarım neler?", "Yarın için masa ayırt" }
                    };
                }

                var alreadyChecked = _data.Reservations
                    .Where(r => r.UserId == user.Id && r.Status == "CheckedIn")
                    .OrderByDescending(r => r.Date)
                    .FirstOrDefault();

                if (alreadyChecked != null)
                {
                    var ws = _data.Workspaces.FirstOrDefault(w => w.Id == alreadyChecked.WorkspaceItemId);
                    return new
                    {
                        reply = $"Zaten **{alreadyChecked.Date}** günü için **{ws?.Code}** masasında Check-In doğrulamanız yapılmış durumda! Başka bir gün için masa ayırtmamı ister misiniz?",
                        action = "NONE",
                        targetDate = alreadyChecked.Date,
                        suggestions = new[] { "Yarın için boş masa ayırt", "Ofiste kimler var?" }
                    };
                }

                return new
                {
                    reply = "Şu anda Check-In bekleyen aktif bir masa rezervasyonunuz görünmüyor. Hemen bugün için size boş bir masa ayırtıp Check-In yapmamı ister misiniz?",
                    action = "NONE",
                    targetDate = targetDate,
                    suggestions = new[] { "Bugün için boş masa ayırt", "Boş koltuklar hangileri?" }
                };
            }

            // INTENT B: CANCEL RESERVATION ("iptal", "vazgeç", "sil")
            if (lower.Contains("iptal") || lower.Contains("vazgeç") || lower.Contains("sil"))
            {
                var activeUserRes = _data.Reservations
                    .Where(r => r.UserId == user.Id && r.Status != "Cancelled")
                    .OrderByDescending(r => r.Date)
                    .FirstOrDefault();

                if (activeUserRes != null)
                {
                    activeUserRes.Status = "Cancelled";
                    Save();
                    var ws = _data.Workspaces.FirstOrDefault(w => w.Id == activeUserRes.WorkspaceItemId);
                    return new
                    {
                        reply = $"**{activeUserRes.Date}** tarihli **{ws?.Code} ({ws?.Name})** rezervasyonunuzu iptal ettim ve koltuğu tekrar boşa çıkardım.",
                        action = "CANCELLED",
                        targetDate = activeUserRes.Date,
                        suggestions = new[] { "Bugün boş masa ayırt", "Yarın boş koltuklar hangileri?" }
                    };
                }

                return new
                {
                    reply = "İptal edilebilecek aktif bir rezervasyonunuz bulunmuyor.",
                    action = "NONE",
                    targetDate = targetDate,
                    suggestions = new[] { "Bana bugün boş masa ayırt", "Yarın için masa ayırt" }
                };
            }

            // INTENT C: WHO IS IN THE OFFICE ("kimler var", "kim var", "ofistekiler")
            if (lower.Contains("kimler") || lower.Contains("kim var") || lower.Contains("ofisteki"))
            {
                var checkedInPeople = _data.Reservations
                    .Where(r => r.Date == targetDate && r.Status == "CheckedIn")
                    .Select(r =>
                    {
                        var u = _data.Users.FirstOrDefault(x => x.Id == r.UserId);
                        var w = _data.Workspaces.FirstOrDefault(x => x.Id == r.WorkspaceItemId);
                        return $"{u?.FullName} ({w?.Code})";
                    })
                    .ToList();

                if (checkedInPeople.Count == 0)
                {
                    return new
                    {
                        reply = $"**{targetDate}** tarihinde henüz ofise gelip Check-In yapmış kimse görünmüyor.",
                        action = "NONE",
                        targetDate = targetDate,
                        suggestions = new[] { "Bugün için masa ayırt", "Check-In yap" }
                    };
                }

                return new
                {
                    reply = $"**{targetDate}** tarihinde ofiste Check-In yapmış **{checkedInPeople.Count} kişi** var: " + string.Join(", ", checkedInPeople) + ".",
                    action = "SHOW_PRESENCE",
                    targetDate = targetDate,
                    suggestions = new[] { "Bana da boş masa ayırt", "Boş koltuklar hangileri?" }
                };
            }

            // INTENT D: MY RESERVATIONS ("rezervasyonlarım", "masam hangisi", "nerede oturuyorum")
            if (lower.Contains("rezervasyonlarım") || lower.Contains("masam ne") || lower.Contains("nerede otur"))
            {
                var myList = _data.Reservations
                    .Where(r => r.UserId == user.Id && r.Status != "Cancelled")
                    .OrderBy(r => r.Date)
                    .Select(r =>
                    {
                        var w = _data.Workspaces.FirstOrDefault(x => x.Id == r.WorkspaceItemId);
                        var st = r.Status == "CheckedIn" ? "✅ Check-In Yapıldı" : "⏳ Check-In Bekleniyor";
                        return $"{r.Date}: {w?.Code} ({st})";
                    })
                    .ToList();

                if (myList.Count == 0)
                {
                    return new
                    {
                        reply = "Henüz aktif bir masa veya oda rezervasyonunuz bulunmuyor. İsterseniz hemen ayırtabilirim!",
                        action = "NONE",
                        targetDate = targetDate,
                        suggestions = new[] { "Bugün için boş masa ayırt", "Yarın için 4A koltuğunu ayırt" }
                    };
                }

                return new
                {
                    reply = "Aktif rezervasyonlarınız:\n• " + string.Join("\n• ", myList),
                    action = "NONE",
                    targetDate = targetDate,
                    suggestions = new[] { "Ofise geldim, Check-In yap", "Son rezervasyonumu iptal et" }
                };
            }

            // INTENT E: LIST AVAILABLE SEATS ("boş", "müsait", "hangileri") WITHOUT BOOKING INTENT
            bool hasBookingVerb = lower.Contains("ayırt") || lower.Contains("ayirt") || lower.Contains("rezerv") || lower.Contains("ayır") || lower.Contains("istiyorum") || lower.Contains("al ") || lower.Contains("seç") || matchedWorkspace != null;
            if ((lower.Contains("boş") || lower.Contains("bos") || lower.Contains("müsait") || lower.Contains("musait") || lower.Contains("hangi")) && !hasBookingVerb)
            {
                var reservedIds = _data.Reservations
                    .Where(r => r.Date == targetDate && r.Status != "Cancelled")
                    .Select(r => r.WorkspaceItemId)
                    .ToHashSet();

                var freeDesks = _data.Workspaces
                    .Where(w => w.IsActive && w.Type == "Masa" && !reservedIds.Contains(w.Id))
                    .Select(w => w.Code)
                    .ToList();

                var freeRooms = _data.Workspaces
                    .Where(w => w.IsActive && w.Type == "Oda" && !reservedIds.Contains(w.Id))
                    .Select(w => w.Code)
                    .ToList();

                var topSuggestions = freeDesks.Take(3).Select(c => $"{targetDate} için {c} koltuğunu ayırt").ToList();
                return new
                {
                    reply = $"**{targetDate}** tarihinde **{freeDesks.Count} boş koltuk** ({string.Join(", ", freeDesks.Take(8))}...) ve **{freeRooms.Count} boş toplantı odası** ({string.Join(", ", freeRooms)}) müsait. Hangisini sizin için ayırtmamı istersiniz?",
                    action = "DATE_CHANGED",
                    targetDate = targetDate,
                    suggestions = topSuggestions
                };
            }

            // INTENT F: BOOK / RESERVE A SEAT OR ROOM (Specific or Auto-Select Available)
            if (hasBookingVerb || lower.Contains("masa") || lower.Contains("koltuk") || lower.Contains("oda"))
            {
                var reservedIds = _data.Reservations
                    .Where(r => r.Date == targetDate && r.Status != "Cancelled")
                    .Select(r => r.WorkspaceItemId)
                    .ToHashSet();

                WorkspaceItem? targetWs = matchedWorkspace;

                if (targetWs != null && reservedIds.Contains(targetWs.Id))
                {
                    var alternatives = _data.Workspaces
                        .Where(w => w.IsActive && w.Type == targetWs.Type && !reservedIds.Contains(w.Id))
                        .Take(3)
                        .Select(w => w.Code)
                        .ToList();

                    return new
                    {
                        reply = $"Üzgünüm, **{targetDate}** tarihinde **{targetWs.Code}** zaten dolu. Ancak şu alternatifler boş: **{string.Join(", ", alternatives)}**. Bunlardan birini ayırtmamı ister misiniz?",
                        action = "NONE",
                        targetDate = targetDate,
                        suggestions = alternatives.Select(c => $"{targetDate} için {c} koltuğunu ayırt").ToArray()
                    };
                }

                if (targetWs == null)
                {
                    bool wantsRoom = lower.Contains("oda") || lower.Contains("toplantı");
                    targetWs = _data.Workspaces
                        .FirstOrDefault(w => w.IsActive && w.Type == (wantsRoom ? "Oda" : "Masa") && !reservedIds.Contains(w.Id));
                }

                if (targetWs == null)
                {
                    return new
                    {
                        reply = $"**{targetDate}** tarihinde hiç boş yer kalmamış görünüyor. Başka bir gün seçmek ister misiniz?",
                        action = "NONE",
                        targetDate = targetDate,
                        suggestions = new[] { "Yarın için boş masa ayırt", "Öbür gün için masa ayırt" }
                    };
                }

                var createRes = CreateReservation(new CreateReservationRequest
                {
                    UserId = user.Id,
                    WorkspaceItemId = targetWs.Id,
                    Date = targetDate,
                    TimeSlot = timeSlot,
                    Note = "RAY AI Asistan ile ayrıldı"
                });

                return new
                {
                    reply = $"Tamamdır {user.FullName}! **{targetDate}** tarihi ({timeSlot}) için **{targetWs.Code} — {targetWs.Name}** koltuğunu sizin adınıza ayırttım. Ofise ulaştığınızda bana **'Ofise geldim, Check-In yap'** yazarak doğrulayabilirsiniz!",
                    action = "RESERVED",
                    targetDate = targetDate,
                    reservedCode = targetWs.Code,
                    suggestions = new[] { "Ofise geldim, Check-In yap", "Rezervasyonlarım neler?", "Rezervasyonumu iptal et" }
                };
            }

            // DEFAULT GREETING / HELP
            return new
            {
                reply = $"Merhaba {user.FullName}! Ben **RAY Yapay Zeka Asistanınızım**. Benimle konuşarak şunları anında yapabilirsiniz:\n• **\"Bana bugün boş bir masa ayırt\"** veya **\"Yarın 4A koltuğunu rezerve et\"**\n• **\"Ofise geldim, Check-In yap\"** (Masanızı doğrular)\n• **\"Bugün ofiste kimler var?\"** veya **\"Yarın hangi koltuklar boş?\"**",
                action = "NONE",
                targetDate = targetDate,
                suggestions = new[] { "Bana bugün boş masa ayırt", "Ofise geldim, Check-In yap", "Bugün ofiste kimler var?", "Yarın boş koltuklar hangileri?" }
            };
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
