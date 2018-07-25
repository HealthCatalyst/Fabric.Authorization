namespace Catalyst.Fabric.Authorization.Models
{
    public class Error
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Target { get; set; }
        public Error[] Details { get; set; }
        public InnerError InnerError { get; set; }
    }

    public class InnerError
    {
        public string Code { get; set; }
        public InnerError innerError { get; set; }
    }
}
