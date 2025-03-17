using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Make_It_Stick_Backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlServer(connectionString));

// Setează URL-ul înainte de Build, pentru ca serverul să asculte pe toate interfețele
builder.WebHost.UseUrls("http://0.0.0.0:7057");

// Adaugă fișierul secrets.json la configurație
builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

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

// Adaugă serviciile necesare
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Folosește CORS în pipeline-ul HTTP
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Pentru testare, dezactivează redirecționarea către HTTPS
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
