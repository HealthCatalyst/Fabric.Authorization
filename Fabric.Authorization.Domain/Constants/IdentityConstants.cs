namespace Fabric.Authorization.Domain
{
    public static class IdentityConstants
    {
        public static readonly string ActiveDirectory = "Windows";
        public static readonly string AzureActiveDirectory = "AzureActiveDirectory";

        public static readonly string[] ValidIdentityProviders = { ActiveDirectory, AzureActiveDirectory };
    }

    public static class Identity
    {
        public static readonly string ClientName = "fabric-authorization-client";
    }
}
