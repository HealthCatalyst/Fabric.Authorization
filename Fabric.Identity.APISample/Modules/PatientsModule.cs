using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using Nancy.Security;

namespace Fabric.Identity.APISample.Modules
{
    public class PatientsModule : NancyModule
    {
        public PatientsModule() : base("/patients")
        {
            this.RequiresAuthentication();
            Get("/{patientId}", parameters => new
            {
                FirstName = "Test",
                LastName = "Patient",
                DateOfBirth = DateTime.Parse("03/27/1965"),
                RequestingUserClaims = this.Context.CurrentUser.Claims.Select(c => new { c.Type, c.Value})
            });
        }
    }
}
