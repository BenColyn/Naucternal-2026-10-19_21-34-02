[[Ordner Struktur]]
### Allgemeine Struktur & Benennung:

Dateien / Klassen haben **sprechende, beschreibende** Namen.

->Bsp. PlayerManager, PlayerMovement, EnemyMovement, ItemManager, etc.

Eine Klasse pro Datei.

->Der Dateiname entspricht exakt dem Klassennamen.

[Observer-design-pattern](https://learn.microsoft.com/en-us/dotnet/standard/events/observer-design-pattern "https://learn.microsoft.com/en-us/dotnet/standard/events/observer-design-pattern")

### Sichtbarkeiten & Serialisierung:

**Public** nur, wenn **nötig.**

->Interne Logik bleibt private oder protected.

Für Variablen die im Unity Inspector sichtbar sein sollen nutze `[SerializeField] private`

Wenn eine Variable für Designer **nicht** _**direkt**_ **verständlich** ist wird ein `[Tooltip("...")]` verwendet.

Beispiel Code:

`[Tooltip("Movementspeed of Player in seconds")] [SerializeField] private float moveSpeed;`

Variablen werden **klein** geschrieben.

Methoden werden **groß** geschrieben.

### Namenskonventionen:

|   |   |   |
|---|---|---|
|Element|Beispiel|Regel|
|Klassen|`PlayerController`|PascalCase|
|Methoden|`MovePlayer()`, `HandleInput()`|PascalCase|
|Variablen|`playerSpeed`, `isAlive`|camelCase|
|Konstanten|`MAX_HEALTH`, `PLAYER_LAYER`|UPPER_CASE|

### Beispiel Aufbaustruktur:

`using ..... public class Name { public variablen; .... private variablen; .... void Awake(){ } void Start(){ } void Update(){ } eigene Methoden(){ } }`

### Architekturprinzipien:

Trennung von Logik vs Darstellung

Single Responsibility

->Jede Klasse hat eine klar definierte Aufgabe.

Manager-Klassen verwalten mehrere Entitäten oder Systeme.

### Zusätzliches:

Kommentare, dort wo nötig. Code sollte sich größtenteils selbst erklären.