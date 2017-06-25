using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using Scheduler.Models;
using Scheduler.Providers;
using Scheduler.Results;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Data.SqlClient;
using System.Web.Http.Results;

namespace Scheduler.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat)
        {
            UserManager = userManager;
            AccessTokenFormat = accessTokenFormat;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        public UserInfoViewModel GetUserInfo()
        {
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            return new UserInfoViewModel
            {
                Email = User.Identity.GetUserName(),
                HasRegistered = externalLogin == null,
                LoginProvider = externalLogin != null ? externalLogin.LoginProvider : null
            };
        }

        // POST api/Account/Logout
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false)
        {
            IdentityUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null)
            {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IdentityUserLogin linkedAccount in user.Logins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/SetPassword
        [Route("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.AddPasswordAsync(model.Id, model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/AddExternalLogin
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("The external login is already associated with an account.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
        {
            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

                ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                   OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else
            {
                IEnumerable<Claim> claims = externalLogin.GetClaims();
                ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                Authentication.SignIn(identity);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<HttpResponseMessage> SetNewUserPassword(Tuple<string, string, string> data)
        {
            string request_id = data.Item1;
            string password = data.Item2;

            using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                string cmdString = "SELECT * FROM ChangePasswordLog WHERE RequestId=@id;";

                SqlCommand cmd = new SqlCommand(cmdString, con);

                cmd.Parameters.AddWithValue("@id", request_id);


                try
                {
                    con.Open();

                    DateTime date;
                    string email = null;

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            email = rdr["Email"].ToString();
                            date = Convert.ToDateTime(rdr["SendingTime"].ToString());
                        }
                    }

                    string id = GetUserId(con, email);

                    SetNullPassword(id);

                    IHttpActionResult res = await SetPassword(new SetPasswordBindingModel { Id = id, NewPassword = data.Item2, ConfirmPassword = data.Item3 });
                    var contentResult = res as OkResult;

                    if (contentResult == null)
                    {
                        throw new Exception("An error has occured");
                    }

                    cmdString = "DELETE FROM ChangePasswordLog WHERE RequestId = @id;";
                    cmd = new SqlCommand(cmdString, con);

                    cmd.Parameters.AddWithValue("@id", request_id);
                    cmd.ExecuteNonQuery();

                    return Request.CreateResponse(HttpStatusCode.OK, "OK");
                }
                catch
                {
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error");
                }
            }
        }

        private void SetNullPassword(string id)
        {
            using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                string cmdString = "UPDATE AspNetUsers SET PasswordHash=NULL WHERE Id=@id;";

                SqlCommand cmd = new SqlCommand(cmdString, con);

                cmd.Parameters.AddWithValue("@id", id);

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                }
            }
        }

        private string GetUserId(SqlConnection con, string email)
        {
            string toReturn = "";

            string cmdString = "SELECT Id FROM AspNetUsers WHERE Email=@email;";

            SqlCommand cmd = new SqlCommand(cmdString, con);

            cmd.Parameters.AddWithValue("@email", email);

            using (SqlDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    return rdr["Id"].ToString();
                }
            }

            return toReturn;
        }

        private bool ConfirmAccount(string confirmationToken)
        {
            bool toReturn = false;

            using (SqlConnection connection = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                string cmdString = "SELECT * FROM AspNetUsers WHERE ConfirmationToken=@token;";

                SqlCommand cmd = new SqlCommand(cmdString, connection);
                cmd.Parameters.AddWithValue("@token", confirmationToken);

                try
                {
                    connection.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();

                    if (rdr.Read())
                    {
                        rdr.Close();
                        toReturn = SetEmailConfirmed(connection, confirmationToken);
                    }
                }
                catch
                {
                    toReturn = false;
                }
            }

            return toReturn;
        }
        private bool SetEmailConfirmed(SqlConnection con, string confirmationToken)
        {
            bool toReturn = false;

            string cmdString = "UPDATE AspNetUsers SET EmailConfirmed=1 WHERE ConfirmationToken = @token";

            SqlCommand cmd = new SqlCommand(cmdString, con);

            cmd.Parameters.AddWithValue("@token", confirmationToken);

            try
            {
                cmd.ExecuteNonQuery();
                toReturn = true;
            }
            catch
            {
                toReturn = false;
            }
            finally
            {
                con.Close();
            }

            return toReturn;
        }

        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage ChangeUserPassword(string email)
        {
            string guid = Guid.NewGuid().ToString();

            string letterSent = WriteChangePasswordLetter(email, guid);

            if (letterSent == "OK")
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    string cmdString = "INSERT INTO ChangePasswordLog VALUES(GETDATE(),@RequestId, @mail);";

                    SqlCommand cmd = new SqlCommand(cmdString, con);

                    cmd.Parameters.AddWithValue("@RequestId", guid);
                    cmd.Parameters.AddWithValue("@mail", email);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();

                        return Request.CreateResponse(HttpStatusCode.OK, "Letter was sent");
                    }
                    catch (Exception ex)
                    {
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
                    }
                }
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, letterSent);
            }
        }



        private string WriteChangePasswordLetter(string email, string guid)
        {
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUserName = "btsemail1@gmail.com";
            string smtpUserPass = "btsadmin";

            SmtpClient client = new SmtpClient(smtpHost, smtpPort);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUserName, smtpUserPass);
            client.EnableSsl = true;

            string msgFrom = smtpUserName;
            string msgTo = email;
            string msgSubject = "Password Notification";

            string msgBody = "Please follow this link: http://localhost:24082/Main/SetNewPassword?request_id=" +
                guid + " to change your password";

            MailMessage message = new MailMessage(msgFrom, msgTo, msgSubject, msgBody);

            message.IsBodyHtml = true;

            try
            {
                client.Send(message);

                return "OK";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }



        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage RegisterConfirmation([FromUri]string token)
        {
            if (ConfirmAccount(token))
            {
                return Request.CreateResponse(HttpStatusCode.OK, "Email confirmed");
            }

            return Request.CreateErrorResponse(HttpStatusCode.NotModified, "Wrong confirmation credentials");
        }


        private string CreateConfirmationToken()
        {
            return Guid.NewGuid().ToString();
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(JsonConvert.SerializeObject(ModelState.Values.SelectMany(modelState => modelState.Errors)));
            }

            var user = new ApplicationUser()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                Bonus = 0,
                ConfirmationToken = CreateConfirmationToken()
            };

            IdentityResult result = await UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }
            else
            {
                SendEmailConfirmation(user.Email, user.FirstName, user.ConfirmationToken);
            }

            return Ok();
        }

        private void SendEmailConfirmation(string email, string firstName, string confirmationToken)
        {
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUserName = "btsemail1@gmail.com";
            string smtpUserPass = "btsadmin";

            SmtpClient client = new SmtpClient(smtpHost, smtpPort);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUserName, smtpUserPass);
            client.EnableSsl = true;

            string msgFrom = smtpUserName;
            string msgTo = email;
            string msgSubject = "Password Notification";

            string msgBody = "Dear " + firstName + ", <br/><br/>";
            msgBody += "Please follow this link: http://localhost:24082/Main/ConfirmMail?token=" + confirmationToken + " to confirm your account";
            MailMessage message = new MailMessage(msgFrom, msgTo, msgSubject, msgBody);

            message.IsBodyHtml = true;

            client.Send(message);
        }

        // POST api/Account/RegisterExternal
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(JsonConvert.SerializeObject(ModelState.Values));
            }

            var info = await Authentication.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return InternalServerError();
            }

            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };



            IdentityResult result = await UserManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }
            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }
}
