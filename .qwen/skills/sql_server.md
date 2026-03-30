# SQL Server Skill

## Таблицы БД
- Users (Id, Login, PasswordHash, Email, Role, FullName)
- Properties (Id, Title, Price, Area, Rooms, City, Status)
- Appointments (Id, PropertyId, ClientId, SlotStart, Status)
- Favorites (Id, UserId, PropertyId)
- Reviews (Id, UserId, Rating, Comment, IsApproved)

## Индексы
CREATE INDEX IX_Properties_Price ON Properties(Price);
CREATE INDEX IX_Properties_City ON Properties(City);