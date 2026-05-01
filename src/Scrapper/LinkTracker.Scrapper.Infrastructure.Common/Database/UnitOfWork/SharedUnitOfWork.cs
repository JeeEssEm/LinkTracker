using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Database.UnitOfWork;

public class SharedUnitOfWork(DatabaseSession session, IEnumerable<ITransactionParticipant> participants) : IUnitOfWork
{
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        var conn = await session.OpenConnectionAsync(cancellationToken);
        session.Transaction = await conn.BeginTransactionAsync(cancellationToken);

        foreach (var p in participants)
        {
            await p.OnBeginAsync(session.Transaction, cancellationToken);
        }
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