# ​Database Importer

## ​Overview

The Database Importer is a .NET application designed to execute SQL scripts for database schema creation and data population. It uses queries to connect to a SQL Server database and run the provided SQL scripts.

## ​Components

### ​1. DatabaseExporter Application

The DatabaseExporter application is the main executable responsible for orchestrating the execution of SQL scripts. It connects to a SQL Server database and runs the provided scripts.

### ​2. Execution of SQL Scripts

The application reads SQL scripts from specified folders and executes them using the SQLCMD utility or CMD.EXE. It supports modifying scripts dynamically, such as altering tables before insertion.

## ​Configuration

### ​Connection Strings

Modify the connection string in the `Main` method to connect to the desired SQL Server database.

```
string connectionString = "Server=localhost;Database=localServer;User Id=userId;Password=password;";if(!PanWlasciciel)connectionString = "Data Source=localServer;Initial Catalog=database;Integrated Security=True;";
```

### ​Script Folders and Paths

Replace the script folders and SQLCMD executable paths as needed.

```
string scriptsFolder = @"C:\your\scripts\path\";string populationScriptFolder = @"C:\your\scripts\path\forpupulating";string sqlcmdPath = @"C:\path\to\SQLCMD.EXE";
```

## ​Execution

1. **Run the Application:**

- Execute the application to connect to the specified SQL Server database.

2. **Monitor Console Output:**

- The application will display progress messages in the console, indicating the execution progress of different SQL scripts.

3. **Review Output:**

- Check the specified output directory for the generated output files, including edited queries and execution logs.

## ​Notes

- Modify the connection strings, script folders, and SQLCMD paths according to your database and script locations.
- Ensure that the necessary permissions are granted to execute SQL scripts.
- This application supports modifying scripts dynamically, such as altering tables before insertion.
