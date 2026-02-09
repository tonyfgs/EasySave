using System.Security.Cryptography;

namespace CryptoSoft;

public static class AesGcmEncryptor
{
    private const int NonceSize = 12; // 96 bits - taille recommandée NIST pour GCM
    private const int TagSize = 16;   // 128 bits - taille du tag d'authentification
    private const int KeySize = 32;   // 256 bits - AES-256
    private const int BufferSize = 81920; // 80 Ko - buffer pour streaming

 
    public static int EncryptFile(string inputPath, string keyBase64)
    {
        try
        {
            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"Erreur : Fichier source introuvable - {inputPath}");
                return 1;
            }

            byte[] key;
            try
            {
                key = Convert.FromBase64String(keyBase64);
                if (key.Length != KeySize)
                {
                    Console.Error.WriteLine($"Erreur : La clé doit faire exactement 32 octets (256 bits). Taille reçue : {key.Length} octets");
                    return 5;
                }
            }
            catch (FormatException)
            {
                Console.Error.WriteLine("Erreur : La clé fournie n'est pas un Base64 valide");
                return 5;
            }

            byte[] nonce = new byte[NonceSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            string outputPath = inputPath + ".crypt";

            using var aesGcm = new AesGcm(key, TagSize);
            using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize);

            outputStream.Write(nonce, 0, NonceSize);
            
            byte[] tag = new byte[TagSize];
            outputStream.Write(tag, 0, TagSize);

            byte[] plaintext = new byte[inputStream.Length];
            int bytesRead = inputStream.Read(plaintext, 0, plaintext.Length);
            
            byte[] ciphertext = new byte[bytesRead];
            
            aesGcm.Encrypt(nonce, plaintext.AsSpan(0, bytesRead), ciphertext, tag);
            
            outputStream.Write(ciphertext, 0, ciphertext.Length);
            
            outputStream.Seek(NonceSize, SeekOrigin.Begin);
            outputStream.Write(tag, 0, TagSize);

            Console.WriteLine($"✓ Fichier chiffré avec succès : {outputPath}");
            Console.WriteLine($"  Taille originale : {bytesRead:N0} octets");
            Console.WriteLine($"  Taille chiffrée : {(NonceSize + TagSize + bytesRead):N0} octets");
            
            return 0;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Erreur : Accès refusé - {ex.Message}");
            return 3;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Erreur I/O : {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erreur inattendue : {ex.Message}");
            return 3;
        }
    }

   
    public static int DecryptFile(string inputPath, string keyBase64)
    {
        try
        {
            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"Erreur : Fichier source introuvable - {inputPath}");
                return 1;
            }

            byte[] key;
            try
            {
                key = Convert.FromBase64String(keyBase64);
                if (key.Length != KeySize)
                {
                    Console.Error.WriteLine($"Erreur : La clé doit faire exactement 32 octets (256 bits). Taille reçue : {key.Length} octets");
                    return 5;
                }
            }
            catch (FormatException)
            {
                Console.Error.WriteLine("Erreur : La clé fournie n'est pas un Base64 valide");
                return 5;
            }

            string outputPath = inputPath.EndsWith(".crypt", StringComparison.OrdinalIgnoreCase)
                ? inputPath[..^6]
                : inputPath + ".decrypted";

            using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);

            if (inputStream.Length < NonceSize + TagSize)
            {
                Console.Error.WriteLine($"Erreur : Fichier chiffré corrompu (trop petit). Taille : {inputStream.Length} octets, minimum requis : {NonceSize + TagSize}");
                return 4;
            }

            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            
            inputStream.Read(nonce, 0, NonceSize);
            inputStream.Read(tag, 0, TagSize);

            int ciphertextLength = (int)(inputStream.Length - NonceSize - TagSize);
            byte[] ciphertext = new byte[ciphertextLength];
            inputStream.Read(ciphertext, 0, ciphertextLength);

            byte[] plaintext = new byte[ciphertextLength];
            
            using var aesGcm = new AesGcm(key, TagSize);
            
            try
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            }
            catch (CryptographicException)
            {
                Console.Error.WriteLine("Erreur : Échec de l'authentification GCM.");
                Console.Error.WriteLine("Causes possibles :");
                Console.Error.WriteLine("  - Mauvaise clé de déchiffrement");
                Console.Error.WriteLine("  - Fichier corrompu ou altéré");
                Console.Error.WriteLine("  - Fichier non chiffré avec CryptoSoft");
                return 4;
            }

            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize);
            outputStream.Write(plaintext, 0, plaintext.Length);

            Console.WriteLine($"✓ Fichier déchiffré avec succès : {outputPath}");
            Console.WriteLine($"  Taille déchiffrée : {plaintext.Length:N0} octets");
            
            return 0;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Erreur : Accès refusé - {ex.Message}");
            return 3;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Erreur I/O : {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erreur inattendue : {ex.Message}");
            return 3;
        }
    }
    
    public static string GenerateKey()
    {
        byte[] key = new byte[KeySize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        return Convert.ToBase64String(key);
    }
}

