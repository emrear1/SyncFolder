using System;
using System.IO;
using System.Linq;
using System.Timers;

class SyncAppMain
{
    private static string sourcePath = string.Empty;
    private static string destinationPath = string.Empty;
    private static string logFilePath = string.Empty;
    private static bool isNewLogFile = false;
    private static System.Timers.Timer syncTimer = null!;

    static void Main(string[] args)
    {
        Console.WriteLine();
        // Get source, destination and log paths with validation
        sourcePath = GetValidPath("source");
        destinationPath = GetValidPath("destination");
        logFilePath = GetValidPath("log");

        Console.WriteLine();

        //Check if the logFilePath includes a specific file name
        if (!Path.HasExtension(logFilePath))
        {
            logFilePath = Path.Combine(logFilePath, "sync.log"); // create sync.log if no specific file name is provided
        }

        // determine if the log file is new or existing
        if (!File.Exists(logFilePath))
        {
            isNewLogFile = true;
            // Create an empty log file to indicate that it's new
            File.Create(logFilePath).Close();
        }

        //interval in seconds
        int intervalInSeconds = GetIntervalInSeconds();
        Console.WriteLine();

        // Log the inputs to the console
        LogInputs(sourcePath, destinationPath, intervalInSeconds, logFilePath);
        Console.WriteLine();

        // Timer method accepts milliseconds so entry will be multiplied by 1000 to get correct milliseconds
        syncTimer = new System.Timers.Timer(intervalInSeconds * 1000);
        syncTimer.Elapsed += (sender, e) => SyncFolders(sourcePath, destinationPath);
        syncTimer.Start();

        Console.WriteLine("Synchronization started. Press [Enter] to exit.");
        Console.ReadLine();
    }

    static string GetValidPath(string pathType)
    {
        string path;
        do
        {
            Console.WriteLine($"Please enter the {pathType} path (e.g., C:\\{pathType}Folder):");
            path = Console.ReadLine();
            if (pathType == "log")
            {
                // extra check for the log file
                string? directoryPath = Path.GetDirectoryName(path);
                if (directoryPath == null || !Directory.Exists(directoryPath))
                {
                    Console.WriteLine($"Invalid path. Please enter a valid {pathType} path.");
                    path = string.Empty;
                }
            }
            else if (!Directory.Exists(path))
            {
                Console.WriteLine($"Invalid path. Please enter a valid {pathType} path.");
            }
        } while (string.IsNullOrWhiteSpace(path));
        return path;
    }

    static int GetIntervalInSeconds()
    {
        int intervalInSeconds;
        Console.WriteLine("Please enter the interval in seconds (e.g., 60):");
        while (!int.TryParse(Console.ReadLine(), out intervalInSeconds))
        {
            Console.WriteLine("Invalid input. Please enter a valid number for the interval in seconds:");
        }
        return intervalInSeconds;
    }

    static void LogInputs(string sourcePath, string destinationPath, int intervalInSeconds, string logFilePath)
    {
        Console.WriteLine($"Source Path: {sourcePath}");
        Console.WriteLine($"Destination Path: {destinationPath}");
        Console.WriteLine($"Interval (in seconds): {intervalInSeconds}");
        Console.WriteLine($"Log File Path: {logFilePath}");

        string logFileName = Path.GetFileName(logFilePath);

        if (isNewLogFile)
        {
            Console.WriteLine($"Log file {logFileName} created.");
        }
        else
        {
            Console.WriteLine($"The existing log file {logFileName} will be updated with new entries.");
        }
    }

    static void SyncFolders(string sourcePath, string destinationPath)
    {
        try
        {
            // Ensure the destination directory exists
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            // Get files
            var sourceFiles = Directory.GetFiles(sourcePath);
            var destFiles = Directory.GetFiles(destinationPath);

            // Copy new and updated files
            foreach (var filePath in sourceFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var destFilePath = Path.Combine(destinationPath, fileName);
                if (!File.Exists(destFilePath) || File.GetLastWriteTime(filePath) > File.GetLastWriteTime(destFilePath))
                {
                    File.Copy(filePath, destFilePath, true);
                    Log($"Copied: {filePath} to {destFilePath}");
                }
            }

            // Delete files not existing
            foreach (var filePath in destFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var sourceFilePath = Path.Combine(sourcePath, fileName);
                if (!File.Exists(sourceFilePath))
                {
                    File.Delete(filePath);
                    Log($"Deleted: {filePath}");
                }
            }
            // sync directories
            var sourceDirs = Directory.GetDirectories(sourcePath);
            var destDirs = Directory.GetDirectories(destinationPath);

            // Copy
            foreach (var dirPath in sourceDirs)
            {
                var dirName = Path.GetFileName(dirPath);
                var destDirPath = Path.Combine(destinationPath, dirName);
                SyncFolders(dirPath, destDirPath);
            }

            // Remove directories not in source
            foreach (var dirPath in destDirs)
            {
                var dirName = Path.GetFileName(dirPath);
                var sourceDirPath = Path.Combine(sourcePath, dirName);
                if (!Directory.Exists(sourceDirPath))
                {
                    Directory.Delete(dirPath, true);
                    Log($"Deleted directory: {dirPath}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    static void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
    }
}
