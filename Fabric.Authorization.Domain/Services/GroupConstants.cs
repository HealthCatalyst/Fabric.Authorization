namespace Fabric.Authorization.Domain.Services
{
    public static class GroupConstants
    {
        public static readonly string CustomSource = "Custom";
        public static readonly string DirectorySource = "Directory";

        public static readonly string[] ValidGroupSources = { CustomSource, DirectorySource };
    }
}