using Fracture.Models;

namespace Fracture.Services;

public static class CookieHealthService
{
    public static async Task<bool> ValidateCookieAsync(string encryptedCookie)
    {
        try
        {
            string cookie = CryptoService.Decrypt(encryptedCookie);
            await RobloxService.ValidateCookieAsync(cookie);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task CheckAllAccountsAsync(
        IList<RobloxAccount> accounts,
        Action<RobloxAccount, bool> onResult,
        Action onComplete)
    {
        foreach (var account in accounts.ToList())
        {
            bool valid = await ValidateCookieAsync(account.EncryptedCookie);
            account.CookieValid = valid;
            account.LastHealthCheck = DateTime.UtcNow;
            onResult(account, valid);
        }

        onComplete();
    }
}
