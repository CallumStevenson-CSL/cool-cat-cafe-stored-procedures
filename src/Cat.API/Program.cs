using Cat.Api.Constants;
using Cat.Api.Entities;
using Cat.Api.Interfaces;
using Cat.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseCosmos(
            "https://localhost:8081/",
            "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            "CoolCatCafe",
            cosmosOptions =>
            {
                cosmosOptions.HttpClientFactory(() => new(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }));

                cosmosOptions.ConnectionMode(ConnectionMode.Gateway);
            })
    );

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<IApplicationDbContext>();
    await dbContext.EnsureCreatedAsync().ConfigureAwait(false);
}

app.MapGet("/kittens", async Task<Results<Ok<Kitten>, NotFound>> (IApplicationDbContext dbContext) =>
    {
        await dbContext.Kittens.AddAsync(new()
            {
                Id = Guid.NewGuid(),
                Name = "RegularSizedMichael",
                CreatedAt = DateTime.UtcNow
            })
            .ConfigureAwait(false);

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        var kitten = await dbContext.Kittens
            .Where(x => x.Name == "RegularSizedMichael")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (kitten is null)
        {
            return TypedResults.NotFound();
        }

        kitten.Name = "BigMike";

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        return TypedResults.Ok(kitten);
    })
    .WithName("Kittens")
    .WithOpenApi();

app.MapGet("/stored-procedure", async Task<Results<Ok<string>, NotFound>> (IApplicationDbContext dbContext) =>
    {
        // Delete stored procedure
        await dbContext.Client.GetContainer("CoolCatCafe", CosmosDbConstants.AnimalsContainerName)
            .Scripts.DeleteStoredProcedureAsync("testSP").ConfigureAwait(false);

        // Create stored procedure
        await dbContext.Client.GetContainer("CoolCatCafe", CosmosDbConstants.AnimalsContainerName)
            .Scripts.CreateStoredProcedureAsync(new StoredProcedureProperties()
            {
                Id = "testSP",
                Body = "function helloWorld() {" +
                       "var context = getContext();" +
                       "var response = context.getResponse();" +
                       "response.setBody(\"Hello, World\");" +
                       "}"
            }).ConfigureAwait(false);


        // Execute stored procedure
        var response = await dbContext.Client.GetContainer("CoolCatCafe", CosmosDbConstants.AnimalsContainerName)
            .Scripts.ExecuteStoredProcedureAsync<string>("testSP", new PartitionKey("Kitten"), null,
                new StoredProcedureRequestOptions() { EnableScriptLogging = true }).ConfigureAwait(false);

        return TypedResults.Ok(response.Resource);
    })
.WithName("SP")
.WithOpenApi();

app.Run();
