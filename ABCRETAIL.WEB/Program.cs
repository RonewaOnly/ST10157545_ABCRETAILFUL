using ABCRETAIL.WEB.Data;
using ABCRETAIL.WEB.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// MVC services
builder.Services.AddControllersWithViews();




// Register our Azure Storage services for dependency injection.
// Both services read the connection string from configuration (appsettings.json
// or, better, from environment variables / user-secrets so the real key never
// gets committed to source control).
builder.Services.AddSingleton<ITableStorageService, TableStorageService>();
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<IQueueStorageService, QueueStorageService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();

//

var app = builder.Build();

// Make sure the Azure Table Storage tables and Blob Storage container exist
// before the app starts serving requests.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var tableService = scope.ServiceProvider
            .GetRequiredService<ITableStorageService>();

        await tableService.InitializeAsync();

        Console.WriteLine("Azure Table Storage initialized successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Azure Table Storage initialization failed.");
        Console.WriteLine(ex.Message);
    }

    try
    {
        var blobService = scope.ServiceProvider
            .GetRequiredService<IBlobStorageService>();

        await blobService.InitializeAsync();

        Console.WriteLine("Azure Blob Storage initialized successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Azure Blob Storage initialization failed.");
        Console.WriteLine(ex.Message);
    }

    try
    {
        var queueService = scope.ServiceProvider
            .GetRequiredService<IQueueStorageService>();

        await queueService.InitializeAsync();

        Console.WriteLine("Azure Queue Storage initialized successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Azure Queue Storage initialization failed.");
        Console.WriteLine(ex.Message);
    }

    try
    {
        var fileService = scope.ServiceProvider
            .GetRequiredService<IFileStorageService>();

        await fileService.InitializeAsync();

        Console.WriteLine("Azure File Storage initialized successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Azure File Storage initialization failed.");
        Console.WriteLine(ex.Message);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
