# EF Core Skill

## Best Practices
- Используй AsNoTracking() для read-only запросов
- Include() для eager loading
- Async методы (ToListAsync, FirstOrDefaultAsync)

## Пример
```csharp
var properties = await context.Properties
    .AsNoTracking()
    .Where(p => p.Status == "active")
    .Include(p => p.Images)
    .ToListAsync();