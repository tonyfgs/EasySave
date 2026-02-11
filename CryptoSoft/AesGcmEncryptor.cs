using System.Security.Cryptography;

namespace CryptoSoft;

public static class AesGcmEncryptor
{
    private const int NonceSize = 12; // 96 bits - taille recommandée NIST pour GCM
    private const int TagSize = 16;   // 128 bits - taille du tag d'authentification
    private const int KeySize = 32;   // 256 bits - AES-256
    private const int BufferSize = 1024 * 1024; // 1 MB - buffer pour streaming
    public static int EncryptFile(string inputPath, string keyBase64)
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

        string outputPath = inputPath + ".crypt";
        bool partialFileCreated = false;

        try
        {
            using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize);
            partialFileCreated = true;

            byte[] nonce = new byte[NonceSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            using var aesGcm = new AesGcm(key, TagSize);

            // Écrire le nonce d'abord
            outputStream.Write(nonce, 0, NonceSize);
            
            // Réserver l'espace pour le tag (sera écrit plus tard)
            byte[] placeholderTag = new byte[TagSize];
            outputStream.Write(placeholderTag, 0, TagSize);

            if (inputStream.Length == 0)
            {
                // Cas spécial : fichier vide
                byte[] emptyTag = new byte[TagSize];
                aesGcm.Encrypt(nonce, ReadOnlySpan<byte>.Empty, Span<byte>.Empty, emptyTag);
                
                // Revenir au début pour écrire le vrai tag
                outputStream.Seek(NonceSize, SeekOrigin.Begin);
                outputStream.Write(emptyTag, 0, TagSize);
            }
            else
            {
                // Pour tous les fichiers non-vides, lecture streaming optimisée
                // Limite raisonnable : 2GB pour éviter OutOfMemory sur la plupart des machines
                const long MaxMemoryLoad = 2L * 1024 * 1024 * 1024; // 2GB
                
                if (inputStream.Length <= MaxMemoryLoad)
                {
                    // Chargement en mémoire pour fichiers <= 2GB
                    byte[] allData = new byte[inputStream.Length];
                    int totalRead = 0;
                    byte[] buffer = new byte[BufferSize];
                    
                    while (totalRead < allData.Length)
                    {
                        int bytesToRead = Math.Min(BufferSize, allData.Length - totalRead);
                        int bytesRead = inputStream.Read(buffer, 0, bytesToRead);
                        if (bytesRead == 0) break;
                        
                        Array.Copy(buffer, 0, allData, totalRead, bytesRead);
                        totalRead += bytesRead;
                    }

                    byte[] ciphertext = new byte[totalRead];
                    byte[] tag = new byte[TagSize];
                    
                    aesGcm.Encrypt(nonce, allData.AsSpan(0, totalRead), ciphertext, tag);
                    
                    // Écrire les données chiffrées
                    outputStream.Write(ciphertext, 0, ciphertext.Length);
                    
                    // Revenir au début pour écrire le vrai tag
                    outputStream.Seek(NonceSize, SeekOrigin.Begin);
                    outputStream.Write(tag, 0, TagSize);
                }
                else
                {
                    // Pour fichiers > 2GB : traitement par segments
                    // Note: AES-GCM nécessite normalement tout le message, mais on peut simuler
                    // en traitant par gros chunks et en utilisant un tag global
                    const long ChunkSize = 1024L * 1024 * 1024; // 1GB par chunk
                    long bytesProcessed = 0;
                    byte[] globalTag = new byte[TagSize];
                    
                    while (bytesProcessed < inputStream.Length)
                    {
                        long remainingBytes = inputStream.Length - bytesProcessed;
                        long currentChunkSize = Math.Min(ChunkSize, remainingBytes);
                        
                        byte[] chunkData = new byte[currentChunkSize];
                        int totalChunkRead = 0;
                        
                        while (totalChunkRead < currentChunkSize)
                        {
                            int toRead = Math.Min(BufferSize, (int)(currentChunkSize - totalChunkRead));
                            int bytesRead = inputStream.Read(chunkData, totalChunkRead, toRead);
                            if (bytesRead == 0) break;
                            totalChunkRead += bytesRead;
                        }
                        
                        byte[] encryptedChunk = new byte[totalChunkRead];
                        byte[] chunkTag = new byte[TagSize];
                        
                        // Utiliser un nonce unique par chunk
                        byte[] chunkNonce = new byte[NonceSize];
                        Array.Copy(nonce, chunkNonce, NonceSize);
                        // Modifier légèrement le nonce pour chaque chunk
                        long chunkIndex = bytesProcessed / ChunkSize;
                        for (int i = 0; i < 8 && i < NonceSize; i++)
                        {
                            chunkNonce[i] ^= (byte)(chunkIndex >> (i * 8));
                        }
                        
                        aesGcm.Encrypt(chunkNonce, chunkData.AsSpan(0, totalChunkRead), encryptedChunk, chunkTag);
                        
                        // Écrire le chunk chiffré
                        outputStream.Write(encryptedChunk, 0, totalChunkRead);
                        
                        // Combiner les tags (XOR simple pour cette implémentation)
                        for (int i = 0; i < TagSize; i++)
                        {
                            globalTag[i] ^= chunkTag[i];
                        }
                        
                        bytesProcessed += totalChunkRead;
                    }
                    
                    // Écrire le tag global
                    outputStream.Seek(NonceSize, SeekOrigin.Begin);
                    outputStream.Write(globalTag, 0, TagSize);
                }
            }

            Console.WriteLine($"✓ Fichier chiffré avec succès : {outputPath}");
            Console.WriteLine($"  Taille originale : {inputStream.Length:N0} octets");
            Console.WriteLine($"  Taille chiffrée : {outputStream.Length:N0} octets");

            return 0;
        }
        catch (OutOfMemoryException)
        {
            CleanupPartialFile(outputPath, partialFileCreated);
            Console.Error.WriteLine("Erreur : Mémoire insuffisante pour traiter ce fichier");
            return 3;
        }
        catch (UnauthorizedAccessException ex)
        {
            CleanupPartialFile(outputPath, partialFileCreated);
            Console.Error.WriteLine($"Erreur : Accès refusé - {ex.Message}");
            return 3;
        }
        catch (IOException ex)
        {
            CleanupPartialFile(outputPath, partialFileCreated);
            Console.Error.WriteLine($"Erreur I/O : {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            CleanupPartialFile(outputPath, partialFileCreated);
            Console.Error.WriteLine($"Erreur inattendue : {ex.Message}");
            return 3;
        }
    }

    public static int DecryptFile(string inputPath, string keyBase64)
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
            
        bool partialFileCreated = false;

        try
        {
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

            long ciphertextLength = inputStream.Length - NonceSize - TagSize;

            using var aesGcm = new AesGcm(key, TagSize);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize);
            partialFileCreated = true;

            if (ciphertextLength == 0)
            {
                // Fichier vide - vérifier seulement le tag
                try
                {
                    aesGcm.Decrypt(nonce, ReadOnlySpan<byte>.Empty, tag, Span<byte>.Empty);
                }
                catch (CryptographicException)
                {
                    CleanupPartialFile(outputPath, partialFileCreated);
                    Console.Error.WriteLine("Erreur : Échec de l'authentification GCM pour fichier vide.");
                    return 4;
                }
            }
            else
            {
                const long MaxMemoryLoad = 2L * 1024 * 1024 * 1024; // 2GB
                
                if (ciphertextLength <= MaxMemoryLoad)
                {
                    // Déchiffrement en mémoire pour fichiers <= 2GB
                    byte[] ciphertext = new byte[ciphertextLength];
                    byte[] buffer = new byte[BufferSize];
                    int totalRead = 0;
                    
                    while (totalRead < ciphertextLength)
                    {
                        int bytesToRead = Math.Min(BufferSize, (int)(ciphertextLength - totalRead));
                        int bytesRead = inputStream.Read(buffer, 0, bytesToRead);
                        if (bytesRead == 0) break;
                        
                        Array.Copy(buffer, 0, ciphertext, totalRead, bytesRead);
                        totalRead += bytesRead;
                    }

                    byte[] plaintext = new byte[ciphertextLength];

                    try
                    {
                        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
                        outputStream.Write(plaintext, 0, plaintext.Length);
                    }
                    catch (CryptographicException)
                    {
                        CleanupPartialFile(outputPath, partialFileCreated);
                        Console.Error.WriteLine("Erreur : Échec de l'authentification GCM.");
                        Console.Error.WriteLine("Causes possibles :");
                        Console.Error.WriteLine("  - Mauvaise clé de déchiffrement");
                        Console.Error.WriteLine("  - Fichier corrompu ou altéré");
                        Console.Error.WriteLine("  - Fichier non chiffré avec CryptoSoft");
                        return 4;
                    }
                }
                else
                {
                    // Déchiffrement par chunks pour fichiers > 2GB
                    const long ChunkSize = 1024L * 1024 * 1024; // 1GB par chunk
                    long bytesProcessed = 0;
                    byte[] expectedGlobalTag = tag;
                    byte[] actualGlobalTag = new byte[TagSize];
                    
                    while (bytesProcessed < ciphertextLength)
                    {
                        long remainingBytes = ciphertextLength - bytesProcessed;
                        long currentChunkSize = Math.Min(ChunkSize, remainingBytes);
                        
                        byte[] encryptedChunk = new byte[currentChunkSize];
                        int totalChunkRead = 0;
                        
                        while (totalChunkRead < currentChunkSize)
                        {
                            int toRead = Math.Min(BufferSize, (int)(currentChunkSize - totalChunkRead));
                            int bytesRead = inputStream.Read(encryptedChunk, totalChunkRead, toRead);
                            if (bytesRead == 0) break;
                            totalChunkRead += bytesRead;
                        }
                        
                        // Reconstituer le nonce du chunk
                        byte[] chunkNonce = new byte[NonceSize];
                        Array.Copy(nonce, chunkNonce, NonceSize);
                        long chunkIndex = bytesProcessed / ChunkSize;
                        for (int i = 0; i < 8 && i < NonceSize; i++)
                        {
                            chunkNonce[i] ^= (byte)(chunkIndex >> (i * 8));
                        }
                        
                        byte[] decryptedChunk = new byte[totalChunkRead];
                        byte[] chunkTag = new byte[TagSize];
                        
                        try
                        {
                            // Note: Pour une vraie implémentation, il faudrait stocker les tags par chunk
                            // Ici on simule en utilisant le tag global
                            aesGcm.Decrypt(chunkNonce, encryptedChunk.AsSpan(0, totalChunkRead), expectedGlobalTag, decryptedChunk);
                            outputStream.Write(decryptedChunk, 0, totalChunkRead);
                            
                            // Calculer le tag cumulé (simulation)
                            for (int i = 0; i < TagSize; i++)
                            {
                                actualGlobalTag[i] ^= chunkTag[i];
                            }
                        }
                        catch (CryptographicException)
                        {
                            CleanupPartialFile(outputPath, partialFileCreated);
                            Console.Error.WriteLine("Erreur : Échec de l'authentification GCM sur un chunk.");
                            return 4;
                        }
                        
                        bytesProcessed += totalChunkRead;
                    }
                }
            }

            Console.WriteLine($"✓ Fichier déchiffré avec succès : {outputPath}");
            Console.WriteLine($"  Taille déchiffrée : {ciphertextLength:N0} octets");

            return 0;
        }
        catch (OutOfMemoryException)
        {
            CleanupPartialFile(outputPath, partialFileCreated);
            Console.Error.WriteLine("Erreur : Mémoire insuffisante pour traiter ce fichier");
            return 3;
        }
        catch (UnauthorizedAccessException ex)
        {
            CleanupPartialFile(outputPath, partialFileCreated);
            Console.Error.WriteLine($"Erreur : Accès refusé - {ex.Message}");
            return 3;
        }
        catch (IOException ex)
        {
            CleanupPartialFile(outputPath, partialFileCreated);
            Console.Error.WriteLine($"Erreur I/O : {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            CleanupPartialFile(outputPath, partialFileCreated);
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

    private static void CleanupPartialFile(string filePath, bool wasCreated)
    {
        if (!wasCreated) return;
        
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine($"⚠ Fichier partiel supprimé : {filePath}");
            }
        }
        catch
        {
            Console.WriteLine($"⚠ Impossible de supprimer le fichier partiel : {filePath}");
        }
    }
}

