using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Nancy;
using Nancy.Security;

namespace Fabric.Identity.APISample.Modules
{
    public class PatientsModule : NancyModule
    {
        public PatientsModule() : base("/patients")
        {
            Predicate<Claim> readDemographicsClaim = claim => claim.Type == "allowedresource" && claim.Value == "user/Patient.read";

            this.RequiresClaims(new[] { readDemographicsClaim });
            Get("/{patientId}", parameters => new
            {
                FirstName = "Test",
                LastName = "Patient",
                DateOfBirth = DateTime.Parse("03/27/1965"),
                RequestingUserClaims = Context.CurrentUser.Claims.Select(c => new { c.Type, c.Value})
            });
        }
    }
}
