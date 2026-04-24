
API
-----
Step 1
 1 — Configure the connection string

Edit `src/API/appsettings.Development.json`:

 2 — Apply EF Core migrations   
        cd InventorySystem.API
        dotnet ef database update


 3 — Deploy the stored procedure   SQL/sp_ConsumeStockFIFO.sql
 
 4 —  Seed demo data    SQL/SeedData.sql
 
 5 — Run the API
dotnet run --project InventorySystem.API


--------------------------------------------------------------------------------
Authentication Flow

Endpoint URL
https://localhost:44350/index.html

```
1.  POST /api/auth/login
    Body: { "username": "admin", "password": "Admin@123" }
    → Returns: { "token": "eyJ...", "role": "WarehouseAdmin", "expiresAt": "..." }

2.  Copy the token value.

3.  In Swagger UI: click [Authorize] → enter:  Bearer eyJ...

4.  All subsequent requests include the JWT in the Authorization header.

--------------------------------------------------------------------------------

