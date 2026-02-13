using System.Security.Cryptography;

namespace CryptoSoft;

public static class AesGcmEncryptor
{
    private const int NonceSize = 12; // 96 bits - taille recommandée NIST pour GCM
    private const int TagSize = 16;   // 128 bits - taille du tag d'authentification
    private const int KeySize = 32;   // 256 bits - AES-256
    private const int BufferSize = 1024 * 1024; // 1 MB - buffer pour streaming
    private const long MaxMemoryLoad = 2L * 1024 * 1024 * 1024; // 2GB - limite pour chargement mémoire

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

            if (inputStream.Length <= MaxMemoryLoad)
            {
                // Fichiers <= 2GB : AES-GCM standard
                return EncryptWithAesGcm(inputStream, outputStream, key);
            }
            else
            {
                // Fichiers > 2GB : ChaCha20-Poly1305 streaming
                return EncryptWithChaCha20Poly1305(inputStream, outputStream, key);
            }
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

    private static int EncryptWithAesGcm(FileStream inputStream, FileStream outputStream, byte[] key)
    {
        byte[] nonce = new byte[NonceSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

        // Header: [mode_flag:1][nonce:12][tag_placeholder:16]
        outputStream.WriteByte(0x01); // Mode AES-GCM
        outputStream.Write(nonce, 0, NonceSize);
        byte[] placeholderTag = new byte[TagSize];
        outputStream.Write(placeholderTag, 0, TagSize);

        using var aesGcm = new AesGcm(key, TagSize);

        if (inputStream.Length == 0)
        {
            // Fichier vide
            byte[] emptyTag = new byte[TagSize];
            aesGcm.Encrypt(nonce, ReadOnlySpan<byte>.Empty, Span<byte>.Empty, emptyTag);

            outputStream.Seek(1 + NonceSize, SeekOrigin.Begin);
            outputStream.Write(emptyTag, 0, TagSize);
        }
        else
        {
            // Charger tout en mémoire pour AES-GCM
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

            outputStream.Write(ciphertext, 0, ciphertext.Length);

            // Écrire le vrai tag
            outputStream.Seek(1 + NonceSize, SeekOrigin.Begin);
            outputStream.Write(tag, 0, TagSize);
        }

        Console.WriteLine($"✓ Fichier chiffré avec succès (AES-GCM) : {outputStream.Name}");
        Console.WriteLine($"  Taille originale : {inputStream.Length:N0} octets");
        Console.WriteLine($"  Taille chiffrée : {outputStream.Length:N0} octets");

        return 0;
    }

    private static int EncryptWithChaCha20Poly1305(FileStream inputStream, FileStream outputStream, byte[] key)
    {
        byte[] nonce = new byte[12]; // ChaCha20 utilise 12 octets
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

        // Header: [mode_flag:1][nonce:12]
        outputStream.WriteByte(0x02); // Mode ChaCha20-Poly1305
        outputStream.Write(nonce, 0, 12);

        using var chacha = new ChaCha20Poly1305(key);
        const int chunkSize = 64 * 1024; // 64KB chunks pour streaming optimal
        byte[] buffer = new byte[chunkSize];
        byte[] encryptedBuffer = new byte[chunkSize + 16]; // +16 pour le tag par chunk
        long chunkIndex = 0;

        while (true)
        {
            int bytesRead = inputStream.Read(buffer, 0, chunkSize);
            if (bytesRead == 0) break;

            // Nonce unique par chunk (nonce de base + compteur)
            byte[] chunkNonce = new byte[12];
            Array.Copy(nonce, chunkNonce, 12);
            // Ajouter l'index du chunk aux 8 derniers octets du nonce
            byte[] indexBytes = BitConverter.GetBytes(chunkIndex);
            for (int i = 0; i < 8 && i < indexBytes.Length; i++)
            {
                chunkNonce[4 + i] = indexBytes[i];
            }

            // Chiffrer le chunk
            chacha.Encrypt(chunkNonce, buffer.AsSpan(0, bytesRead), encryptedBuffer.AsSpan(0, bytesRead), encryptedBuffer.AsSpan(bytesRead, 16));

            // Écrire la taille du chunk + chunk chiffré + tag
            outputStream.Write(BitConverter.GetBytes(bytesRead), 0, 4);
            outputStream.Write(encryptedBuffer, 0, bytesRead + 16);

            chunkIndex++;
        }

        Console.WriteLine($"✓ Fichier chiffré avec succès (ChaCha20-Poly1305) : {outputStream.Name}");
        Console.WriteLine($"  Taille originale : {inputStream.Length:N0} octets");
        Console.WriteLine($"  Taille chiffrée : {outputStream.Length:N0} octets");
        Console.WriteLine($"  Nombre de chunks : {chunkIndex:N0}");

        return 0;
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
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize);
            partialFileCreated = true;

            if (inputStream.Length < 1)
            {
                Console.Error.WriteLine("Erreur : Fichier chiffré corrompu (vide)");
                return 4;
            }

            // Lire le flag de mode
            byte modeFlag = (byte)inputStream.ReadByte();

            if (modeFlag == 0x01)
            {
                // Mode AES-GCM
                return DecryptWithAesGcm(inputStream, outputStream, key);
            }
            else if (modeFlag == 0x02)
            {
                // Mode ChaCha20-Poly1305
                return DecryptWithChaCha20Poly1305(inputStream, outputStream, key);
            }
            else
            {
                Console.Error.WriteLine($"Erreur : Mode de chiffrement inconnu (0x{modeFlag:X2})");
                return 4;
            }
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

    private static int DecryptWithAesGcm(FileStream inputStream, FileStream outputStream, byte[] key)
    {
        if (inputStream.Length < NonceSize + TagSize)
        {
            Console.Error.WriteLine($"Erreur : Fichier AES-GCM corrompu (trop petit)");
            return 4;
        }

        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];

        inputStream.Read(nonce, 0, NonceSize);
        inputStream.Read(tag, 0, TagSize);

        long ciphertextLength = inputStream.Length - NonceSize - TagSize;

        using var aesGcm = new AesGcm(key, TagSize);

        if (ciphertextLength == 0)
        {
            // Fichier vide
            try
            {
                aesGcm.Decrypt(nonce, ReadOnlySpan<byte>.Empty, tag, Span<byte>.Empty);
            }
            catch (CryptographicException)
            {
                Console.Error.WriteLine("Erreur : Échec de l'authentification GCM pour fichier vide.");
                return 4;
            }
        }
        else
        {
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
                Console.Error.WriteLine("Erreur : Échec de l'authentification GCM.");
                return 4;
            }
        }

        Console.WriteLine($"✓ Fichier déchiffré avec succès (AES-GCM) : {outputStream.Name}");
        return 0;
    }

    private static int DecryptWithChaCha20Poly1305(FileStream inputStream, FileStream outputStream, byte[] key)
    {
        if (inputStream.Length < 12)
        {
            Console.Error.WriteLine("Erreur : Fichier ChaCha20 corrompu (trop petit)");
            return 4;
        }

        byte[] nonce = new byte[12];
        inputStream.Read(nonce, 0, 12);

        using var chacha = new ChaCha20Poly1305(key);
        long chunkIndex = 0;
        byte[] sizeBuffer = new byte[4];

        while (inputStream.Position < inputStream.Length)
        {
            // Lire la taille du chunk
            if (inputStream.Read(sizeBuffer, 0, 4) != 4) break;
            int chunkSize = BitConverter.ToInt32(sizeBuffer, 0);

            if (chunkSize <= 0 || chunkSize > 64 * 1024 + 16)
            {
                Console.Error.WriteLine($"Erreur : Taille de chunk invalide ({chunkSize})");
                return 4;
            }

            byte[] encryptedChunk = new byte[chunkSize + 16];
            if (inputStream.Read(encryptedChunk, 0, chunkSize + 16) != chunkSize + 16)
            {
                Console.Error.WriteLine("Erreur : Chunk tronqué");
                return 4;
            }

            // Reconstituer le nonce du chunk
            byte[] chunkNonce = new byte[12];
            Array.Copy(nonce, chunkNonce, 12);
            byte[] indexBytes = BitConverter.GetBytes(chunkIndex);
            for (int i = 0; i < 8 && i < indexBytes.Length; i++)
            {
                chunkNonce[4 + i] = indexBytes[i];
            }

            // Déchiffrer
            byte[] decryptedChunk = new byte[chunkSize];
            try
            {
                chacha.Decrypt(chunkNonce, encryptedChunk.AsSpan(0, chunkSize), encryptedChunk.AsSpan(chunkSize, 16), decryptedChunk);
                outputStream.Write(decryptedChunk, 0, chunkSize);
            }
            catch (CryptographicException)
            {
                Console.Error.WriteLine($"Erreur : Échec de l'authentification sur le chunk {chunkIndex}");
                return 4;
            }

            chunkIndex++;
        }

        Console.WriteLine($"✓ Fichier déchiffré avec succès (ChaCha20-Poly1305) : {outputStream.Name}");
        Console.WriteLine($"  Chunks traités : {chunkIndex:N0}");
        return 0;
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

