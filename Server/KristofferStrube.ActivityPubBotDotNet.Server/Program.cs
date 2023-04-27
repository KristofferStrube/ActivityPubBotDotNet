using KristofferStrube.ActivityPubBotDotNet.Server;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add ActivityPubService
builder.Services.AddHttpClient();
builder.Services.AddScoped<ActivityPubService>();
builder.Services.AddScoped<IOutboxService>(_ => new RSSFeedOutboxService("https://kristoffer-strube.dk/RSS.xml", "bot", builder.Configuration));

// Configure the database
string connectionString = builder.Configuration.GetConnectionString("Todos") ?? "Data Source=.db/Todos.db";
builder.Services.AddSqlite<ActivityPubDbContext>(connectionString);

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.Map("/", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();

app.MapUsers();
app.MapWebFingers();

ActivityPubDbContext dbContext = app.Services.CreateScope().ServiceProvider.GetRequiredService<ActivityPubDbContext>();
if (!dbContext.Users.Any(u => u.Name == "Bot"))
{
    dbContext.Add(new UserInfo("Bot", $"{app.Configuration["HostUrls:Server"]}/Users/bot"));
    dbContext.SaveChanges();
}

app.Run();