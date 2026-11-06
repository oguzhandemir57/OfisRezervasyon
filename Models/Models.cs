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

public class WorkspaceItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Masa"; // "Masa", "Oda", "Kabin"
    public string Zone { get; set; } = string.Empty;
    public int Capacity { get; set; } = 1;
    public List<string> Features { get; set; } = new();
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridW { get; set; } = 1;
    public int GridH { get; set; } = 1;
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
}
