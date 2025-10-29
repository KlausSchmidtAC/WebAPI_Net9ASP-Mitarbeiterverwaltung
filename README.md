# 🏢 WebAPI_Net9ASP - Mitarbeiterverwaltung

Eine moderne **Mitarbeiterverwaltungs-API** entwickelt mit **.NET 9** und **Clean Architecture** Prinzipien. Diese RESTful Web API ermöglicht das vollständige Management von Mitarbeiterdaten mit asynchroner Programmierung und robuster Fehlerbehandlung.

## 🚀 Features

- ✅ **CRUD-Operationen** für Mitarbeiter (Create, Read, Update, Delete)
- ✅ **Async/Await Pattern** für optimale Performance
- ✅ **Clean Architecture** mit Domain-Driven Design
- ✅ **OperationResult Pattern** für elegante Fehlerbehandlung
- ✅ **Thread-sichere Datenbankinitialisierung** mit Semaphore
- ✅ **MySQL Integration** mit Dapper ORM
- ✅ **Comprehensive Unit Tests** mit NUnit und NSubstitute
- ✅ **Swagger/OpenAPI** Dokumentation
- ✅ **Structured Logging** mit Serilog
- ✅ **Dependency Injection** Container

## 🏗️ Architektur

Das Projekt folgt dem **Clean Architecture** Pattern mit klarer Trennung der Verantwortlichkeiten:

```
📁 WebAPI_Net9ASP/
├── 🌐 WebAPI_NET9/          # Presentation Layer (Controllers, Program.cs)
├── 🔧 Application/          # Application Layer (Services, Business Logic)
├── 💾 Data/                 # Infrastructure Layer (Repositories, Database)
├── 📋 Domain/               # Domain Layer (Entities, Core Logic)
└── 🧪 Tests/                # Test Layer (Unit Tests)
```

### Projektstruktur

- **Domain**: Kerngeschäftslogik und Entitäten (`Mitarbeiter`, `OperationResult`)
- **Application**: Anwendungsservices und Geschäftslogik (`IMitarbeiterService`)
- **Data**: Datenzugriff und Repository Pattern (`IMitarbeiterRepository`)
- **WebAPI_NET9**: HTTP-Controller und API-Endpunkte
- **Tests**: Umfassende Unit Tests für alle Layer

## 🛠️ Technologie-Stack

| Technologie | Version | Zweck |
|-------------|---------|-------|
| **.NET** | 9.0 | Core Framework |
| **ASP.NET Core** | 9.0 | Web API Framework |
| **MySQL** | Latest | Datenbank |
| **Dapper** | 2.1.66 | Micro-ORM |
| **Serilog** | 9.0.0 | Structured Logging |
| **Swagger** | 9.0.4 | API Dokumentation |
| **NUnit** | Latest | Unit Testing |
| **NSubstitute** | Latest | Mocking Framework |

## 📊 API-Endpunkte

### Mitarbeiter Management

| HTTP Verb | Endpunkt | Beschreibung |
|-----------|----------|--------------|
| `GET` | `/api/Mitarbeiter` | Alle Mitarbeiter abrufen |
| `GET` | `/api/Mitarbeiter/{id}` | Mitarbeiter nach ID abrufen |
| `GET` | `/api/Mitarbeiter/search?search=LastName` | Mitarbeiter nach Nachnamen aufsteigend sortiert |
| `GET` | `/api/Mitarbeiter/search?search=isActive` | Alle aktiven Mitarbeiter |
| `GET` | `/api/Mitarbeiter/search?search={yyyy-MM-dd}` | Mitarbeiter mit Geburtsdatum vor angegebenem Datum |
| `GET` | `/api/Mitarbeiter/birthDate?birthDate={yyyy-MM-dd}`| Mitarbeiter mit Geburtsdatum vor angegebenem Datum |
| `POST` | `/api/Mitarbeiter` | Neuen Mitarbeiter erstellen |
| `PUT` | `/api/Mitarbeiter/{id}` | Mitarbeiter aktualisieren |
| `DELETE` | `/api/Mitarbeiter/{id}` | Mitarbeiter deaktivieren |

### Beispiel-Request

```json
POST /api/Mitarbeiter
{
  "id": 0,
  "firstName": "Fritz",
  "lastName": "Mustermann", 
  "birthDate": "1990-05-15",
  "isActive": true
}
```

### Beispiel-Response

```json
{
  "id": 1,
  "firstName": "Max",
  "lastName": "Mustermann",
  "birthDate": "1985-05-15", 
  "isActive": true
}
```

## 🚀 Schnellstart

### Voraussetzungen

- **.NET 9 SDK** installiert
- **MySQL Server** verfügbar
- **Visual Studio 2022** oder **VS Code** (empfohlen)

### Installation

1. **Repository klonen**
   ```bash
   git clone https://github.com/[IhrUsername]/WebAPI_Net9ASP-Mitarbeiterverwaltung.git
   cd WebAPI_Net9ASP-Mitarbeiterverwaltung
   ```

2. **Abhängigkeiten wiederherstellen**
   ```bash
   dotnet restore
   ```

3. **Datenbankverbindung konfigurieren**
   
   Passen Sie in `Data/SQL_DB/SqlServerDatabaseInitializer.cs` die Verbindungsparameter an:
   ```csharp
   public SqlServerDatabaseInitializer(
       string serverIP = "localhost", 
       string databaseName = "Mitarbeiter",
       string port = "3306", 
       string username = "root", 
       string password = "IhrPasswort"
   )
   ```

4. **Projekt starten**
   ```bash
   dotnet run --project WebAPI_NET9
   ```

5. **API testen**
   
   Öffnen Sie `https://localhost:7071/swagger` für die Swagger-Dokumentation

## 🧪 Tests ausführen

```bash
# Alle Tests ausführen
dotnet test

# Mit detaillierten Ausgaben
dotnet test --verbosity normal

# Nur bestimmtes Testprojekt
dotnet test Tests/WebAPI_NET9Tests/WebAPI_NET9Tests.csproj
```

## 🎯 Besondere Implementierungsdetails

### Async/Await Pattern
Das gesamte Projekt verwendet konsequent async/await für optimale Performance:

```csharp
public async Task<OperationResult> CreateMitarbeiter(Mitarbeiter mitarbeiter)
{
    var result = await _mitarbeiterRepository.Add(mitarbeiter);
    return result;
}
```

### OperationResult Pattern
Elegante Fehlerbehandlung ohne Exceptions für Geschäftslogik:

```csharp
public class OperationResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    public static OperationResult SuccessResult() => new(true);
    public static OperationResult FailureResult(string error) => new(false, error);
}
```

### Thread-sichere Initialisierung
Database-Initialisierung mit Semaphore für Thread-Sicherheit:

```csharp
private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

public async Task<MySqlConnection> CreateConnection()
{
    await _semaphore.WaitAsync();
    try
    {
        // Thread-sichere Initialisierung
    }
    finally
    {
        _semaphore.Release();
    }
}
```

## 📈 Datenbankschema

```sql
CREATE TABLE Mitarbeiter (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Birthdate DATE NOT NULL,
    IsActive BOOLEAN NOT NULL
);
```

## 🛡️ Validierung & Fehlerbehandlung

- **Input-Validierung** auf Repository-Ebene
- **Datums-Validierung** im Format `yyyy-MM-dd`
- **Duplikat-Prüfung** für Name/Geburtsdatum-Kombinationen
- **Async Exception Handling** mit OperationResult Pattern
- **Structured Error Messages** für Client-Feedback

## 🚧 Geplante Erweiterungen

- [ ] **Authentication & Authorization** (JWT)
- [ ] **Paginierung** für große Datensätze
- [ ] **Caching** mit Redis
- [ ] **Docker** Containerisierung
- [ ] **CI/CD Pipeline** mit GitHub Actions
- [ ] **Health Checks** für Monitoring
- [ ] **Rate Limiting** für API-Schutz

## 🤝 Mitwirken

1. Fork das Repository
2. Erstelle einen Feature-Branch (`git checkout -b feature/NeuesFunktion`)
3. Committe deine Änderungen (`git commit -am 'Füge neue Funktion hinzu'`)
4. Push zum Branch (`git push origin feature/NeuesFunktion`)
5. Erstelle einen Pull Request

## 📝 Lizenz

Dieses Projekt steht unter der [MIT License](LICENSE).

## 👨‍💻 Autor

**Klaus Schmidt**
- GitHub: [@KlausSchmidtAC](https://github.com/KlausSchmidtAC)

---

⭐ **Star dieses Repository, wenn es dir geholfen hat!**

🔧 **Entwickelt mit .NET 9 und ❤️**