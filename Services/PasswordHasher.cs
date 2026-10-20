using System;
using System.Security.Cryptography;

namespace ClinicQueueSystem.Services
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[16]; rng.GetBytes(salt);
                using (var pbkdf = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                    return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(pbkdf.GetBytes(32));
            }
        }
        public static bool Verify(string password, string stored)
        {
            try { var p = stored.Split(':'); if (p.Length != 2) return false; var salt = Convert.FromBase64String(p[0]); var expected = Convert.FromBase64String(p[1]); using (var pbkdf = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256)) { var actual = pbkdf.GetBytes(32); return CryptographicOperations.FixedTimeEquals(actual, expected); } }
            catch { return false; }
        }
    }
}
