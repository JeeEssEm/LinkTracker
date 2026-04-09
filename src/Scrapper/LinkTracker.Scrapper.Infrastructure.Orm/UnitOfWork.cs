using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;

namespace LinkTracker.Scrapper.Infrastructure.Orm;

public class UnitOfWork(LinkTrackerContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.RollbackTransactionAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (dbContext.Database.CurrentTransaction != null)
        {
            await dbContext.Database.RollbackTransactionAsync();
        }
    }
}