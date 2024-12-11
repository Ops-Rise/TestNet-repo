using Microsoft.AspNetCore.Authorization;

public class OwnDataOrAdminRequirement : IAuthorizationRequirement
{
    public OwnDataOrAdminRequirement() { }
}
