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

### 6. Security Headers & CSP
Modern browsers respect headers that mitigate XSS and clickjacking.

```csharp
// Standard hardening
app.UseHsts();
app.UseHttpsRedirection();

// Advanced CSP (Content Security Policy)
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' https://trusted.cdn.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:;");
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    await next();
});
```

### 7. Data Protection API (Encryption at Rest)
Use `IDataProtectionProvider` for sensitive data that doesn't belong in a DB (e.g., temporary tokens, cookies).

```csharp
public class SecretService(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("SecretService.v1");

    public string Encrypt(string input) => _protector.Protect(input);
    public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
}
```

### 8. Policy-Based Authorization
Move logic out of controllers into reusable requirements.

```csharp
// Definition
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AtLeast21", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
});

// Usage
[Authorize(Policy = "AtLeast21")]
public IActionResult DrinkBeer() => Ok();
```

## Security Checklist

- [ ] Enable HTTPS (`UseHttpsRedirection`)
- [ ] Enable HSTS in production (`UseHsts`)
- [ ] Set `HttpOnly` and `Secure` flags on all cookies
- [ ] Use `SameSite=Strict` or `Lax` to mitigate CSRF
- [ ] Validate all inputs (FluentValidation)
- [ ] Use parameterized queries / EF Core
- [ ] Store secrets in Key Vault / User Secrets
- [ ] Implement Rate Limiting (`AddRateLimiter`)
- [ ] Audit logs for sensitive operations
- [ ] Scrutinize `unsafe-inline` in CSP
- [ ] Use Data Protection API for local encryption

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Microsoft: ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [AntiXSS, NWebSec NuGet packages](https://www.nuget.org/packages/NWebsec.AspNetCore.Middleware/)
