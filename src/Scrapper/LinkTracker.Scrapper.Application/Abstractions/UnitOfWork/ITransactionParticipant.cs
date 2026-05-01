using System.Data.Common;

namespace LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;

public interface ITransactionParticipant
{
    Task OnBeginAsync(DbTransaction transaction, CancellationToken ct = default);
    Task OnCommitAsync(CancellationToken ct = default);
    Task OnRollbackAsync(CancellationToken ct = default);
}
