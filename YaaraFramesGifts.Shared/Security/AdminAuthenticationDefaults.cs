namespace YaaraFramesGifts.Security;

public static class AdminAuthenticationDefaults
{
    public const string Scheme = "AdminBearer";
    public const string Policy = "AdminOnly";
    public const int AccessTokenLifetimeSeconds = 3600;
}
