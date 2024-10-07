Make sure to add below appsettings in function app before using:

UserAssignedClientId = The client ID of the user managed identity
SqlConnectionString = Server=tcp:sqlserverName.database.windows.net,1433;Initial Catalog=databaseName;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
(Replace sqlserverName and databaseName with yours)
