namespace TutoringBackend.Api.Models;

public enum LessonStatus
{
    Planned = 0,
    Completed = 1,
    CancelledValid = 2,      // odwołana z wyprzedzeniem - saldo nie przepada
    CancelledInvalid = 3,    // odwołana bez wyprzedzenia - saldo przepada
    Rescheduled = 4          // przeniesiona - "oryginał" zamykany tym statusem, powstaje nowy Lesson
}

public enum MaterialType
{
    Link = 0,
    File = 1
}

public enum BalanceTransactionType
{
    TopUp = 0,              // doładowanie / edycja salda przez admina
    LessonCharge = 1,       // odjęcie po zamknięciu lekcji
    CancellationPenalty = 2,// odjęcie za odwołanie bez wyprzedzenia
    Refund = 3               // zwrot (np. korekta)
}
