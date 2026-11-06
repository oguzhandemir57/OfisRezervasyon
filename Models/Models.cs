namespace RayOfisRezervasyon.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee"; // "Admin" or "Employee"
    public string AvatarInitials { get; set; } = string.Empty;
}

public class OfficeFloor
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // "KAT-1", "KAT-2"
    public string Name { get; set; } = string.Empty; // "1. Kat — A Blok Açık Ofis"
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true; // Kullanıma Açık / Kapalı
}

public class WorkspaceItem
{
    public int Id { get; set; }
    public int FloorId { get; set; } = 1;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Masa"; // "Masa", "Oda", "Kabin"
    public string Zone { get; set; } = string.Empty;
    public int Capacity { get; set; } = 1;
    public List<string> Features { get; set; } = new();
    public string Orientation { get; set; } = "horizontal"; // "horizontal" (Yatay) or "vertical" (Dikine)
    public int GridX { get; set; } // 1 = Sol Cephe, 2 = Sağ Cephe
    public int GridY { get; set; } // Row number (1..N)
    public int GridW { get; set; } = 1; // DeskNo
    public int GridH { get; set; } = 1; // SeatIndex (1..M)
    public int CanvasX { get; set; } = -1; // 2D Square Canvas X% (0..82)
    public int CanvasY { get; set; } = -1; // 2D Square Canvas Y% (0..82)
    public bool IsActive { get; set; } = true;
}

public class Reservation
{
    public int Id { get; set; }
    public int WorkspaceItemId { get; set; }
    public int UserId { get; set; }
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD
    public string TimeSlot { get; set; } = "Tam Gün (09:00 - 18:00)";
    public string Note { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // "Active", "CheckedIn", "Cancelled"
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CreateReservationRequest
{
    public int WorkspaceItemId { get; set; }
    public int UserId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = "Tam Gün (09:00 - 18:00)";
    public string Note { get; set; } = string.Empty;
    public bool ReplaceExisting { get; set; } = false;
}

public class SaveWorkspaceRequest
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Masa";
    public string Zone { get; set; } = "A Blok - Açık Ofis";
    public int Capacity { get; set; } = 1;
    public List<string> Features { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class AiChatRequest
{
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SelectedDate { get; set; } = string.Empty;
    public bool ReplaceExisting { get; set; } = false;
    public string? GeminiApiKey { get; set; }
}

public class AiActionButton
{
    public string Label { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Style { get; set; } = "blue"; // "blue", "red", "outline"
    public bool ReplaceExisting { get; set; } = false;
}

public class CreateFloorRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int InitialDeskCount { get; set; } = 4;
    public int SeatsPerDesk { get; set; } = 2;
    public string DefaultOrientation { get; set; } = "horizontal"; // "horizontal" or "vertical"
    public bool IncludeMeetingRoom { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class ToggleFloorStatusRequest
{
    public int FloorId { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateDeskGroupRequest
{
    public int FloorId { get; set; } = 1;
    public int DeskNo { get; set; } = 1;
    public int SeatCount { get; set; } = 2; // 1..8 seats
    public string Orientation { get; set; } = "horizontal"; // "horizontal" or "vertical"
    public int GridX { get; set; } = 1; // 1 = Sol Cephe, 2 = Sağ Cephe
    public int GridY { get; set; } = 1; // Row index
    public int? CanvasX { get; set; }
    public int? CanvasY { get; set; }
    public List<string>? Features { get; set; }
}

public class AddDeskGroupRequest
{
    public int FloorId { get; set; } = 1;
    public int SeatCount { get; set; } = 2;
    public string Orientation { get; set; } = "horizontal";
    public int GridX { get; set; } = 1;
    public int? CanvasX { get; set; }
    public int? CanvasY { get; set; }
    public List<string>? Features { get; set; }
}

public class DeleteDeskGroupRequest
{
    public int FloorId { get; set; } = 1;
    public int DeskNo { get; set; } = 1;
}
