namespace WebAPI.Common
{
    using System.Security.Cryptography;
    using System.Text;

    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha1.ComputeHash(inputBytes);

                // Convert hash to hex string (like FormsAuthentication used to)
                StringBuilder sb = new StringBuilder();
                foreach (var b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }
    }

}
