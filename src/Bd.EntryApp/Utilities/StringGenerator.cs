using System.Security.Cryptography;
using System.Text;

namespace Bd.EntryApp.Utilities;

public static class StringGenerator
{
    public static string GenerateRandomString(int length)
    {
        var alphabet = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        var sb = new StringBuilder();
        var suffix = Enumerable.Range(0, length)
            .Select(i => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]);
        sb.AppendJoin(string.Empty, suffix);
        return sb.ToString();
    }
}