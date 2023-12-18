using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        // Replace with your actual SQL Server connection string
        string connectionString = "Server=localhost;Database=RFSMZ1;User Id=rufus;Password=rufus123;";

        // Replace with the path to the folder containing your scripts
        string scriptsFolder = @"C:\Users\patry\Desktop\Export";

        // Replace with the path to sqlcmd executable (adjust the path accordingly)
        string sqlcmdPath = @"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE";

        // Create an output folder in the script folder
        string outputFolder = Path.Combine(scriptsFolder, "Output");
        Directory.CreateDirectory(outputFolder);

        // Run the scripts three times
        for (int i = 0; i < 3; i++)
        {
            Console.WriteLine($"Execution Round: {i + 1}");
            ExecuteScriptsInFolder(connectionString, scriptsFolder, outputFolder, sqlcmdPath);
        }
    }

    static void ExecuteScriptsInFolder(string connectionString, string folderPath, string outputFolder, string sqlcmdPath)
    {
        try
        {
            string[] scriptFiles = Directory.GetFiles(folderPath, "*.sql");

            foreach (string scriptFile in scriptFiles)
            {
                try
                {
                    string scriptContent = File.ReadAllText(scriptFile);

                    // Extract table name from the first INSERT statement
                    string tableName = ExtractTableName(scriptContent);

                    // If a valid table name is found, modify the script content
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        // Modify the INSERT statement
                        scriptContent = scriptContent.Replace($"INSERT {tableName} ON", $"ALTER TABLE {tableName} NOCHECK CONSTRAINT ALL; GO{Environment.NewLine}INSERT {tableName} ON");

                        // Find the index of "SET IDENTITY_INSERT {tableName} OFF"
                        int indexOfIdentityInsert = scriptContent.IndexOf($"SET IDENTITY_INSERT {tableName} OFF", StringComparison.OrdinalIgnoreCase);

                        // If the index is found, insert the ALTER TABLE statement before it
                        if (indexOfIdentityInsert != -1)
                        {
                            scriptContent = scriptContent.Insert(indexOfIdentityInsert,
                                $"ALTER TABLE {tableName} WITH CHECK CHECK CONSTRAINT ALL;{Environment.NewLine}");
                        }

                        if (tableName == "dbo.Client")
                        {
                            string editedQueryFile = Path.Combine(outputFolder, "Edited_Query.txt");
                            File.WriteAllText(editedQueryFile, scriptContent);
                            Console.WriteLine($"Edited query for dbo.Client written to: {editedQueryFile}");
                        }
                    }

                    // Save the modified script content to a temporary file
                    string tempScriptFile = Path.Combine(outputFolder, "temp.sql");
                    File.WriteAllText(tempScriptFile, scriptContent);

                    // Run sqlcmd command to execute the modified script and redirect output to a file
                    string outputFileName = Path.Combine(outputFolder, $"{Path.GetFileNameWithoutExtension(scriptFile)}_output.txt");
                    string commandArguments = $"-S localhost -d RFSMZ2 -U rufus -P rufus123 -i \"{tempScriptFile}\" -o \"{outputFileName}\"";
                    ProcessStartInfo psi = new ProcessStartInfo(sqlcmdPath, commandArguments);
                    psi.RedirectStandardOutput = true;
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;

                    using (Process process = new Process())
                    {
                        process.StartInfo = psi;
                        process.Start();
                        process.WaitForExit();

                        Console.WriteLine($"Script {Path.GetFileName(scriptFile)} executed successfully. Output written to {outputFileName}");
                    }

                    // Delete the temporary script file
                    File.Delete(tempScriptFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing script {Path.GetFileName(scriptFile)}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static string ExtractTableName(string scriptContent)
    {
        // Use a regular expression to find the first INSERT statement and extract the table name
        Match match = Regex.Match(scriptContent, @"INSERT\s+(?:INTO\s+)?\s*(\w+\.\w+|\w+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}
