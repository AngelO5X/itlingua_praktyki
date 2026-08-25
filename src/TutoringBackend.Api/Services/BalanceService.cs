using Microsoft.EntityFrameworkCore;
using TutoringBackend.Api.Data;
using TutoringBackend.Api.Models;

namespace TutoringBackend.Api.Services;

public interface IBalanceService
{
    Task ApplyTransactionAsync(Guid studentId, decimal amount, BalanceTransactionType type,
        Guid createdByUserId, Guid? lessonId = null, string? note = null, CancellationToken ct = default);
}

/// Jedyne miejsce, które wolno modyfikować saldo ucznia. Operacja jest transakcyjna:
/// zapis wpisu w BalanceTransaction i aktualizacja ApplicationUser.Balance dzieją się
/// razem albo wcale (spójność audytu z rzeczywistym saldem).
public class BalanceService : IBalanceService
{
    private readonly ApplicationDbContext _db;

    public BalanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task ApplyTransactionAsync(Guid studentId, decimal amount, BalanceTransactionType type,
        Guid createdByUserId, Guid? lessonId = null, string? note = null, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var student = await _db.Users.FirstOrDefaultAsync(u => u.Id == studentId, ct)
            ?? throw new InvalidOperationException("Nie znaleziono ucznia.");

        student.Balance += amount;

        _db.BalanceTransactions.Add(new BalanceTransaction
        {
            StudentId = studentId,
            Amount = amount,
            Type = type,
            LessonId = lessonId,
            Note = note,
            CreatedByUserId = createdByUserId
        });

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
