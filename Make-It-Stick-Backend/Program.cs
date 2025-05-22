using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.ML.OnnxRuntime;

var builder = WebApplication.CreateBuilder(args);



var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlServer(connectionString));


builder.Services.AddMemoryCache();

// Setează URL-ul înainte de Build, pentru ca serverul să asculte pe toate interfețele
builder.WebHost.UseUrls("http://0.0.0.0:7057");

// Adaugă fișierul secrets.json la configurație
builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Înlocuiește YOUR_FIREBASE_PROJECT_ID cu ID-ul proiectului tău Firebase
        options.Authority = "https://securetoken.google.com/" + builder.Configuration["Firebase:ProjectId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://securetoken.google.com/" + builder.Configuration["Firebase:ProjectId"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Firebase:ProjectId"],
            ValidateLifetime = true
        };

    });

// Definește politica CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var firebaseApiKey = builder.Configuration["Firebase:ApiKey"];
var googleBooksApiKey = builder.Configuration["GoogleBooks:ApiKey"];
var nytApiKey = builder.Configuration["NYT:ApiKey"];
var serviceAccountPath = builder.Configuration["ServiceAccountPath"];

// Debug: Afișează valoarea cheii pentru a verifica dacă a fost încărcată corect
Console.WriteLine($"ServiceAccountPath: {serviceAccountPath}");

// Inițializează Firebase Admin SDK
try
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(serviceAccountPath),
    });
    Console.WriteLine("Firebase Admin SDK initialized successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing Firebase Admin SDK: {ex.Message}");
}

// Înregistrează serviciul GoogleBooksService cu HttpClient
builder.Services.AddHttpClient<GoogleBooksService>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/books/v1/");
});

// Înregistrează serviciul NYTBooksService cu HttpClient
builder.Services.AddHttpClient<NYTBooksService>(client =>
{
    client.BaseAddress = new Uri("https://api.nytimes.com/svc/books/v3/");
});

// serviciile pentru Serverul pe care ruleaza modelul de generare de tag-uri
builder.Services.AddHttpClient("FastApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:8000/"); // URL-ul unde rulează FastAPI
    client.Timeout = TimeSpan.FromSeconds(5);
});



// Adaugă serviciile necesare
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Alte setări Swagger...
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer YOUR_TOKEN_HERE'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


var app = builder.Build();

// Folosește CORS în pipeline-ul HTTP
app.UseCors("AllowAll");

// Rulează seeder‑ul la startup, doar dacă baza de date nu e deja populată
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    await BooksDataSeederInitializer.InitializeAsync(serviceProvider);
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Pentru testare, dezactivează redirecționarea către HTTPS
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
