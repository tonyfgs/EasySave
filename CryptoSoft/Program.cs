namespace CryptoSoft;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  CryptoSoft v1.2 - AES-256-GCM Encryption Tool");
        Console.WriteLine("  Developed for EasySave (Client-Server TCP Mode)");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        if (args.Length == 0)
        {
            ShowUsage();
            return 2;
        }

        string operation = args[0].ToLowerInvariant();

        return operation switch
        {
            "server" => StartServer(),
            "genkey" => HandleGenerateKey(),
            "help" => ShowHelp(),
            "encrypt" => HandleEncryptClient(args),
            "decrypt" => HandleDecryptClient(args),
            _ => HandleUnknownOperation(operation)
        };
    }

    private static int StartServer()
    {
        var server = new CryptoServer();
        return server.Run();
    }

    private static int HandleEncryptClient(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Error: Insufficient number of arguments.");
            ShowUsage();
            return 2;
        }

        string filePath = args[1];
        string keyBase64 = args[2];

        if (string.IsNullOrWhiteSpace(keyBase64) || string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("Error: Path or key cannot be empty.");
            return 2;
        }

        // Check if the server is running
        if (CryptoClient.IsServerRunning())
        {
            Console.WriteLine("Connecting to CryptoSoft server...");
            var client = new CryptoClient();
            var response = client.Encrypt(filePath, keyBase64);

            if (response.Success)
            {
                Console.WriteLine($"Encryption succeeded in {response.DurationMs}ms");
                return 0;
            }
            else
            {
                Console.Error.WriteLine($"Failed: {response.ErrorMessage}");
                return response.ExitCode;
            }
        }
        else
        {
            // Standalone mode (no server)
            Console.WriteLine($"Direct encryption of: {filePath}");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int exitCode = AesGcmEncryptor.EncryptFile(filePath, keyBase64);
            stopwatch.Stop();

            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Exit code: {exitCode} ({GetExitCodeDescription(exitCode)})");
            return exitCode;
        }
    }

    private static int HandleDecryptClient(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Error: Insufficient number of arguments.");
            ShowUsage();
            return 2;
        }

        string filePath = args[1];
        string keyBase64 = args[2];

        if (string.IsNullOrWhiteSpace(keyBase64) || string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("Error: Path or key cannot be empty.");
            return 2;
        }

        // Check if the server is running
        if (CryptoClient.IsServerRunning())
        {
            Console.WriteLine("Connecting to CryptoSoft server...");
            var client = new CryptoClient();
            var response = client.Decrypt(filePath, keyBase64);

            if (response.Success)
            {
                Console.WriteLine($"Decryption succeeded in {response.DurationMs}ms");
                return 0;
            }
            else
            {
                Console.Error.WriteLine($"Failed: {response.ErrorMessage}");
                return response.ExitCode;
            }
        }
        else
        {
            // Standalone mode (no server)
            Console.WriteLine($"Direct decryption of: {filePath}");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int exitCode = AesGcmEncryptor.DecryptFile(filePath, keyBase64);
            stopwatch.Stop();

            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Exit code: {exitCode} ({GetExitCodeDescription(exitCode)})");
            return exitCode;
        }
    }

    private static int HandleGenerateKey()
    {
        string newKey = AesGcmEncryptor.GenerateKey();
        Console.WriteLine("New AES-256 key generated:");
        Console.WriteLine();
        Console.WriteLine($"  {newKey}");
        Console.WriteLine();
        Console.WriteLine("IMPORTANT: Save this key in a safe place!");
        Console.WriteLine("  This key is required to decrypt your files.");
        Console.WriteLine("  CryptoSoft does not store any keys - this is your responsibility.");
        return 0;
    }

    private static int ShowHelp()
    {
        ShowUsage();
        return 0;
    }

    private static int HandleUnknownOperation(string operation)
    {
        Console.Error.WriteLine($"Error: Unknown operation '{operation}'");
        Console.Error.WriteLine();
        ShowUsage();
        return 2;
    }

    private static void ShowUsage()
    {
        Console.WriteLine("USAGE:");
        Console.WriteLine();
        Console.WriteLine("  Start the server (single-instance):");
        Console.WriteLine("    CryptoSoft.exe server");
        Console.WriteLine("    -> Starts the TCP server that accepts requests from multiple jobs");
        Console.WriteLine("    -> Encryptions are processed one at a time (single-instance)");
        Console.WriteLine();
        Console.WriteLine("  Key generation:");
        Console.WriteLine("    CryptoSoft.exe genkey");
        Console.WriteLine("    -> Generates a new AES-256 key. Save it yourself!");
        Console.WriteLine();
        Console.WriteLine("  Encryption:");
        Console.WriteLine("    CryptoSoft.exe encrypt \"C:\\folder\\file.pdf\" \"base64Key==\"");
        Console.WriteLine("    -> If server active: sends request to server");
        Console.WriteLine("    -> Otherwise: encrypts directly (standalone fallback)");
        Console.WriteLine();
        Console.WriteLine("  Decryption:");
        Console.WriteLine("    CryptoSoft.exe decrypt \"C:\\folder\\file.pdf.crypt\" \"base64Key==\"");
        Console.WriteLine("    -> Same behavior as encrypt");
        Console.WriteLine();
        Console.WriteLine("  Help:");
        Console.WriteLine("    CryptoSoft.exe help");
        Console.WriteLine();
        Console.WriteLine("EXIT CODES:");
        Console.WriteLine("  0 - Success");
        Console.WriteLine("  1 - Source file not found / unreadable");
        Console.WriteLine("  2 - Invalid arguments");
        Console.WriteLine("  3 - I/O error");
        Console.WriteLine("  4 - GCM authentication failure");
        Console.WriteLine("  5 - Invalid key");
        Console.WriteLine("  6 - Server already running / Connection timeout");
        Console.WriteLine();
        Console.WriteLine("ARCHITECTURE:");
        Console.WriteLine("  - Server mode: One server, multiple simultaneous clients");
        Console.WriteLine("  - Single-instance: Encryptions are processed one at a time");
        Console.WriteLine("  - Standalone mode: If no server, direct encryption");
        Console.WriteLine("  - TCP localhost: Cross-platform communication (Win/Linux/Mac)");
        Console.WriteLine($"  - Default port: {CryptoServer.DefaultPort}");
    }

    private static string GetExitCodeDescription(int code)
    {
        return code switch
        {
            0 => "Success",
            1 => "File not found",
            2 => "Invalid arguments",
            3 => "I/O error",
            4 => "GCM authentication failure",
            5 => "Invalid key",
            6 => "Instance already running / Timeout",
            _ => "Unknown code"
        };
    }
}
