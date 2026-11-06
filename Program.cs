using RayOfisRezervasyon.Models;
using RayOfisRezervasyon.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ReservationStore>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Auth Endpoint
app.MapPost("/api/auth/login", (LoginRequest req, ReservationStore store) =>
{
    var user = store.Authenticate(req.Username, req.Password);
    if (user == null)
    {
        return Results.Json(new { success = false, message = "Kullanıcı adı veya şifre hatalı!" }, statusCode: 401);
    }

    return Results.Ok(new
    {
        success = true,
        message = $"Hoş geldiniz, {user.FullName}!",
        user = new
        {
            user.Id,
            user.Username,
            user.FullName,
            user.Department,
            user.Title,
            user.Role,
            user.AvatarInitials
        }
    });
});

// Get Demo Users
app.MapGet("/api/users", (ReservationStore store) =>
{
    return Results.Ok(store.GetUsers());
});

// Get Workspaces (Desks, Rooms, Pods) with availability for a specific date
app.MapGet("/api/workspaces", (string? date, ReservationStore store) =>
{
    var targetDate = string.IsNullOrWhiteSpace(date)
        ? DateTime.Today.ToString("yyyy-MM-dd")
        : date;
    return Results.Ok(store.GetWorkspacesForDate(targetDate));
});

// Create a Reservation for 1 day on a desk or room
app.MapPost("/api/reservations", (CreateReservationRequest req, ReservationStore store) =>
{
    var result = store.CreateReservation(req);
    return Results.Ok(result);
});

// Check-In to a Reservation
app.MapPost("/api/reservations/{id:int}/checkin", (int id, int userId, ReservationStore store) =>
{
    var result = store.CheckInReservation(id, userId);
    if (!result.Success)
    {
        return Results.BadRequest(new { success = false, message = result.Message });
    }
    return Results.Ok(new { success = true, message = result.Message });
});

// Cancel a Reservation
app.MapDelete("/api/reservations/{id:int}", (int id, int userId, ReservationStore store) =>
{
    var result = store.CancelReservation(id, userId);
    if (!result.Success)
    {
        return Results.BadRequest(new { success = false, message = result.Message });
    }
    return Results.Ok(new { success = true, message = result.Message });
});

// Get Logged-in User's Reservations
app.MapGet("/api/my-reservations", (int userId, ReservationStore store) =>
{
    return Results.Ok(store.GetUserReservations(userId));
});

// Admin: Save or Update Workspace Item (Desk/Room)
app.MapPost("/api/admin/workspaces", (SaveWorkspaceRequest req, ReservationStore store) =>
{
    var result = store.SaveWorkspace(req);
    if (!result.Success)
    {
        return Results.BadRequest(new { success = false, message = result.Message });
    }
    return Results.Ok(new { success = true, message = result.Message });
});

// Admin: Get Dashboard Metrics & All Reservations
app.MapGet("/api/admin/dashboard", (string? date, ReservationStore store) =>
{
    var targetDate = string.IsNullOrWhiteSpace(date)
        ? DateTime.Today.ToString("yyyy-MM-dd")
        : date;
    return Results.Ok(store.GetAdminDashboard(targetDate));
});

// AI Chatbot Endpoint (Gemini Free AI + Conversational Reservation & Check-In Assistant)
app.MapPost("/api/ai/chat", async (AiChatRequest req, ReservationStore store) =>
{
    var result = await store.ProcessAiChatAsync(req);
    return Results.Ok(result);
});

app.Run();
