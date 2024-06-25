using Cat.Api.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Cat.Api.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Kitten> Kittens { get; }
    CosmosClient Client { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default);
}
