using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SilkierQuartz.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;

namespace SilkierQuartz.Controllers
{
    public class AuthResponse{
        public string userId;
        public string firstName;
        public string lastName;
        public string token;
    }

    [AllowAnonymous]
    public class AuthenticateController : PageControllerBase
    {
        private string authUri;

        private static readonly HttpClient client = new HttpClient();

        public static int uid;

        private readonly SilkierQuartzAuthenticationOptions authenticationOptions;

        public AuthenticateController(SilkierQuartzAuthenticationOptions authenticationOptions)
        {
            authUri = IJobRegistratorExtensions.AppConfiguration["EASEY_AUTH_API"];
            this.authenticationOptions = authenticationOptions ?? throw new ArgumentNullException(nameof(authenticationOptions));
        }

        [HttpGet]
        public async Task<IActionResult> Login([FromServices] IAuthenticationSchemeProvider schemes)
        {

            uid = new Random().Next();

            if (authenticationOptions.AccessRequirement == SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous)
            {
                return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
            }

            var silkierScheme = await schemes.GetSchemeAsync(authenticationOptions.AuthScheme);

            if (string.IsNullOrEmpty(authenticationOptions.UserName) ||
                string.IsNullOrEmpty(authenticationOptions.UserPassword))
            {
                foreach (var userClaim in HttpContext.User.Claims)
                {
                    Debug.WriteLine($"{userClaim.Type} - {userClaim.Value}");
                }

                if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated ||
                    !HttpContext.User.HasClaim(authenticationOptions.SilkierQuartzClaim,
                        authenticationOptions.SilkierQuartzClaimValue))
                {
                    await SignIn(false);

                    return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
                }
                else
                {
                    return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
                }
            }
            else
            {
                if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated ||
                    !HttpContext.User.HasClaim(authenticationOptions.SilkierQuartzClaim, authenticationOptions.SilkierQuartzClaimValue))
                {
                    ViewBag.IsLoginError = false;
                    return View(new AuthenticateViewModel());
                }
                else
                {
                    return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromForm] AuthenticateViewModel request)
        {
            var form = HttpContext.Request.Form;

            var values = new Dictionary<string, string>
            {
                { "userId",  request.UserName},
                { "password",  request.Password}
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(authUri + "/authentication/sign-in", content);

            if(response.IsSuccessStatusCode){
                AuthResponse parsed = JsonConvert.DeserializeObject<AuthResponse>(await response.Content.ReadAsStringAsync());
                HttpContext.Session.SetString("token", parsed.token);
                
                var currentSession = HttpContext.Session;

                Task t = Task.Run(async () => {
                    for(;;){
                        await Task.Delay(1000);
                        Console.Write("Refreshing User Token For User " + request.UserName + " UID: " + uid);
                        try{
                            
                        }
                        catch(Exception e){
                            Console.Write(e.Message);
                        }
                    }
                });

                await SignIn(request.IsPersist);

                return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
            }
            else
            {
                request.IsLoginError = true;
                return View(request);
            }
        }

        [HttpGet]
        [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
        public async Task<IActionResult> Logout()
        {    
            string token = HttpContext.Session.GetString("token");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.DeleteAsync(authUri + "/authentication/sign-out");
            
            await HttpContext.SignOutAsync(authenticationOptions.AuthScheme);
            return RedirectToAction(nameof(Login));
        }

        private async Task SignIn(bool isPersistentSignIn)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, string.IsNullOrEmpty(authenticationOptions.UserName)
                    ? "SilkierQuartzAdmin"
                    : authenticationOptions.UserName ),

                new Claim(ClaimTypes.Name, string.IsNullOrEmpty(authenticationOptions.UserPassword)
                    ? "SilkierQuartzPassword"
                    : authenticationOptions.UserPassword),

                new Claim(authenticationOptions.SilkierQuartzClaim, authenticationOptions.SilkierQuartzClaimValue)
            };

            var authProperties = new AuthenticationProperties()
            {
                IsPersistent = isPersistentSignIn
            };

            var userIdentity = new ClaimsIdentity(claims, authenticationOptions.AuthScheme);
            await HttpContext.SignInAsync(authenticationOptions.AuthScheme, new ClaimsPrincipal(userIdentity),
                authProperties);
        }
    }
}
