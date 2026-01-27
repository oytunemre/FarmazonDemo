using System.Security.Cryptography;
using System.Text;

namespace FarmazonDemo.Services.TwoFactor
{
    public class TwoFactorService : ITwoFactorService
    {
        private const int SecretKeyLength = 20;
        private const int CodeLength = 6;
        private const int TimeStepSeconds = 30;
        private const string Issuer = "FarmazonDemo";

        public string GenerateSecretKey()
        {
            var key = new byte[SecretKeyLength];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            return Base32Encode(key);
        }

        public string GenerateQrCodeUri(string email, string secretKey)
        {
            // otpauth://totp/Issuer:email?secret=xxx&issuer=Issuer&algorithm=SHA1&digits=6&period=30
            var encodedIssuer = Uri.EscapeDataString(Issuer);
            var encodedEmail = Uri.EscapeDataString(email);

            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}&algorithm=SHA1&digits={CodeLength}&period={TimeStepSeconds}";
        }

        public bool ValidateCode(string secretKey, string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length != CodeLength)
                return false;

            var key = Base32Decode(secretKey);
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Check current time step and one before/after for clock drift tolerance
            for (int i = -1; i <= 1; i++)
            {
                var timeStep = (currentTime / TimeStepSeconds) + i;
                var expectedCode = GenerateTOTP(key, timeStep);

                if (expectedCode == code)
                    return true;
            }

            return false;
        }

        public string GenerateBackupCodes(int count = 10)
        {
            var codes = new List<string>();
            using var rng = RandomNumberGenerator.Create();

            for (int i = 0; i < count; i++)
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var code = (BitConverter.ToUInt32(bytes, 0) % 100000000).ToString("D8");
                codes.Add(code);
            }

            return string.Join(",", codes);
        }

        private string GenerateTOTP(byte[] key, long timeStep)
        {
            var timeBytes = BitConverter.GetBytes(timeStep);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(timeBytes);

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(timeBytes);

            // Dynamic truncation
            var offset = hash[^1] & 0x0F;
            var binaryCode = ((hash[offset] & 0x7F) << 24)
                           | ((hash[offset + 1] & 0xFF) << 16)
                           | ((hash[offset + 2] & 0xFF) << 8)
                           | (hash[offset + 3] & 0xFF);

            var otp = binaryCode % (int)Math.Pow(10, CodeLength);
            return otp.ToString().PadLeft(CodeLength, '0');
        }

        private static string Base32Encode(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();
            int buffer = 0, bitsLeft = 0;

            foreach (var b in data)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;

                while (bitsLeft >= 5)
                {
                    bitsLeft -= 5;
                    result.Append(alphabet[(buffer >> bitsLeft) & 0x1F]);
                }
            }

            if (bitsLeft > 0)
            {
                result.Append(alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
            }

            return result.ToString();
        }

        private static byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            input = input.ToUpper().TrimEnd('=');

            var result = new List<byte>();
            int buffer = 0, bitsLeft = 0;

            foreach (var c in input)
            {
                var index = alphabet.IndexOf(c);
                if (index < 0) continue;

                buffer = (buffer << 5) | index;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    result.Add((byte)(buffer >> bitsLeft));
                }
            }

            return result.ToArray();
        }
    }
}
