using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fabric.Identity.MvcSample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [Authorize]
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        public async Task Logout()
        {
            await HttpContext.Authentication.SignOutAsync("Cookies");
            await HttpContext.Authentication.SignOutAsync("oidc");
        }

        public async Task<IActionResult> CallApiUsingClientCredentials()
        {
            try
            {
                var tokenClient = new TokenClient("http://localhost:5001/connect/token", "fabric-mvcsample", "secret");
                var tokenResponse = await tokenClient.RequestClientCredentialsAsync("patientapi");

                var client = new HttpClient();
                client.SetBearerToken(tokenResponse.AccessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var content = await client.GetStringAsync("http://localhost:5003/patients/123");

                ViewBag.PatientDataResponse = JsonConvert.DeserializeObject<PatientDataResponse>(content);
                return View("Json");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        public async Task<IActionResult> CallApiUsingUserAccessToken()
        {
            try
            {
                var accessToken = await HttpContext.Authentication.GetTokenAsync("access_token");

                var client = new HttpClient();
                client.SetBearerToken(accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var content = await client.GetStringAsync("http://localhost:5003/patients/123");

                ViewBag.PatientDataResponse = JsonConvert.DeserializeObject<PatientDataResponse>(content);
                return View("Json");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }
    }

    public class PatientDataResponse
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public List<UserClaim> RequestingUserClaims { get; set; }
    }

    public class UserClaim
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
