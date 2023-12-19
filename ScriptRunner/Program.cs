using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        // Replace with your actual SQL Server connection string
        //string connectionString = "Data Source=(localdb)\\local;Initial Catalog=RFSMZ1;Integrated Security=True;";
        string connectionString = "Server=localhost;Database=RFSMZ1;User Id=rufus;Password=rufus123;";

        // Replace with the path to the folder containing your scripts
        //string scriptsFolder = @"D:\Praktyki\Fork\bravura\Rufus\Database\Export";
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

        // Remove errors containing "Cannot insert duplicate key in object"
        RemoveSpecificErrors(outputFolder, "Cannot insert duplicate key in object");

        // Remove errors containing "There is already an object named"
        RemoveSpecificErrors(outputFolder, "There is already an object named");

        // Remove errors containing "The statement has been terminated"
        RemoveSpecificErrors(outputFolder, "The statement has been terminated");
    }

    static void ExecuteScriptsInFolder(string connectionString, string folderPath, string outputFolder, string sqlcmdPath)
    {
        try
        {
            string[] scriptFiles = Directory.GetFiles(folderPath, "*.sql");
            int i = 0;
            foreach (string scriptFile in scriptFiles)
            {
                i++;
                try
                {
                    string scriptContent = File.ReadAllText(scriptFile);

                    // If a valid table name is found, modify the script content 
                    if (Path.GetFileName(scriptFile).ToLower().StartsWith("dbo"))
                    {
                        // Extract table name from the first INSERT statement
                        string tableName = ExtractTableName(scriptContent);
                        //scriptContent = scriptContent.Replace($"INSERT {tableName} ON", $"ALTER TABLE {tableName} NOCHECK CONSTRAINT ALL; GO{Environment.NewLine}INSERT {tableName} ON");
                        scriptContent = scriptContent.Replace($"SET IDENTITY_INSERT {tableName} ON", $"ALTER TABLE {tableName} NOCHECK CONSTRAINT ALL;{Environment.NewLine}GO{Environment.NewLine}SET IDENTITY_INSERT {tableName} ON");

                        // Find the index of "SET IDENTITY_INSERT {tableName} OFF"
                        int indexOfIdentityInsert = scriptContent.IndexOf($"SET IDENTITY_INSERT {tableName} OFF", StringComparison.OrdinalIgnoreCase);

                        // If the index is found, insert the ALTER TABLE statement before it
                        if (indexOfIdentityInsert != -1)
                        {
                            scriptContent = scriptContent.Insert(indexOfIdentityInsert,
                                $"ALTER TABLE {tableName} WITH CHECK CHECK CONSTRAINT ALL;{Environment.NewLine}GO{Environment.NewLine}");
                        }

                        if (tableName == "dbo.Client" || tableName == "RFSMZ1.dbo.Client")
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
                    string commandArguments = $"-S localhost -d RFSMZ1 -U rufus -P rufus123 -i \"{tempScriptFile}\" -o \"{outputFileName}\"";
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
        Match match = Regex.Match(scriptContent, @"INSERT\s+(?i:INTO\s+)?\s*([a-zA-Z_]\w*\.[a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)?|\w+)", RegexOptions.IgnoreCase);
        if (match.Success && match.Groups[1].Value.ToLower() != "into")
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    static void RemoveSpecificErrors(string outputFolder, string errorMessage)
    {
        try
        {
            string[] outputFiles = Directory.GetFiles(outputFolder, "*_output.txt");

            foreach (string outputFile in outputFiles)
            {
                try
                {
                    string[] lines = File.ReadAllLines(outputFile);
                    bool errorFound = false;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        // Check if the line contains the specified error message
                        if (lines[i].Contains(errorMessage))
                        {
                            // Remove the current line and the line above it
                            lines[i] = "";
                            if (i > 0) lines[i - 1] = "";

                            errorFound = true;
                        }
                    }

                    if (errorFound)
                    {
                        // Write the modified content back to the file
                        File.WriteAllLines(outputFile, lines);
                        Console.WriteLine($"Errors removed from: {outputFile}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing output file {Path.GetFileName(outputFile)}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}