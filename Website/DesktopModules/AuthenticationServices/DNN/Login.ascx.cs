#region Copyright
//
// DotNetNuke® - https://www.dnnsoftware.com
// Copyright (c) 2002-2018
// by DotNetNuke Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using DotNetNuke.Abstractions;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Instrumentation;
using DotNetNuke.Security;
using DotNetNuke.Security.Membership;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Skins.Controls;

using Globals = DotNetNuke.Common.Globals;

#endregion

namespace DotNetNuke.Modules.Admin.Authentication.DNN
{
    using Host = DotNetNuke.Entities.Host.Host;

	/// <summary>
	/// The Login AuthenticationLoginBase is used to provide a login for a registered user
	/// portal.
	/// </summary>
	/// <remarks>
	/// </remarks>
	public partial class Login : AuthenticationLoginBase
	{
		private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof (Login));
        private readonly INavigationManager _navigationManager;

        public Login()
        {
            _navigationManager = DependencyProvider.GetRequiredService<INavigationManager>();
        }

		#region Protected Properties

		/// <summary>
		/// Gets whether the Captcha control is used to validate the login
		/// </summary>
		protected bool UseCaptcha
		{
			get
			{
				return AuthenticationConfig.GetConfig(PortalId).UseCaptcha;
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Check if the Auth System is Enabled (for the Portal)
		/// </summary>
		/// <remarks></remarks>
		public override bool Enabled
		{
			get
			{
				return AuthenticationConfig.GetConfig(PortalId).Enabled;
			}
		}

		#endregion

		#region Event Handlers

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			cmdLogin.Click += OnLoginClick;

			cancelLink.NavigateUrl = GetRedirectUrl(false);

            if (PortalSettings.UserRegistration == (int)Globals.PortalRegistrationType.NoRegistration)
            {
                liRegister.Visible = false;
            }
            lblLogin.Text = Localization.GetSystemMessage(PortalSettings, "MESSAGE_LOGIN_INSTRUCTIONS");
		    if (string.IsNullOrEmpty(lblLogin.Text))
		    {
		        lblLogin.AssociatedControlID = string.Empty;
		    }

            if (Request.QueryString["usernameChanged"] == "true")
            {
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetSystemMessage(PortalSettings, "MESSAGE_USERNAME_CHANGED_INSTRUCTIONS"), ModuleMessage.ModuleMessageType.BlueInfo);
            }

            var returnUrl = _navigationManager.NavigateURL();
            string url;
            if (PortalSettings.UserRegistration != (int)Globals.PortalRegistrationType.NoRegistration)
            {
                if (!string.IsNullOrEmpty(UrlUtils.ValidReturnUrl(Request.QueryString["returnurl"])))
                {
                    returnUrl = Request.QueryString["returnurl"];
                }
                returnUrl = HttpUtility.UrlEncode(returnUrl);

                url = Globals.RegisterURL(returnUrl, Null.NullString);
                registerLink.NavigateUrl = url;
                if (PortalSettings.EnablePopUps && PortalSettings.RegisterTabId == Null.NullInteger
                    && !AuthenticationController.HasSocialAuthenticationEnabled(this))
                {
                    registerLink.Attributes.Add("onclick", "return " + UrlUtils.PopUpUrl(url, this, PortalSettings, true, false, 600, 950));
                }
            }
            else
            {
                registerLink.Visible = false;
            }

            //see if the portal supports persistant cookies
            chkCookie.Visible = Host.RememberCheckbox;



            // no need to show password link if feature is disabled, let's check this first
            if (MembershipProviderConfig.PasswordRetrievalEnabled || MembershipProviderConfig.PasswordResetEnabled)
            {
                url = _navigationManager.NavigateURL("SendPassword", "returnurl=" + returnUrl);
                passwordLink.NavigateUrl = url;
                if (PortalSettings.EnablePopUps)
                {
                    passwordLink.Attributes.Add("onclick", "return " + UrlUtils.PopUpUrl(url, this, PortalSettings, true, false, 300, 650));
                }
            }
            else
            {
                passwordLink.Visible = false;
            }


            if (!IsPostBack)
            {
                if (!string.IsNullOrEmpty(Request.QueryString["verificationcode"]) && PortalSettings.UserRegistration == (int) Globals.PortalRegistrationType.VerifiedRegistration)
                {
                    if (Request.IsAuthenticated)
                    {
                        Controls.Clear();
                    }

                    var verificationCode = Request.QueryString["verificationcode"];


                    try
                    {
                        UserController.VerifyUser(verificationCode.Replace(".", "+").Replace("-", "/").Replace("_", "="));

						var redirectTabId = PortalSettings.Registration.RedirectAfterRegistration;

	                    if (Request.IsAuthenticated)
	                    {
                            Response.Redirect(_navigationManager.NavigateURL(redirectTabId > 0 ? redirectTabId : PortalSettings.HomeTabId, string.Empty, "VerificationSuccess=true"), true);
	                    }
	                    else
	                    {
                            if (redirectTabId > 0)
                            {
                                var redirectUrl = _navigationManager.NavigateURL(redirectTabId, string.Empty, "VerificationSuccess=true");
                                redirectUrl = redirectUrl.Replace(Globals.AddHTTP(PortalSettings.PortalAlias.HTTPAlias), string.Empty);
                                Response.Cookies.Add(new HttpCookie("returnurl", redirectUrl) { Path = (!string.IsNullOrEmpty(Globals.ApplicationPath) ? Globals.ApplicationPath : "/") });
                            }

		                    UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("VerificationSuccess", LocalResourceFile), ModuleMessage.ModuleMessageType.GreenSuccess);
	                    }
                    }
                    catch (UserAlreadyVerifiedException)
                    {
                        UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("UserAlreadyVerified", LocalResourceFile), ModuleMessage.ModuleMessageType.YellowWarning);
                    }
                    catch (InvalidVerificationCodeException)
                    {
                        UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("InvalidVerificationCode", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                    }
                    catch (UserDoesNotExistException)
                    {
                        UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("UserDoesNotExist", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                    }
                    catch (Exception)
                    {
                        UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("InvalidVerificationCode", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                    }
                }
            }

			if (!Request.IsAuthenticated)
			{
				if (!Page.IsPostBack)
				{
					try
					{
						if (Request.QueryString["username"] != null)
						{
							txtUsername.Text = Request.QueryString["username"];
						}
					}
					catch (Exception ex)
					{
						//control not there
						Logger.Error(ex);
					}
				}
				try
				{
					Globals.SetFormFocus(string.IsNullOrEmpty(txtUsername.Text) ? txtUsername : txtPassword);
				}
				catch (Exception ex)
				{
					//Not sure why this Try/Catch may be necessary, logic was there in old setFormFocus location stating the following
					//control not there or error setting focus
					Logger.Error(ex);
				}
			}

			var registrationType = PortalSettings.Registration.RegistrationFormType;
		    bool useEmailAsUserName;
            if (registrationType == 0)
            {
				useEmailAsUserName = PortalSettings.Registration.UseEmailAsUserName;
            }
            else
            {
				var registrationFields = PortalSettings.Registration.RegistrationFields;
                useEmailAsUserName = !registrationFields.Contains("Username");
            }

		    plUsername.Text = LocalizeString(useEmailAsUserName ? "Email" : "Username");
		    divCaptcha1.Visible = UseCaptcha;
			divCaptcha2.Visible = UseCaptcha;
		}

		private void OnLoginClick(object sender, EventArgs e)
		{
			if ((UseCaptcha && ctlCaptcha.IsValid) || !UseCaptcha)
			{
				var loginStatus = UserLoginStatus.LOGIN_FAILURE;
				string userName = PortalSecurity.Instance.InputFilter(txtUsername.Text,
										PortalSecurity.FilterFlag.NoScripting |
                                        PortalSecurity.FilterFlag.NoAngleBrackets |
                                        PortalSecurity.FilterFlag.NoMarkup);

                //DNN-6093
                //check if we use email address here rather than username
                UserInfo userByEmail = null;
                var emailUsedAsUsername = PortalController.GetPortalSettingAsBoolean("Registration_UseEmailAsUserName", PortalId, false);

                if (emailUsedAsUsername)
                {
                    // one additonal call to db to see if an account with that email actually exists
                    userByEmail = UserController.GetUserByEmail(PortalId, userName);

                    if (userByEmail != null)
                    {
                        //we need the username of the account in order to authenticate in the next step
                        userName = userByEmail.Username;
                    }
                }

                UserInfo objUser = null;

                if (!emailUsedAsUsername || userByEmail != null)
                {
                    objUser = UserController.ValidateUser(PortalId, userName, txtPassword.Text, "DNN", string.Empty, PortalSettings.PortalName, IPAddress, ref loginStatus);
                }

                var authenticated = Null.NullBoolean;
				var message = Null.NullString;
				if (loginStatus == UserLoginStatus.LOGIN_USERNOTAPPROVED)
				{
				    message = "UserNotAuthorized";
				}
				else
				{
					authenticated = (loginStatus != UserLoginStatus.LOGIN_FAILURE);
				}

                if (objUser != null && loginStatus != UserLoginStatus.LOGIN_FAILURE && emailUsedAsUsername)
                {
                    //make sure internal username matches current e-mail address
                    if (objUser.Username.ToLower() != objUser.Email.ToLower())
                    {
                        UserController.ChangeUsername(objUser.UserID, objUser.Email);
                        userName = objUser.Username = objUser.Email;
                    }
                }

				//Raise UserAuthenticated Event
				var eventArgs = new UserAuthenticatedEventArgs(objUser, userName, loginStatus, "DNN")
				                    {
				                        Authenticated = authenticated,
                                        Message = message,
                                        RememberMe = chkCookie.Checked
				                    };
				OnUserAuthenticated(eventArgs);
			}
		}

		#endregion

		#region Private Methods

		protected string GetRedirectUrl(bool checkSettings = true)
		{
			var redirectUrl = "";
			var redirectAfterLogin = PortalSettings.Registration.RedirectAfterLogin;
			if (checkSettings && redirectAfterLogin > 0) //redirect to after registration page
			{
				redirectUrl = _navigationManager.NavigateURL(redirectAfterLogin);
			}
			else
			{
				if (Request.QueryString["returnurl"] != null)
				{
					//return to the url passed to register
					redirectUrl = HttpUtility.UrlDecode(Request.QueryString["returnurl"]);

                    //clean the return url to avoid possible XSS attack.
                    redirectUrl = UrlUtils.ValidReturnUrl(redirectUrl);

                    if (redirectUrl.Contains("?returnurl"))
					{
						string baseURL = redirectUrl.Substring(0,
							redirectUrl.IndexOf("?returnurl", StringComparison.Ordinal));
						string returnURL =
							redirectUrl.Substring(redirectUrl.IndexOf("?returnurl", StringComparison.Ordinal) + 11);

						redirectUrl = string.Concat(baseURL, "?returnurl", HttpUtility.UrlEncode(returnURL));
					}
				}
				if (String.IsNullOrEmpty(redirectUrl))
				{
					//redirect to current page
					redirectUrl = _navigationManager.NavigateURL();
				}
			}

			return redirectUrl;
		}

		#endregion

	}
}
