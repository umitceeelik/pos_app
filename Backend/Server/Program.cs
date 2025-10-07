using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Server;                  // AppSettings
using Server.Common;
using Server.Domain.Entities;  // Seed entities
using Server.Endpoints;        // MapRooms/MapSessions
using Server.Hubs;             // SignalR hub
using Server.Infrastructure;   // AppDb
using Server.Services;         // IReceiptPrinter + PreviewReceiptPrinter

var builder = WebApplication.CreateBuilder(args);

//
// -----------------------
// 1) CONFIG & SERVICES
// -----------------------
//

// Bind strongly-typed settings from appsettings.json (Printer settings etc.)
builder.Services.Configure<AppSettings>(builder.Configuration);

// Database: EF Core + SQLite (simple, robust for LAN/offline-first)
builder.Services.AddDbContext<AppDb>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=app.db"));

// Real-time: SignalR for push updates to all connected clients
builder.Services.AddSignalR();

// CORS: allow LAN/mobile during development (restrict in production!)
builder.Services.AddCors(o => o.AddPolicy("client", p =>
    p.AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()
     .SetIsOriginAllowed(_ => true)));

// Swagger/OpenAPI: interactive API docs (dev only UI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register validators from this assembly
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Receipt printer DI
// PREVIEW printer writes files; later you can switch to LAN/USB by changing AppSettings.Printer.Mode
builder.Services.AddSingleton<IReceiptPrinter>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<AppSettings>>().Value;
    return cfg.Printer.Mode.ToUpperInvariant() switch
    {
        "LAN" => new EscPosTcpPrinter(sp.GetRequiredService<IOptions<AppSettings>>()),
        // "USB" => new UsbRawPrinter(...), // future
        _ => new PreviewReceiptPrinter(sp.GetRequiredService<IOptions<AppSettings>>()),
    };
});

// Register the generic validation filter so we can add it per-endpoint
builder.Services.AddSingleton(typeof(ValidateRequestFilter<>));

//
// -----------------------
// 2) BUILD APP
// -----------------------
//

var app = builder.Build();

//
// -----------------------
// 3) MIDDLEWARE PIPELINE
// -----------------------
//

// Global exception handler -> returns ProblemDetails JSON on unhandled errors
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        ctx.Response.ContentType = "application/problem+json";
        var feature = ctx.Features.Get<IExceptionHandlerPathFeature>();
        var problem = new
        {
            type = "about:blank",
            title = "Unexpected error",
            status = 500,
            // In production you should hide details:
            detail = app.Environment.IsDevelopment() ? feature?.Error.ToString() : "An unexpected error occurred."
        };
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(problem);
    });
});

// Allow cross-origin requests from LAN/mobile during development
app.UseCors("client");

// Swagger UI only for development (avoid exposing in production)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Optional: enforce invariant culture & UTC across the app
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

//
// -----------------------
// 4) DATABASE MIGRATION & SEED
// -----------------------
//

// Apply migrations and seed initial data at startup (safe for small apps)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    await db.Database.MigrateAsync();

    // Seed default rooms if database is empty (idempotent)
    if (!await db.Rooms.AnyAsync())
    {
        db.Rooms.AddRange(new[]
        {
            new Room { Name = "Hot Room 1" },
            new Room { Name = "Hot Room 2" },
            new Room { Name = "Hot Room 3" },
            new Room { Name = "Massage 1" },
            new Room { Name = "Massage 2" },
            new Room { Name = "Scrub 1" }
        });
        await db.SaveChangesAsync();
    }
}

//
// -----------------------
// 5) ENDPOINTS
// -----------------------
//

// Simple health-check
app.MapGet("/health", () => Results.Ok(new { ok = true }))
   .WithSummary("Health Check")
   .WithDescription("Returns 200 OK if the service is up.");

// Readiness probe: pings DB quickly (useful for docker/monitoring)
app.MapGet("/ready", async (Server.Infrastructure.AppDb db, CancellationToken ct) =>
{
    // a lightweight query against Rooms table (or PRAGMA quick check)
    var any = await db.Rooms.AsNoTracking().AnyAsync(ct);
    return Results.Ok(new { ready = true, rooms = any });
})
.WithSummary("Readiness check")
.WithDescription("Quick DB ping to verify the service is fully ready.");

// Map report endpoints
app.MapReports();

// Real-time hub for rooms/sessions updates
app.MapHub<RoomsHub>("/hubs/rooms");

// REST APIs (defined in extension methods)
app.MapRooms();
app.MapSessions();

//
// -----------------------
// 6) RUN SERVER
// -----------------------
//

app.Run();
