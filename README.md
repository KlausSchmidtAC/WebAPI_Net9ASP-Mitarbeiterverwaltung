# 🏢 WebAPI_Net9ASP - Mitarbeiterverwaltung

Eine moderne **Mitarbeiterverwaltungs-API** entwickelt mit **.NET 9** und **Clean Architecture** Prinzipien. Diese RESTful Web API ermöglicht das vollständige Management von Mitarbeiterdaten mit asynchroner Programmierung und robuster Fehlerbehandlung.

## 🚀 Features

- ✅ **CRUD-Operationen** für Mitarbeiter (Create, Read, Update, Delete)
- ✅ **JWT Authentication & Authorization** mit Claims-basierter Zugriffskontrolle
- ✅ **OpenTelemetry OTLP Logging** für moderne Observability
- ✅ **REST-konforme JSON Responses** mit strukturierten Message/Data Objekten
- ✅ **Async/Await Pattern** für optimale Performance
- ✅ **Clean Architecture** mit Domain-Driven Design
- ✅ **OperationResult Pattern** für elegante Fehlerbehandlung
- ✅ **Thread-sichere Datenbankinitialisierung** mit Semaphore
- ✅ **MySQL Integration** mit Dapper ORM
- ✅ **Comprehensive Unit Tests** (37 Tests) mit NUnit und NSubstitute
- ✅ **Swagger/OpenAPI** Dokumentation mit JWT-Support
- ✅ **Structured Logging** mit ILogger und OpenTelemetry
- ✅ **Dependency Injection** Container
- ✅ **Advanced Search & Filtering** (Name, Status, Geburtsdatum)
- ✅ **JSON Source Generation** für optimierte Serialization

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
| **JWT Bearer** | Latest | Authentication & Authorization |
| **OpenTelemetry** | Latest | Observability & Logging |
| **OTLP Exporter** | Latest | Log Export (Seq, Jaeger, etc.) |
| **Swagger** | 9.0.4 | API Dokumentation |
| **System.Text.Json** | 9.0 | JSON Serialization |
| **NUnit** | Latest | Unit Testing Framework |
| **NSubstitute** | Latest | Mocking Framework |

## 📊 API-Endpunkte

### 🔐 Authentication

| HTTP Verb | Endpunkt | Beschreibung |
|-----------|----------|--------------|
| `POST` | `/api/Auth/login` | JWT Token generieren |
| `POST` | `/api/Auth/logout` | Token invalidieren |

### 👥 Mitarbeiter Management

| HTTP Verb | Endpunkt | Beschreibung | Authorization |
|-----------|----------|--------------|---------------|
| `GET` | `/api/Mitarbeiter` | Alle Mitarbeiter abrufen | JWT Required |
| `GET` | `/api/Mitarbeiter/{id}` | Mitarbeiter nach ID abrufen | JWT Required |
| `GET` | `/api/Mitarbeiter/sorted?filter=LastName` | Mitarbeiter nach Nachnamen sortiert | JWT Required |
| `GET` | `/api/Mitarbeiter/sorted?filter=isActive` | Alle aktiven Mitarbeiter | JWT Required |
| `GET` | `/api/Mitarbeiter/birthDate?birthDate={yyyy-MM-dd}` | Mitarbeiter mit Geburtsdatum vor Datum | JWT Required |
| `POST` | `/api/Mitarbeiter` | Neuen Mitarbeiter erstellen | Admin Role |
| `PUT` | `/api/Mitarbeiter/{id}` | Mitarbeiter aktualisieren | Admin Role |
| `DELETE` | `/api/Mitarbeiter/{id}` | Mitarbeiter löschen | Admin Role |

### JWT Authentication Beispiel

```json
POST /api/Auth/login
{
  "username": "admin",
  "password": "password123"
}
```

**Response:**
```json
{
  "message": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "expiry": "2025-12-05T15:30:00Z"
  }
}
```

### Mitarbeiter Request Beispiel

```json
POST /api/Mitarbeiter
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

{
  "id": 0,
  "firstName": "Fritz",
  "lastName": "Mustermann", 
  "birthDate": "1990-05-15",
  "isActive": true
}
```

### REST-konforme Response Struktur

```json
{
  "message": "Neuer Mitarbeiter erstellt",
  "data": {
    "id": 1,
    "firstName": "Max",
    "lastName": "Mustermann",
    "birthDate": "1985-05-15", 
    "isActive": true
  }
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

3. **Konfiguration anpassen**
   
   **Datenbankverbindung** in `appsettings.json`:
   ```json
   {
     "Database": {
       "ServerIP": "localhost",
       "DatabaseName": "Mitarbeiter",
       "Port": "3306",
       "Username": "root",
       "Password": "IhrPasswort"
     }
   }
   ```

   **JWT-Konfiguration** in `appsettings.json`:
   ```json
   {
     "JWTSettings": {
       "Issuer": "WebAPI_NET9_MitarbeiterService",
       "Audience": "WebAPI_NET9_Client",
       "SecretKey": "your-super-secret-jwt-signing-key-here"
     }
   }
   ```

4. **OpenTelemetry/OTLP Setup (Optional)**
   
   Für erweiterte Observability können Sie einen OTLP-kompatiblen Collector verwenden:
   
   **Seq (empfohlen für Development):**
   ```bash
   docker run -d --name seq -e ACCEPT_EULA=Y -p 5099:5099 -p 80:80 datalust/seq:latest
   ```
   
   **Jaeger (für Distributed Tracing):**
   ```bash
   docker run -d --name jaeger -p 14268:14268 -p 16686:16686 jaegertracing/all-in-one:latest
   ```

5. **Projekt starten**
   ```bash
   dotnet run --project WebAPI_NET9
   ```

6. **API testen**
   
   - **Swagger UI**: `https://localhost:5101/swagger`
   - **HTTP**: `http://localhost:5100`  
   - **HTTPS**: `https://localhost:5101`
   - **Logs**: `http://localhost:5099` (falls Seq läuft)

## 🧪 Tests ausführen

```bash
# Alle Tests ausführen (37 Tests)
dotnet test

# Mit detaillierten Ausgaben
dotnet test --verbosity normal

# Nur Controller Tests
dotnet test Tests/WebAPI_NET9Tests/MitarbeiterControllerTests.cs

# Nur Repository Tests  
dotnet test Tests/WebAPI_NET9Tests/SqlConnectionFactoryTests.cs

# Test Coverage (falls installiert)
dotnet test --collect:"XPlat Code Coverage"
```

**Aktuelle Test-Statistiken:**
- ✅ **37 Unit Tests** - Alle erfolgreich
- 🧪 **Controller Tests**: REST-Response-Validierung mit JsonDocument
- 🗄️ **Repository Tests**: Datenbankverbindungen und -operationen
- 🔒 **Service Tests**: Geschäftslogik und OperationResult Pattern

## 🎯 Besondere Implementierungsdetails

### OpenTelemetry OTLP Logging
Moderne Observability mit strukturierten Logs:

```csharp
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateEmpty()
        .AddService("WebAPI_NET9_MitarbeiterService")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "development",
            ["service.version"] = "1.0.0"
        }));

    options.AddOtlpExporter(exporter =>
    {
        exporter.Endpoint = new Uri("http://localhost:5099/ingest/otlp/v1/logs");
        exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
    });
});
```

### JWT Authentication & Authorization
Claims-basierte Sicherheit mit Role-Based Access Control:

```csharp
[HttpPost]
[RequiresClaim(IdentityData.Claims.AdminRole, "true")]
public async Task<IActionResult> CreateMitarbeiter([FromBody] Mitarbeiter mitarbeiter)
{
    // Nur Admins können Mitarbeiter erstellen
}
```

### REST-konforme JSON Responses
Strukturierte Antworten für konsistente API-Nutzung:

```csharp
// Erfolgreiche Antwort
return Ok(new { 
    Message = "Alle Mitarbeiter erfolgreich abgerufen.", 
    Data = mitarbeiterList,
    Count = mitarbeiterList.Count() 
});

// Fehler-Antwort
return NotFound(new { 
    Message = "Mitarbeiter mit ID 999 wurde nicht gefunden.", 
    Data = (object?)null 
});
```

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
public class OperationResult<T>
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public T? Data { get; private set; }
    
    public static OperationResult<T> SuccessResult(T data) => new(true, data);
    public static OperationResult<T> FailureResult(string error) => new(false, error);
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
        // Thread-sichere Initialisierung mit strukturiertem Logging
        _logger.LogDebug("Creating new MySQL connection");
    }
    finally
    {
        _semaphore.Release();
    }
}
```

### JSON Source Generation
Optimierte Serialisierung mit .NET 9 Native AOT Unterstützung:

```csharp
[JsonSerializable(typeof(Mitarbeiter))]
[JsonSerializable(typeof(List<Mitarbeiter>))]
[JsonSerializable(typeof(TokenGenerationRequest))]
public partial class AppJsonSerializerContext : JsonSerializerContext { }
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

- [x] **Authentication & Authorization** (JWT) ✅ **Implementiert**
- [x] **OpenTelemetry Logging** ✅ **Implementiert**
- [x] **Advanced Filtering & Search** ✅ **Implementiert**
- [ ] **Paginierung** für große Datensätze
- [ ] **Caching** mit Redis/Memory Cache
- [ ] **Docker** Containerisierung mit Multi-Stage Build
- [ ] **CI/CD Pipeline** mit GitHub Actions
- [ ] **Health Checks** für Monitoring
- [ ] **Rate Limiting** für API-Schutz
- [ ] **API Versioning** (v1, v2)
- [ ] **Integration Tests** mit TestContainers
- [ ] **Metrics & Tracing** mit OpenTelemetry
- [ ] **Database Migrations** mit Entity Framework
- [ ] **Swagger Code Generation** für Client SDKs

## 🌐 Frontend-Integration & Framework-Support

Diese API wurde mit **Frontend-First** Design entwickelt und bietet vollständige Kompatibilität mit modernen Web- und Mobile-Frameworks:

### ⚛️ **React.js Integration**
```typescript
// JWT Authentication Hook
const useAuth = () => {
  const [token, setToken] = useState(localStorage.getItem('jwt_token'));
  
  const login = async (credentials) => {
    const response = await fetch('/api/Auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials)
    });
    const { data } = await response.json();
    setToken(data.token);
    localStorage.setItem('jwt_token', data.token);
  };
};

// Mitarbeiter Service
const mitarbeiterService = {
  getAll: () => fetch('/api/Mitarbeiter', {
    headers: { 'Authorization': `Bearer ${token}` }
  }).then(res => res.json())
};
```

### 🟢 **Vue.js Integration**
```typescript
// Composable für Mitarbeiterverwaltung
export const useMitarbeiter = () => {
  const mitarbeiterList = ref([]);
  const isLoading = ref(false);
  
  const fetchMitarbeiter = async () => {
    isLoading.value = true;
    try {
      const response = await $fetch('/api/Mitarbeiter', {
        headers: { Authorization: `Bearer ${authToken.value}` }
      });
      mitarbeiterList.value = response.data;
    } finally {
      isLoading.value = false;
    }
  };
  
  return { mitarbeiterList, fetchMitarbeiter, isLoading };
};
```

### 🅰️ **Angular Integration**
```typescript
// Angular Service
@Injectable({ providedIn: 'root' })
export class MitarbeiterService {
  private apiUrl = '/api/Mitarbeiter';
  
  constructor(private http: HttpClient) {}
  
  getMitarbeiter(): Observable<ApiResponse<Mitarbeiter[]>> {
    return this.http.get<ApiResponse<Mitarbeiter[]>>(this.apiUrl);
  }
  
  createMitarbeiter(mitarbeiter: Mitarbeiter): Observable<ApiResponse<Mitarbeiter>> {
    return this.http.post<ApiResponse<Mitarbeiter>>(this.apiUrl, mitarbeiter);
  }
}

// JWT Interceptor
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = localStorage.getItem('jwt_token');
    if (token) {
      req = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` }
      });
    }
    return next.handle(req);
  }
}
```

### 📱 **React Native / Mobile Apps**
```typescript
// React Native Integration
import AsyncStorage from '@react-native-async-storage/async-storage';

class ApiService {
  private baseUrl = 'https://your-api.com/api';
  
  async authenticatedFetch(endpoint: string, options: RequestInit = {}) {
    const token = await AsyncStorage.getItem('jwt_token');
    return fetch(`${this.baseUrl}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
        ...options.headers,
      },
    });
  }
}
```

### 🔥 **Svelte/SvelteKit Integration**
```typescript
// Svelte Store
import { writable } from 'svelte/store';

export const mitarbeiterStore = writable([]);
export const authStore = writable({ token: null, isAuthenticated: false });

// API Client
export const apiClient = {
  async getMitarbeiter() {
    const { token } = get(authStore);
    const response = await fetch('/api/Mitarbeiter', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const result = await response.json();
    mitarbeiterStore.set(result.data);
    return result;
  }
};
```

### 💡 **Framework-agnostische Features**

| Feature | Frontend-Vorteil |
|---------|------------------|
| **REST-konforme JSON** | Standardisierte Response-Struktur (`message`/`data`) |
| **JWT Authentication** | Stateless, client-side Token-Management |
| **CORS-Support** | Cross-Origin Requests für alle Domains |
| **OpenAPI/Swagger** | Automatische TypeScript Client-Generierung |
| **Structured Error Responses** | Konsistente Fehlerbehandlung |
| **HTTP Status Codes** | Standard-konforme Antworten (200, 401, 404, etc.) |

### 🛠️ **Code Generation & Tools**

```bash
# TypeScript Client aus OpenAPI generieren
npx @openapitools/openapi-generator-cli generate \
  -i https://localhost:5101/swagger/v1/swagger.json \
  -g typescript-axios \
  -o ./src/api

# RTK Query für React (Redux Toolkit)
npx @rtk-query/codegen-openapi openapi-config.ts
```

### 🚀 **Deployment-Optionen für Fullstack**

- **Vercel/Netlify**: Frontend + API als Serverless Functions
- **Docker Compose**: API + Frontend Container zusammen
- **Kubernetes**: Microservices mit Ingress-Controller
- **Azure App Service**: .NET Backend + Static Web Apps Frontend
- **AWS**: ECS/Lambda + CloudFront Distribution

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

🔧 **Entwickelt mit .NET 9, OpenTelemetry & JWT Authentication ❤️**

📊 **Modern API Design | 🔒 Secure Authentication | 📈 Observable Logging**