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
        public IActionResult Patient()
        {
            ViewData["Message"] = "Your application description page.";

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
                return await CallApiWithToken(tokenResponse.AccessToken);
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
                return await CallApiWithToken(accessToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        private async Task<IActionResult> CallApiWithToken(string accessToken)
        {
            var uri = "http://localhost:5003/patients/123";
            var client = new HttpClient();
            client.SetBearerToken(accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                ViewBag.PatientDataResponse = JsonConvert.DeserializeObject<PatientDataResponse>(await response.Content.ReadAsStringAsync());
                return View("Json");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                ViewBag.ErrorMessage = $"Received 403 Forbidden when calling: {uri}";
                return View("Json");
            }
            throw new Exception($"Error received: {response.StatusCode} when trying to contact remote server: {uri}");
        }
    }

    public class PatientDataResponse
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
    }

    public class UserClaim
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
