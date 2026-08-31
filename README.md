# Tutoring Backend

Backend do zarządzania lekcjami korepetycji (uczeń / nauczyciel / admin).
.NET 8 Web API + EF Core + PostgreSQL.

## Czego potrzebujesz

- .NET 8 SDK - https://dotnet.microsoft.com/download/dotnet/8.0
- Docker Desktop - https://www.docker.com/products/docker-desktop/

Sprawdź czy masz .NET:
```
dotnet --version
```
Powinno pokazać 8.x.x.

## Uruchomienie bazy danych

1. Odpal Docker Desktop i poczekaj aż się uruchomi.
2. W terminalu wpisz (tylko za pierwszym razem):
```
docker run --name tutoring-postgres -e POSTGRES_PASSWORD=postgres_dev_only -e POSTGRES_DB=tutoring_db -p 5432:5432 -d postgres:16
```
3. Sprawdź czy działa:
```
docker ps
```
Powinieneś zobaczyć tutoring-postgres ze statusem Up.

Przy kolejnych uruchomieniach (baza już istnieje, nie trzeba jej tworzyć od nowa):
```
docker start tutoring-postgres
```
Zatrzymanie:
```
docker stop tutoring-postgres
```
Jeśli chcesz wyczyścić bazę i zacząć od zera:
```
docker rm -f tutoring-postgres
```
i uruchom ponownie komendę z punktu 2.

## Konfiguracja

Plik `src/TutoringBackend.Api/appsettings.Development.json` jest już ustawiony pod
powyższy kontener. Jeśli nic nie zmieniałeś w komendzie docker run, nie musisz go ruszać.

Domyślnie zakłada się konto admina: `admin@example.com` / `ZmienMnie!2024#`.

## Instalacja i pierwsze uruchomienie

W katalogu `src/TutoringBackend.Api`:
```
dotnet restore
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet run
```

Jeśli wszystko działa, w konsoli zobaczysz "Now listening on: https://localhost:7xxx".

Swagger (do testowania API w przeglądarce):
```
https://localhost:7xxx/swagger
```

## Najczęstszy błąd

"Failed to connect to 127.0.0.1:5432" - baza nie działa. Sprawdź `docker ps`,
jeśli tutoring-postgres nie jest na liście, odpal `docker start tutoring-postgres`.

## Jak tego użyć - pierwsze kroki

1. Zaloguj się jako admin: POST /api/auth/login (mail/hasło z SeedAdmin).
   Skopiuj accessToken i wklej w Swaggerze w Authorize jako "Bearer <token>".
2. Utwórz nauczyciela: POST /api/admin/users/teachers
3. Utwórz ucznia: POST /api/admin/users/students
4. Przypisz ucznia do nauczyciela i ustaw stawkę: POST /api/admin/assignments
5. Zaloguj się jako nauczyciel, dodaj harmonogram: POST /api/teacher/schedule
6. System sam co godzinę generuje konkretne lekcje na kolejne ~3 tygodnie.
7. Nauczyciel widzi je w GET /api/teacher/lessons i może je zamykać, odwoływać
   albo przenosić.

## Struktura danych - w skrócie

- ApplicationUser - każdy użytkownik (uczeń, nauczyciel, admin), rozróżniony rolą.
  Konta zakłada tylko admin.
- TeacherStudentAssignment - łączy ucznia z nauczycielem, trzyma stawkę za lekcję.
- ScheduleSlot - cykliczny wzorzec w kalendarzu, np. "co wtorek 16:00".
- Lesson - konkretna, jednorazowa lekcja z datą i godziną, generowana automatycznie
  ze ScheduleSlot albo ręcznie przy przeniesieniu. Ma status: zaplanowana / odbyta /
  odwołana prawidłowo / odwołana nieprawidłowo / przeniesiona.
- LessonMaterial - link lub plik dopięty do lekcji.
- BalanceTransaction - każda zmiana salda ucznia (doładowanie, odjęcie za lekcję,
  kara za odwołanie bez wyprzedzenia) zapisana osobno, dla historii i audytu.

Zasada odwołań: nauczyciel ustawia ile godzin wyprzedzenia wymaga (domyślnie 24h).
Odwołanie z wyprzedzeniem - saldo nie przepada. Bez wyprzedzenia - przepada.

## Zmienne środowiskowe na produkcji

Nie wpisuj sekretów do appsettings.json. (panowie współtwórcy tego nie uzupełniamy my)
```
ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...
Jwt__Key=<min. 32 losowe znaki>
Jwt__Issuer=TutoringBackend
Jwt__Audience=TutoringBackendClient
Cors__FrontendOrigin=https://twoja-domena.pl
SeedAdmin__Email=admin@twoja-domena.pl
SeedAdmin__Password=<silne hasło>
```

## Zabezpieczenia - w skrócie

- SQL Injection: całość przez EF Core / LINQ, zapytania parametryzowane automatycznie.
- XSS: pola tekstowe (temat, opis, nazwy materiałów) kodowane przed zapisem.
- Hasła: hashowane (Identity, PBKDF2), silna polityka haseł, blokada konta po 5
  nieudanych próbach na 15 minut.
- JWT: krótki access token (15 min) + refresh token, przechowywany w bazie tylko
  jako hash.
- Rate limiting na logowaniu i globalnie na API.
- Autoryzacja przez role, serwer zawsze filtruje dane po ID zalogowanego
  użytkownika, nigdy po parametrze od klienta.
- Upload plików: dozwolone tylko wybrane rozszerzenia, limit 25 MB, losowa nazwa pliku.
- HTTPS wymuszony, CORS ograniczony do jednej domeny frontendu.
- Błędy zwracane do klienta zawsze ogólnym komunikatem, szczegóły tylko w logach.

## Struktura projektu

```
src/TutoringBackend.Api/
  Controllers/   - AuthController, AdminController, TeacherController, StudentController, ReportsController
  Models/        - encje EF Core
  Data/          - ApplicationDbContext, DbInitializer
  DTOs/          - obiekty wejścia/wyjścia z walidacją
  Services/      - TokenService, BalanceService, LessonService, ScheduleGenerationService
  Middleware/    - obsługa błędów, nagłówki bezpieczeństwa
  Extensions/    - konfiguracja Identity/JWT/rate limiting/CORS
```

