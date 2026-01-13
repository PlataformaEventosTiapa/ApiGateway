using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims; // Necesario para ClaimTypes

var builder = WebApplication.CreateBuilder(args);
var Configuration = builder.Configuration;

// --- 1. Configurar Autenticación (Módulo 1) ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Configuration["JwtBearer:Authority"];
        options.Audience = Configuration["JwtBearer:Audience"];
        options.RequireHttpsMetadata = Configuration.GetValue<bool>("JwtBearer:RequireHttpsMetadata");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["JwtBearer:Authority"],
            ValidAudience = Configuration["JwtBearer:Audience"],
            RoleClaimType = ClaimTypes.Role // Mapea 'realm_access.roles'
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context => {
                Console.WriteLine("OnAuthenticationFailed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context => {
                Console.WriteLine("OnTokenValidated: " + context.SecurityToken);
                return Task.CompletedTask;
            }
        };
    });

// --- 2. Configurar Autorización con Políticas (Módulo 1) ---
builder.Services.AddAuthorization(options =>
{
    // Política Básica: Autenticado
    options.AddPolicy("auth_policy", policy => policy.RequireAuthenticatedUser());

    // Política por Rol: Usuario
    options.AddPolicy("auth_policy_user", policy => policy.RequireRole("Usuario"));

    // Política por Rol: Organizador
    options.AddPolicy("auth_policy_organizer", policy => policy.RequireRole("Organizador"));

    // Política por Rol: Administrador
    options.AddPolicy("auth_policy_admin", policy => policy.RequireRole("Administrador"));

    // --- ¡NUEVA POLÍTICA AÑADIDA! ---
    // Política por Rol: Soporte
    options.AddPolicy("auth_policy_support", policy => policy.RequireRole("Soporte"));
    // --- FIN DE NUEVA POLÍTICA ---

    // Política Combinada (Ejemplo): Organizador O Administrador
    options.AddPolicy("auth_policy_organizer_or_admin", policy => policy.RequireRole("Organizador", "Administrador"));
});


// --- 3. Configurar YARP (Módulo 2) ---
builder.Services.AddReverseProxy()
    .LoadFromConfig(Configuration.GetSection("ReverseProxy"));

// --- 4. Configurar CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // URL del frontend React
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});


// --- Construcción de la Aplicación ---
var app = builder.Build();

// --- 5. Habilitar Middlewares (Orden CRÍTICO) ---
app.UseRouting();
app.UseCors("AllowFrontend"); // CORS ANTES de Auth
app.UseAuthentication();    // Quién eres?
app.UseAuthorization();     // Qué puedes hacer?
app.MapReverseProxy();      // Redirigir si todo OK

app.MapGet("/", () => "API Gateway is running!");

app.Run();

