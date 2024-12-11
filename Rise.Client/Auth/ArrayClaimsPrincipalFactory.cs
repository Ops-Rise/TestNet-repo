using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System.Security.Claims;
using System.Text.Json;

namespace Rise.Client.Auth;

public class ArrayClaimsPrincipalFactory<TAccount> : AccountClaimsPrincipalFactory<TAccount> where TAccount : RemoteUserAccount
{
    public ArrayClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor)
        : base(accessor)
    { }

    // when a user belongs to multiple roles, Auth0 returns a single claim with a serialised array of values
    // this class improves the original factory by deserializing the claims in the correct way
    public async override ValueTask<ClaimsPrincipal> CreateUserAsync(TAccount account, RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);

        var claimsIdentity = (ClaimsIdentity)user.Identity!;

        if (account != null)
        {
            foreach (var kvp in account.AdditionalProperties)
            {
                // this isn't really used yet
                var name = kvp.Key;
                var value = kvp.Value;
                if (value != null &&
                    value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    claimsIdentity.RemoveClaim(claimsIdentity.FindFirst(kvp.Key));

                    var claims = element.EnumerateArray()
                        .Select(x => new Claim(kvp.Key, x.ToString()));

                    claimsIdentity.AddClaims(claims);
                }
            }
        }
        return user;
    }
}