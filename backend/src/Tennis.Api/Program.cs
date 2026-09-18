using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Tennis.Api.Auth;
using Tennis.Api.Endpoints;
using Tennis.Application;
using Tennis.Application.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddScoped<CurrentUserResolver>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigin = builder.Configuration["Frontend:Origin"];

        if (string.IsNullOrWhiteSpace(allowedOrigin))
        {
            policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
            return;
        }

        policy.WithOrigins(allowedOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var auth0Domain = builder.Configuration["Auth0:Domain"]
    ?? throw new InvalidOperationException("Auth0 domain was not configured.");
var auth0Audience = builder.Configuration["Auth0:Audience"]
    ?? throw new InvalidOperationException("Auth0 audience was not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = auth0Audience;
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
    return Results.Ok(new { status = "ok", database = canConnect ? "reachable" : "unreachable" });
}).AllowAnonymous();

app.MapGet("/api/me", async (
    HttpContext httpContext,
    CurrentUserResolver currentUserResolver,
    CancellationToken cancellationToken) =>
{
    var currentUser = await currentUserResolver.ResolveAsync(httpContext, cancellationToken);

    return Results.Ok(new
    {
        currentUser.Id,
        currentUser.Email,
        currentUser.DisplayName,
        currentUser.Subject,
        currentUser.Provider
    });
});

app.MapLadderEndpoints();

app.Run();
