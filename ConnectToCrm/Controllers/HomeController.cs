using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ConnectToCrm.Controllers
{
    public class HomeController : Controller
    {
        private static string serviceUrl = "https://crmfortress4.crm6.dynamics.com/";   // CRM Online

        private static string userAccount = "<user-account>";  //CRM user account
        private static string domain = "<server-domain>";  //CRM server domain

        private static string clientId = "c027407b-66e6-4917-bd37-ff01daa58191";     //e.g. "e5cf0024-a66a-4f16-85ce-99ba97a24bb2"
        private static string redirectUrl = "http://connecttocrmwebapi.azurewebsites.net/";  //e.g. "http://localhost/SdkSample"

        public ActionResult Index()
        {
            HttpMessageHandler messageHandler;
            if (serviceUrl.StartsWith("https://"))
            {
                messageHandler = new OAuthMessageHandler(serviceUrl, clientId, redirectUrl,
                         new HttpClientHandler());
            }
            else
            {
                //Prompt for user account password required for on-premise credentials.  (Better
                // approach is to use the SecureString class here.)
                Console.Write("Please enter the password for account {0}: ", userAccount);
                string password = Console.ReadLine().Trim();
                NetworkCredential credentials = new NetworkCredential(userAccount, password, domain);
                messageHandler = new HttpClientHandler() { Credentials = credentials };
            }

            using (HttpClient httpClient = new HttpClient(messageHandler))
            {
                //Specify the Web API address of the service and the period of time each request 
                // has to execute.
                httpClient.BaseAddress = new Uri(serviceUrl);
                httpClient.Timeout = new TimeSpan(0, 2, 0);  //2 minutes

                //Send the WhoAmI request to the Web API using a GET request. 
                var response = httpClient.GetAsync("api/data/v8.1/contacts?$select=fullname",
                        HttpCompletionOption.ResponseHeadersRead).Result;
                if (response.IsSuccessStatusCode)
                {
                    //Get the response content and parse it.
                    JObject body = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    //Guid userId = (Guid)body["UserId"];
                    //Console.WriteLine("Your system user ID is: {0}", userId);
                }
                else
                {
                    //Console.WriteLine("The request failed with a status of '{0}'", response.ReasonPhrase);
                }
            }

            ViewBag.Title = "Home Page";

            return View();
        }
    }

    class OAuthMessageHandler : DelegatingHandler
    {
        private AuthenticationHeaderValue authHeader;

        public OAuthMessageHandler(string serviceUrl, string clientId1, string redirectUrl,
                HttpMessageHandler innerHandler)
            : base(innerHandler)
        { 
            // Obtain the Azure Active Directory Authentication Library (ADAL) authentication context.
            //AuthenticationParameters ap = AuthenticationParameters.CreateFromResourceUrlAsync(
            //        new Uri(serviceUrl + "api/data/")).Result;
            //var credential = new UserCredential("username", "password");
            //Note that an Azure AD access token has finite lifetime, default expiration is 60 minutes.
            //AuthenticationResult authResult = authContext.AcquireToken(serviceUrl, clientId, new Uri(redirectUrl), PromptBehavior.Always);

            //UserCredential credentials = new UserCredential("indika@crmfortress4.onmicrosoft.com", "#crmfortress4");
            //AuthenticationContext authContext = new AuthenticationContext("https://login.microsoftonline.com/crmfortress4.onmicrosoft.com", false);
            //AuthenticationResult authResult = authContext.AcquireToken(serviceUrl, clientId, credentials);
            ////AuthenticationResult authResult = authContext.AcquireToken(serviceUrl, clientId, new Uri(redirectUrl), PromptBehavior.Always);
            //authHeader = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);



            // Get OAuth token using client credentials 
            string tenantName = "crmfortress4.onmicrosoft.com";
            string authString = "https://login.microsoftonline.com/" + tenantName;

            AuthenticationContext authenticationContext = new AuthenticationContext(authString, false);

            // Config for OAuth client credentials  
            string clientId = "c027407b-66e6-4917-bd37-ff01daa58191";
            string key = "wEEocjEuRV0/rRmoMxmvKQ3zuDn0vltBtxrpwwd/hkI=";
            ClientCredential clientCred = new ClientCredential(clientId, key);
            string resource = "http://connecttocrmwebapi.azurewebsites.net/";
            string token;
            try
            {
                AuthenticationResult authenticationResult = authenticationContext.AcquireToken(resource, clientCred);
                token = authenticationResult.AccessToken;
                authHeader = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
            }
            catch (Exception ex)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("Acquiring a token failed with the following error: {0}", ex.Message);
                //if (ex.InnerException != null)
                //{
                //    //  You should implement retry and back-off logic according to
                //    //  http://msdn.microsoft.com/en-us/library/dn168916.aspx . This topic also
                //    //  explains the HTTP error status code in the InnerException message. 
                //    Console.WriteLine("Error detail: {0}", ex.InnerException.Message);
                //}
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(
                 HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            request.Headers.Authorization = authHeader;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
