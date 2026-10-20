using System;
using System.Security.Cryptography;

namespace ClinicQueueSystem.Services
{
    /// <summary>PBKDF2 password hashing compatible with .NET Framework 4.8.</summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static string Hash(string password)
        {
            if (String.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", "password");

            byte[] salt = new byte[SaltSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            using (Rfc2898DeriveBytes pbkdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf.GetBytes(HashSize);
                return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
            }
        }

        public static bool Verify(string password, string stored)
        {
            if (String.IsNullOrEmpty(password) || String.IsNullOrWhiteSpace(stored))
                return false;

            try
            {
                string[] parts = stored.Split(':');
                if (parts.Length != 2) return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expected = Convert.FromBase64String(parts[1]);
                if (salt.Length != SaltSize || expected.Length != HashSize) return false;

                using (Rfc2898DeriveBytes pbkdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] actual = pbkdf.GetBytes(HashSize);
                    return FixedTimeEquals(actual, expected);
                }
            }
            catch (FormatException) { return false; }
            catch (CryptographicException) { return false; }
        }

        // Constant-time comparison without requiring CryptographicOperations.FixedTimeEquals.
        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
