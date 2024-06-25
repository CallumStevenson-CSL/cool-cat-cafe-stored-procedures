using System.Reflection;
using Cat.Api.Entities;
using Cat.Api.Interceptors;
using Cat.Api.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Cat.Api.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Kitten> Kittens { get; init; }

    public CosmosClient Client => Database.GetCosmosClient();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new GroupChangeInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        return this.Database.EnsureCreatedAsync(cancellationToken);
    }
}
