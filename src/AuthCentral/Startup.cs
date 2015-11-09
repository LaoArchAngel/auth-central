﻿using Fsw.Enterprise.AuthCentral.Health;
using Fsw.Enterprise.AuthCentral.IdSvr;
using IdentityServer3.Core.Configuration;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using AuthenticationOptions = IdentityServer3.Core.Configuration.AuthenticationOptions;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Http;
using IdentityServer3.MongoDb;
using MongoDB.Driver;
using IdentityServer3.Core.Services;
using IdentityServer3.MembershipReboot;
using BrockAllen.MembershipReboot.Hierarchical;

namespace Fsw.Enterprise.AuthCentral
{
    public class Startup
    {
        private EnvConfig _config;
        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(appEnv.ApplicationBasePath)
                .AddJsonFile("appsettings.json", optional: true);
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
            _config = new EnvConfig(Configuration);
        }

        public IConfigurationRoot Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var loggerConfig = new LoggerConfiguration()
               .WriteTo.Trace()
               .WriteTo.Console();

            if (_config.IsDebug)
            {
                loggerConfig.MinimumLevel.Information();
            } else
            {
                loggerConfig.MinimumLevel.Error();
            }

            Log.Logger = loggerConfig.CreateLogger();
            
            services.AddDataProtection();
            services.AddMvc();
            services.AddAuthentication(sharedOptions => sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            services.AddAuthorization(options =>
            {
                options.AddPolicy("FswPlatform", policy => {

                    policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
                    policy.RequireClaim("scope", "fsw_platform");
                });
                
                options.DefaultPolicy = options.GetPolicy("FswPlatform");
            });
        }

        public void Configure(IApplicationBuilder app, IApplicationEnvironment env, ILoggerFactory logFactory)
        {
            // TODO: This whole method should be refactored
            var settings = StoreSettings.DefaultSettings();

            settings.ConnectionString = _config.DB.IdentityServer3;
            settings.Database = MongoUrl.Create(settings.ConnectionString).DatabaseName;

            var usrSrv = new Registration<IUserService, MembershipRebootUserService<HierarchicalUserAccount>>();
            IdentityServerServiceFactory factory = new ServiceFactory(usrSrv, settings)
            {
                ViewService = new Registration<IViewService>(typeof(CustomViewService))
            };

            factory.ConfigureCustomUserService(app, _config.DB.MembershipReboot);
            factory.Register(new Registration<IApplicationEnvironment>(env));
            
            app.UseDeveloperExceptionPage();

            app.UseCookieAuthentication(options =>
            {
                options.LoginPath = new PathString("/ids/login");
                options.AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });

            app.UseOpenIdConnectAuthentication(options =>
            {
                options.AuthenticationScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ClientId = "auth_central_client";
                options.ClientSecret = "secret";
                options.Authority = new UriBuilder(_config.Uri.Scheme, _config.Uri.Host, _config.Uri.Port, "ids").Uri.AbsoluteUri;
                options.RedirectUri = new UriBuilder(_config.Uri.Scheme, _config.Uri.Host, _config.Uri.Port, "account").Uri.AbsoluteUri;
                options.ResponseType = OpenIdConnectResponseTypes.Code;
                options.DefaultToCurrentUriOnRedirect = true;
                options.Scope.Add("fsw_platform");
                options.Scope.Add("openid");

                options.Events = new OpenIdConnectEvents
                {
                    OnAuthenticationValidated = data =>
                    {
                        var id = new ClaimsIdentity("application", "given_name", "role");

                        var token = new JwtSecurityToken(data.TokenEndpointResponse.ProtocolMessage.AccessToken);
                        IEnumerable<Claim> claims = token.Claims.Where(c => c.Type != "iss" &&
                                                                            c.Type != "aud" &&
                                                                            c.Type != "nbf" &&
                                                                            c.Type != "exp" &&
                                                                            c.Type != "iat" &&
                                                                            c.Type != "nonce" &&
                                                                            c.Type != "c_hash" &&
                                                                            c.Type != "at_hash");

                        string expiration = token.Claims.First(c => c.Type == "exp").Value;

                        id.AddClaims(claims);
                        id.AddClaim(new Claim("expires_at",
                            new DateTime(1970, 1, 1).AddSeconds(Convert.ToDouble(expiration))
                                .ToString(CultureInfo.CurrentCulture)));

                        data.AuthenticationTicket = new AuthenticationTicket(
                            new ClaimsPrincipal(id),
                            data.AuthenticationTicket.Properties,
                            data.AuthenticationTicket.AuthenticationScheme);

                        return Task.FromResult(0);
                    }
                };
            });

            if(_config.IsDebug)
            {
                logFactory.MinimumLevel = LogLevel.Verbose;
            } else
            {
                logFactory.MinimumLevel = LogLevel.Error;
            }
            app.UseIISPlatformHandler();
            app.UseStaticFiles();

            logFactory.AddSerilog();
            HealthChecker.ScheduleHealthCheck(_config, logFactory);

            app.Map("/ids", ids =>
            {
                var idsOptions = new IdentityServerOptions
                {
                    SiteName = "FSW Identity Server",
                    SigningCertificate = Certificate.Get(_config.Cert.StoreName, _config.Cert.Thumbprint),
                    IssuerUri = _config.Uri.IssuerUri,
                    RequireSsl = true,
                    LoggingOptions = new LoggingOptions()
                    {
                        EnableHttpLogging = true,
                        EnableKatanaLogging = _config.IsDebug,
                        EnableWebApiDiagnostics = _config.IsDebug,
                        WebApiDiagnosticsIsVerbose = _config.IsDebug
                    },
                    Endpoints = new EndpointOptions()
                    {
                        EnableCspReportEndpoint = true
                    },
                    Factory = idSvrFactory,
                    AuthenticationOptions = new AuthenticationOptions()
                    {
                        EnableLocalLogin = true,
                        EnableLoginHint = true,
                        RememberLastUsername = false,
                        CookieOptions = new IdentityServer3.Core.Configuration.CookieOptions()
                        {
                            ExpireTimeSpan = new TimeSpan(10, 0, 0),
                            IsPersistent = false,
                            SlidingExpiration = false,
                            AllowRememberMe = true,
                            RememberMeDuration = new TimeSpan(30, 0, 0, 0)
                        },
                        EnableSignOutPrompt = true,
                        EnablePostSignOutAutoRedirect = true,
                        SignInMessageThreshold = 5                        
                    },                    
                    CspOptions = new CspOptions()
                    {
                        Enabled = true
                    },
                    EnableWelcomePage = true                    
                };

                ids.UseIdentityServer(idsOptions);
            });

            app.UseMvc();
            
        }
    }
}
