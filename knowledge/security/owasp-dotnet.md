# OWASP .NET Security Guide

## Top Threats & Mitigations

### 1. SQL Injection

```csharp
// ❌ VULNERABLE
var sql = $"SELECT * FROM Users WHERE name = '{input}'";

// ✅ SAFE — Parameterized query
var user = await _context.Users
    .Where(u => u.Name == input)
    .FirstOrDefaultAsync();

// ✅ SAFE — Raw SQL with parameters
var users = await _context.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Name = {0}", input)
    .ToListAsync();
```

### 2. Sensitive Data Exposure

```csharp
// ❌ Never log or expose secrets
_logger.LogInformation("Password: {pwd}", userPassword);

// ✅ Use Secret Manager in development
// dotnet user-secrets set "ConnectionStrings:Db" "..."

// ✅ Use Azure Key Vault in production
builder.Configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential());
```

### 3. Insecure Direct Object Reference (IDOR)

```csharp
// ❌ Trusts client-provided ID without ownership check
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id)
    => Ok(await _repo.GetAsync(id));

// ✅ Always scope to the current user
[HttpGet("{id}"), Authorize]
public async Task<IActionResult> Get(Guid id)
{
    var userId = User.GetUserId();
    var resource = await _repo.GetAsync(id);
    if (resource?.OwnerId != userId) return Forbid();
    return Ok(resource);
}
```

### 4. JWT / Auth Hardening

```csharp
// ✅ Validate audience, issuer, and expiry
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };
    });
```

### 5. CORS Policy

```csharp
// ❌ Too permissive
app.UseCors(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// ✅ Restrict to known origins in production
builder.Services.AddCors(o => o.AddPolicy("prod", p =>
    p.WithOrigins("https://myapp.com")
     .WithMethods("GET", "POST")
     .AllowCredentials()));
```

### 6. Security Headers

```csharp
// Add via middleware or NWebSec
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["Permissions-Policy"] = "geolocation=()";
    await next();
});
```

## Security Checklist

- [ ] Enable HTTPS (`UseHttpsRedirection`)
- [ ] Validate all inputs (FluentValidation)
- [ ] Use parameterized queries / EF Core
- [ ] Store secrets in Key Vault, not appsettings.json
- [ ] Enforce JWT validation with signing key
- [ ] Rate limit sensitive endpoints (`AddRateLimiter`)
- [ ] Log security events (failed logins, IDOR attempts)
- [ ] Enable HSTS in production

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Microsoft: ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [AntiXSS, NWebSec NuGet packages](https://www.nuget.org/packages/NWebsec.AspNetCore.Middleware/)
