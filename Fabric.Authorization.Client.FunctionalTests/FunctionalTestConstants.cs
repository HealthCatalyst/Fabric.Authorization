namespace Fabric.Authorization.Client.FunctionalTests
{
    public class FunctionalTestConstants
    {
        // User Information
        public const string BobUserName = "bob";
        public const string BobPassword = "bob";
        public const string AliceUserName = "alice";
        public const string AlicePassword = "alice";       
        public const string IdentityTestUser = "func-test";

        // Configuration Values
        public const string Grain = "app";
        public const string GroupName = "FABRIC\\Health Catalyst Viewer";

        // Application Settings
        public const string FabricIdentityUrl = "FABRIC_IDENTITY_URL";
        public const string FabricAuthUrl = "FABRIC_AUTH_URL";
        public const string FabricInstallerSecret = "FABRIC_INSTALLER_SECRET";
        public const string FabricAuthSecret = "FABRIC_AUTH_SECRET";

        // Authorization schemes
        public const string Bearer = "bearer";
        public const string Basic = "Basic";

        // media types
        public const string Applicationjson = "application/json";

        //testing
        public const string FunctionTestTitle = "Functional Test Dependencies";
    }
}
