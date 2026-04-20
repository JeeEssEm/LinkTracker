using LinkTracker.Scrapper.Application.Abstractions;

namespace LinkTracker.Scrapper.Infrastructure.Sql;

public class UnitOfWork(DatabaseSession session) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        var conn = await session.GetConnectionAsync(cancellationToken);
        session.Transaction = await conn.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (session.Transaction == null)
        {
            throw new InvalidOperationException("Cannot commit transaction which has not started!");
        }

        await session.Transaction.CommitAsync(cancellationToken);
        await session.Transaction.DisposeAsync();
        session.Transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (session.Transaction == null)
        {
            throw new InvalidOperationException("Cannot rollback transaction which has not started!");
        }

        await session.Transaction.RollbackAsync(cancellationToken);
        await session.Transaction.DisposeAsync();
        session.Transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (session.Transaction != null)
        {
            await RollbackAsync();
        }
    }
}