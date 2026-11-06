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

            // RULE: 1 user can reserve at most 1 desk ("Masa") per day. If they try a 2nd desk, ask if they want to change!
            string? replacedOldCode = null;
            if (workspace.Type == "Masa")
            {
                var userSameDayDesk = _data.Reservations
                    .Where(r => r.UserId == req.UserId && r.Date == req.Date && r.Status != "Cancelled")
                    .Select(r => new { Res = r, Ws = _data.Workspaces.FirstOrDefault(w => w.Id == r.WorkspaceItemId) })
                    .FirstOrDefault(x => x.Ws != null && x.Ws.Type == "Masa");

                if (userSameDayDesk != null)
                {
                    if (!req.ReplaceExisting)
                    {
                        return new
                        {
                            success = false,
                            requiresChangeConfirmation = true,
                            existingReservationId = userSameDayDesk.Res.Id,
                            existingCode = userSameDayDesk.Ws!.Code,
                            existingName = userSameDayDesk.Ws.Name,
                            newWorkspaceId = workspace.Id,
                            newCode = workspace.Code,
                            newName = workspace.Name,
                            date = req.Date,
                            timeSlot = req.TimeSlot,
                            message = $"{req.Date} tarihinde zaten {userSameDayDesk.Ws.Code} masasında rezervasyonunuz bulunuyor. Mevcut masanızı {workspace.Code} ile değiştirmek ister misiniz?"
                        };
                    }
                    else
                    {
                        replacedOldCode = userSameDayDesk.Ws!.Code;
                        userSameDayDesk.Res.Status = "Cancelled";
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
                ? $"{req.Date} tarihli masa rezervasyonunuz {replacedOldCode} yerine {workspace.Code} ({workspace.Name}) olarak başarıyla değiştirildi!"
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

            res.Status = "CheckedIn";
            Save();
            var ws = _data.Workspaces.FirstOrDefault(w => w.Id == res.WorkspaceItemId);
            return (true, $"{ws?.Code ?? "Masa"} için Ofis Check-In doğrulamanız tamamlandı! Artık haritada doğrulanmış ikonuyla ve Ofistekiler sayfasında görünüyorsunuz.");
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

            string timeSlot = "Tam Gün (09:00 - 18:00)";
            if (lower.Contains("öğleden önce") || lower.Contains("sabah"))
                timeSlot = "Öğleden Önce (09:00 - 13:00)";
            else if (lower.Contains("öğleden sonra"))
                timeSlot = "Öğleden Sonra (13:30 - 18:00)";

            if (lower.Contains("vazgeç") && !lower.Contains("iptal"))
            {
                return new
                {
                    replyHtml = "İşlemden vazgeçildi. Mevcut rezervasyonunuz korundu. Başka bir konuda yardımcı olabilir miyim?",
                    action = "NONE",
                    targetDate = targetDate,
                    buttons = new List<AiActionButton>
                    {
                        new() { Label = "📅 Rezervasyonlarımı Göster", Command = "Rezervasyonlarım neler?", Style = "outline" },
                        new() { Label = "🏢 Ofiste Kimler Var?", Command = "Bugün ofiste kimler var?", Style = "outline" }
                    }
                };
            }

            // Detect Specific Seat/Room Code (e.g. "1A", "10B", "ODA-1")
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
            }

            // INTENT A: CHECK-IN / VERIFICATION
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
                        replyHtml = $"✅ Harika <strong>{user.FullName}</strong>! <strong>{pending.Date}</strong> tarihli <strong>{ws?.Code} ({ws?.Name})</strong> rezervasyonunuz için <strong>Ofis Check-In</strong> doğrulamanızı tamamladım.",
                        action = "CHECKED_IN",
                        targetDate = pending.Date,
                        showPopup = true,
                        popupTitle = "Ofis Check-In Doğrulandı!",
                        popupMessage = $"{ws?.Code} ({ws?.Name}) masası için ofiste olduğunuz doğrulandı. Masanızda artık doğrulama (✓) ikonu görünmektedir.",
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = "🏢 Ofiste Kimler Var?", Command = "Bugün ofiste kimler var?", Style = "blue" },
                            new() { Label = "📅 Rezervasyonlarım", Command = "Rezervasyonlarım neler?", Style = "outline" }
                        }
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
                        replyHtml = $"Zaten <strong>{alreadyChecked.Date}</strong> günü için <strong>{ws?.Code}</strong> masasında Check-In doğrulamanız yapılmış durumda! Ne yapmak istersiniz?",
                        action = "NONE",
                        targetDate = alreadyChecked.Date,
                        buttons = new List<AiActionButton>
                        {
                            new() { Label = "✨ Yarın İçin Boş Masa Ayırt", Command = "Yarın için bana boş masa ayırt", Style = "blue" },
                            new() { Label = "🏢 Ofiste Kimler Var?", Command = "Bugün ofiste kimler var?", Style = "outline" }
                        }
                    };
                }

                return new
                {
                    replyHtml = "Şu anda Check-In bekleyen aktif bir masa rezervasyonunuz görünmüyor. Hemen bugün için boş masaları listelememi ister misiniz?",
                    action = "NONE",
                    targetDate = targetDate,
                    buttons = new List<AiActionButton>
                    {
                        new() { Label = "✨ Bugün Boş Masaları Göster", Command = "Bana bugün boş masa ayırt", Style = "blue" },
                        new() { Label = "📅 Yarın Boş Masaları Göster", Command = "Yarın için bana boş masa ayırt", Style = "outline" }
                    }
                };
            }

            // INTENT B: CANCEL RESERVATION
            if (lower.Contains("iptal") || lower.Contains("sil"))
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
                        replyHtml = $"🗑️ <strong>{activeUserRes.Date}</strong> tarihli <strong>{ws?.Code} ({ws?.Name})</strong> rezervasyonunuzu iptal ettim.",
                        action = "CANCELLED",
                        targetDate = activeUserRes.Date,
                        showPopup = true,
                        popupTitle = "Rezervasyon İptal Edildi",
                        popupMessage = $"{activeUserRes.Date} tarihli {ws?.Code} rezervasyonunuz iptal edildi ve koltuk boşa çıkarıldı.",
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
            if (lower.Contains("kimler") || lower.Contains("kim var") || lower.Contains("ofisteki"))
            {
                var checkedInPeople = _data.Reservations
                    .Where(r => r.Date == targetDate && r.Status == "CheckedIn")
                    .Select(r =>
                    {
                        var u = _data.Users.FirstOrDefault(x => x.Id == r.UserId);
                        var w = _data.Workspaces.FirstOrDefault(x => x.Id == r.WorkspaceItemId);
                        return $"<strong>{u?.FullName}</strong> (<strong>{w?.Code}</strong>)";
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
            if (lower.Contains("rezervasyonlarım") || lower.Contains("masam ne") || lower.Contains("nerede otur"))
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
                        replyHtml = "Henüz aktif bir masa veya oda rezervasyonunuz bulunmuyor. Hangi gün için masa seçmek istersiniz?",
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

            // INTENT E: SPECIFIC SEAT/ROOM SELECTION (e.g. User clicked a button "2026-11-06 için 4A koltuğunu ayırt" or typed "4A koltuğunu ayırt")
            var reservedIds = _data.Reservations
                .Where(r => r.Date == targetDate && r.Status != "Cancelled")
                .Select(r => r.WorkspaceItemId)
                .ToHashSet();

            if (matchedWorkspace != null)
            {
                if (reservedIds.Contains(matchedWorkspace.Id))
                {
                    var alts = _data.Workspaces
                        .Where(w => w.IsActive && w.Type == matchedWorkspace.Type && !reservedIds.Contains(w.Id))
                        .ToList();

                    var altBold = string.Join(", ", alts.Select(a => $"<strong>{a.Code}</strong>"));
                    return new
                    {
                        replyHtml = $"Üzgünüm, <strong>{targetDate}</strong> tarihinde <strong>{matchedWorkspace.Code}</strong> dolu.<br><br>Müsait olan numaralar: {altBold}<br>Hangi masayı istersiniz? Seçin:",
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

                // Check if user already has a desk on targetDate
                if (matchedWorkspace.Type == "Masa")
                {
                    var existingSameDay = _data.Reservations
                        .Where(r => r.UserId == user.Id && r.Date == targetDate && r.Status != "Cancelled")
                        .Select(r => new { Res = r, Ws = _data.Workspaces.FirstOrDefault(w => w.Id == r.WorkspaceItemId) })
                        .FirstOrDefault(x => x.Ws != null && x.Ws.Type == "Masa");

                    if (existingSameDay != null && !req.ReplaceExisting && !lower.Contains("değiştir") && !lower.Contains("degistir"))
                    {
                        return new
                        {
                            replyHtml = $"⚠️ <strong>{targetDate}</strong> tarihinde zaten <strong>{existingSameDay.Ws!.Code}</strong> masasında rezervasyonunuz bulunuyor.<br><br>Bir günde 1 masa rezerve edebilirsiniz. Mevcut masanızı <strong>{matchedWorkspace.Code}</strong> ile <strong>değiştirmek ister misiniz?</strong>",
                            action = "ASK_CHANGE",
                            targetDate = targetDate,
                            buttons = new List<AiActionButton>
                            {
                                new() { Label = $"🔄 Evet, {matchedWorkspace.Code} İle Değiştir", Command = $"{targetDate} için {matchedWorkspace.Code} koltuğunu değiştir", Style = "blue", ReplaceExisting = true },
                                new() { Label = $"✕ Hayır, {existingSameDay.Ws.Code} Kalsın", Command = "Değiştirmekten vazgeç", Style = "outline" }
                            }
                        };
                    }
                }

                bool shouldReplace = req.ReplaceExisting || lower.Contains("değiştir") || lower.Contains("degistir");
                if (shouldReplace && matchedWorkspace.Type == "Masa")
                {
                    var oldRes = _data.Reservations
                        .Where(r => r.UserId == user.Id && r.Date == targetDate && r.Status != "Cancelled")
                        .Select(r => new { Res = r, Ws = _data.Workspaces.FirstOrDefault(w => w.Id == r.WorkspaceItemId) })
                        .FirstOrDefault(x => x.Ws != null && x.Ws.Type == "Masa");
                    if (oldRes != null)
                        oldRes.Res.Status = "Cancelled";
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

                return new
                {
                    replyHtml = $"🎉 Tamamdır <strong>{user.FullName}</strong>! <strong>{targetDate}</strong> tarihi ({timeSlot}) için <strong>{matchedWorkspace.Code} — {matchedWorkspace.Name}</strong> sizin adınıza ayrıldı!",
                    action = "RESERVED",
                    targetDate = targetDate,
                    showPopup = true,
                    popupTitle = shouldReplace ? "Masa Değişikliği Tamamlandı!" : "Masa Rezervasyonu Tamamlandı!",
                    popupMessage = $"{targetDate} günü ({timeSlot}) için {matchedWorkspace.Code} ({matchedWorkspace.Name}) masası adınıza ayrıldı.",
                    reservedItem = new
                    {
                        code = matchedWorkspace.Code,
                        name = matchedWorkspace.Name,
                        date = targetDate,
                        timeSlot = timeSlot
                    },
                    buttons = new List<AiActionButton>
                    {
                        new() { Label = "📍 Ofise Geldim — Check-In Yap", Command = "Ofise geldim, Check-In yap", Style = "blue" },
                        new() { Label = "🗑️ Rezervasyonu İptal Et", Command = "Son rezervasyonumu iptal et", Style = "red" }
                    }
                };
            }

            // INTENT F: USER ASKS "Bana boş masa ayırt" / "Boş masalar hangileri?" -> LIST AVAILABLE DESKS IN BOLD & GIVE SELECTION BUTTONS!
            if (lower.Contains("masa") || lower.Contains("koltuk") || lower.Contains("boş") || lower.Contains("bos") || lower.Contains("ayırt") || lower.Contains("ayirt") || lower.Contains("rezerv") || lower.Contains("oda"))
            {
                bool wantsRoom = lower.Contains("oda") || lower.Contains("toplantı");
                var freeItems = _data.Workspaces
                    .Where(w => w.IsActive && w.Type == (wantsRoom ? "Oda" : "Masa") && !reservedIds.Contains(w.Id))
                    .ToList();

                if (freeItems.Count == 0)
                {
                    return new
                    {
                        replyHtml = $"<strong>{targetDate}</strong> tarihinde boş {(wantsRoom ? "oda" : "masa")} kalmamış görünüyor. Yarın için bakmamı ister misiniz?",
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
                    replyHtml = $"📅 <strong>{targetDate}</strong> tarihinde müsait olan boş {(wantsRoom ? "oda" : "masa")} numaraları:<br><br>{boldList}<br><br><strong>Hangi {(wantsRoom ? "odayı" : "masayı")} istersiniz? Aşağıdaki butonlardan seçin:</strong>",
                    action = "LIST_OPTIONS",
                    targetDate = targetDate,
                    buttons = seatButtons
                };
            }

            // DEFAULT GREETING WITH ACTION BUTTONS
            return new
            {
                replyHtml = $"Merhaba <strong>{user.FullName}</strong>! Ben <strong>RAY Yapay Zeka Asistanınızım</strong>.<br>Yapmak istediğiniz işlemi aşağıdaki butonlardan seçebilir veya doğrudan yazabilirsiniz:",
                action = "NONE",
                targetDate = targetDate,
                buttons = new List<AiActionButton>
                {
                    new() { Label = "✨ Bana Bugün Boş Masa Ayırt", Command = "Bana bugün boş masa ayırt", Style = "blue" },
                    new() { Label = "📅 Yarın İçin Boş Masa Ayırt", Command = "Yarın için bana boş masa ayırt", Style = "outline" },
                    new() { Label = "📍 Ofise Geldim, Check-In Yap", Command = "Ofise geldim, Check-In yap", Style = "blue" },
                    new() { Label = "🏢 Bugün Ofiste Kimler Var?", Command = "Bugün ofiste kimler var?", Style = "outline" }
                }
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
