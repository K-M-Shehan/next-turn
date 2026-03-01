using NextTurn.API.Middleware;
using NextTurn.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── Register services ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Registers DbContext, repositories, password hasher, HttpTenantContext, etc.
builder.Services.AddInfrastructure(builder.Configuration);

// ── Build ─────────────────────────────────────────────────────────────────────
WebApplication app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Resolve TenantId from JWT claim 'tid' or X-Tenant-Id header.
// Must run after UseAuthentication (once JWT auth is added) so that
// User.Claims is already populated before this middleware reads them.
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();
