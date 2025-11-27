using AccountGoWeb.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AccountGoWeb.Controllers
{
    public class AccountController : GoodController
    {
        public AccountController(IConfiguration config)
        {
            _configuration = config;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignIn(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel() { Email = "admin@accountgo.ph", Password = "P@ssword1" });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage responseSignIn = Post("account/signin", content);

                var responseContent = responseSignIn.Content.ReadAsStringAsync().Result;

                // Log the response for debugging
                Console.WriteLine($"API Response Status: {responseSignIn.StatusCode}");
                Console.WriteLine($"API Response Content: {responseContent}");

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    ModelState.AddModelError(string.Empty, "API returned empty response. Check API logs.");
                    return View(model);
                }

                Newtonsoft.Json.Linq.JObject resultSignIn = Newtonsoft.Json.Linq.JObject.Parse(responseContent);

                if (resultSignIn["result"] != null)
                {
                    // Extract the JWT token from the signin response
                    string? accessToken = resultSignIn["result"]!["accessToken"]?.ToString();
                    
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        ModelState.AddModelError(string.Empty, "Failed to obtain access token.");
                        return View(model);
                    }

                    var user = await GetAsync<Dto.Security.User>("administration/getuser?username=" + model.Email, accessToken);

                    if (user == null || string.IsNullOrEmpty(user.Email))
                    {
                        ModelState.AddModelError(string.Empty, "User not found or email is missing.");
                        return View(model);
                    }

                    var claims = new List<Claim>();
                    claims.Add(new Claim(ClaimTypes.IsPersistent, model.RememberMe.ToString()));
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Email));
                    claims.Add(new Claim(ClaimTypes.Email, user.Email));
                    claims.Add(new Claim("AccessToken", accessToken)); // Store token for future API calls

                    string firstName = user.FirstName != null ? user.FirstName : "";
                    string lastName = user.LastName != null ? user.LastName : "";

                    claims.Add(new Claim(ClaimTypes.GivenName, firstName));
                    claims.Add(new Claim(ClaimTypes.Surname, lastName));
                    claims.Add(new Claim(ClaimTypes.Name, firstName + " " + lastName));

                    foreach (var role in user.Roles)
                        claims.Add(new Claim(ClaimTypes.Role, role.Name!));

                    claims.Add(new Claim(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(user)));

                    var identity = new ClaimsIdentity(claims, "AuthCookie");

                    ClaimsPrincipal principal = new ClaimsPrincipal(new[] { identity });

                    HttpContext.User = principal;

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToLocal(returnUrl!);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public new async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync();

            return SignedOut();
        }

        public IActionResult SignedOut()
        {
            if (HttpContext.User.Identity!.IsAuthenticated)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return View();
        }
        
        public IActionResult AccessDenied()
        {
            return View();
        }
        
        public IActionResult Unauthorize()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            try
            {
                if (ModelState.IsValid)
                {
                    var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                    var content = new StringContent(serialize);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    HttpResponseMessage responseAddNewUser = Post("account/addnewuser", content);
                    Newtonsoft.Json.Linq.JObject resultAddNewUser = Newtonsoft.Json.Linq.JObject.Parse(responseAddNewUser.Content.ReadAsStringAsync().Result);

                    HttpResponseMessage? responseInitialized = null;
                    Newtonsoft.Json.Linq.JObject? resultInitialized = null;
                    if ((bool)resultAddNewUser["succeeded"]!)
                    {
                        responseInitialized = Get("administration/initializedcompany");
                        resultInitialized = Newtonsoft.Json.Linq.JObject.Parse((responseInitialized.Content.ReadAsStringAsync().Result));
                        return RedirectToAction(nameof(AccountController.SignIn), "Account");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, resultAddNewUser["errors"]![0]!["description"]!.ToString());
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Please check if your database is ready/published." + ": " + ex.Message);
                return View(model);
            }
            return View(model);
        }

        #region Private Methods
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
        #endregion
    }
}
