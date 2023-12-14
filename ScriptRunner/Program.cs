using System;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main()
    {
        // Replace with your actual SQL Server connection string
        string connectionString = "Server=localhost;Database=RFSMZ1;User Id=rufus;Password=rufus123;";

        // Replace with the path to the folder containing your scripts
        string scriptsFolder = @"C:\Users\patry\Desktop\Export";

        // Replace with the path to sqlcmd executable (adjust the path accordingly)
        string sqlcmdPath = @"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe";

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
            // Get all script files in the folder
            string[] scriptFiles = Directory.GetFiles(folderPath, "*.sql");

            foreach (string scriptFile in scriptFiles)
            {
                try
                {
                    // Construct the output file name
                    string baseFileName = Path.GetFileNameWithoutExtension(scriptFile);
                    string outputFileName = Path.Combine(outputFolder, $"{baseFileName}_output.txt");

                    // Run sqlcmd command to execute the script and redirect output to a file
                    string commandArguments = $"-S localhost -d RFSMZ2 -U rufus -P rufus123 -i \"{scriptFile}\" -o \"{outputFileName}\"";
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
}
