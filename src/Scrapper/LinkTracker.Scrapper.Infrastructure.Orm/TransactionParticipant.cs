using System.Data.Common;
using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm;

public class TransactionParticipant(LinkTrackerContext dbContext) : ITransactionParticipant
{
    public Task OnBeginAsync(DbTransaction transaction, CancellationToken ct = default)
    {
        return dbContext.Database.UseTransactionAsync(transaction, ct);
    }

    public Task OnCommitAsync(CancellationToken ct = default)
    {
        return dbContext.Database.CommitTransactionAsync(ct);
    }

    public Task OnRollbackAsync(CancellationToken ct = default)
    {
        return dbContext.Database.RollbackTransactionAsync(ct);
    }
}