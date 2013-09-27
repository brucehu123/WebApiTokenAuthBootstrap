﻿using System;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Newtonsoft.Json;
using TokenAuthorization.Core.Account;
using TokenAuthorization.Core.Configuration;

namespace TokenAuthorization.Core.Controllers
{
    public class TokenAuthApiController : ApiController
    {
        private dynamic _userData;
        private bool _isNewUserData = false;
        private bool _isDataFetched = false;
        public bool PossibleDataModified = false;

        public new UserMetadata User { get; set; }

        public TokenAuthApiController()
        {
            User = new UserMetadata(DateTime.MinValue, 0, string.Empty, false, UserRole.Unknown);
        }

        public dynamic UserData
        {
            get
            {
                PossibleDataModified = true;

                if (!_isNewUserData)
                {
                    if (!_isDataFetched)
                    {
                        var data = TokenAuthenticationConfiguration.UserFetcher.FetchData(Request);

                        if (data == null)
                        {
                            _userData = new ExpandoObject();
                        }
                        else
                        {
                            _userData = JsonConvert.DeserializeObject(data);
                        }

                        // In case of json convertion failed.
                        if (_userData.GetType() != typeof(ExpandoObject))
                        {
                            _userData = new ExpandoObject();
                        }

                        _isDataFetched = true;
                    }
                }

                return _userData;
            }
            set
            {
                PossibleDataModified = true;
                _isNewUserData = true;
                _userData = value;
            }
        }

        private const string LocalHost = "localhost";

        protected virtual HttpResponseMessage Login(int userId, string username, UserRole role)
        {
            var response = Request.CreateResponse(HttpStatusCode.OK);
            var tokenCookie = CreateLoginTokenCookie(userId, username, role, DateTimeOffset.MaxValue);
            response.Headers.AddCookies(new[] { tokenCookie });

            return response;
        }

        protected virtual HttpResponseMessage Logout()
        {
            return Logout(true);
        }

        protected virtual HttpResponseMessage Logout(bool clearUserData)
        {
            var response = Request.CreateResponse(HttpStatusCode.OK);
            var tokenCookie = CreateLogoutTokenCookie();
            response.Headers.AddCookies(new[] { tokenCookie });
            
            if (clearUserData)
            {
                UserData = null;
            }

            return response;
        }

        private CookieHeaderValue CreateLoginTokenCookie(int userId, string username, UserRole role, DateTimeOffset expireTime)
        {
            var tokenProvider = TokenAuthenticationConfiguration.TokenProvider;
            var token = tokenProvider.CreateToken(userId, username, role);

            var tokenCookie = new CookieHeaderValue(TokenAuthenticationConfiguration.TokenName, token)
                {
                    Expires = expireTime,
                    Domain = Request.RequestUri.Host == LocalHost ? null : Request.RequestUri.Host,
                    Path = "/"
                };
            return tokenCookie;
        }

        private CookieHeaderValue CreateLogoutTokenCookie()
        {
            var tokenCookie = new CookieHeaderValue(TokenAuthenticationConfiguration.TokenName, string.Empty)
            {
                Expires = DateTime.Now.AddDays(-1d),
                Domain = Request.RequestUri.Host == LocalHost ? null : Request.RequestUri.Host,
                Path = "/"
            };
            return tokenCookie;
        }

        protected virtual HttpResponseMessage Error(string message)
        {
            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, message);
        }

        protected virtual HttpResponseMessage Unauthorized()
        {
            return Request.CreateResponse(HttpStatusCode.Unauthorized, TokenAuthenticationConfiguration.UnauthorizedMessageResponse);
        }
    }
}