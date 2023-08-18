﻿using Auth.Data;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace Auth;

public static class Config
{
    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            new("weather-api", "Weather API"),
            new("weather-summary-api", "Weather Summary API")
        };

    public static IEnumerable<IdentityResource> IdentityResources => new List<IdentityResource>
    {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    };

    public static IEnumerable<Client> GetClients(IConfiguration configuration)
    {
        var authAdminUrl = configuration.GetServiceUri("auth-admin")!.ToString();
        var mvcClientUrl = configuration.GetServiceUri("mvc-client")!.ToString();
        var bffClientUrl = configuration.GetServiceUri("javascriptbff-client")!.ToString();
        var reactClientUrl = configuration.GetServiceUri("react-client")!.ToString();

        return new List<Client>
        {
            // Machine to machine client
            new()
            {
                ClientId = "weather-api-console-client",

                // secret for authentication
                ClientSecrets = { new Secret("secret".Sha256()) },

                // no interactive user, use the clientid/secret for authentication
                AllowedGrantTypes = GrantTypes.ClientCredentials,

                // scopes that client has access to
                AllowedScopes = { "weather-api" }
            },

            // Machine to machine client
            new()
            {
                ClientId = "weather-api-worker-client",

                // secret for authentication
                ClientSecrets = { new Secret("secret".Sha256()) },

                // no interactive user, use the clientid/secret for authentication
                AllowedGrantTypes = GrantTypes.ClientCredentials,

                // scopes that client has access to
                AllowedScopes = { "weather-api" }
            },

            // Machine to machine client
            new()
            {
                ClientId = "weather-summary-api-console-client",

                // secret for authentication
                ClientSecrets = { new Secret("secret".Sha256()) },

                // no interactive user, use the clientid/secret for authentication
                AllowedGrantTypes = GrantTypes.ClientCredentials,

                // scopes that client has access to
                AllowedScopes = { "weather-summary-api" }
            },

            // Machine to machine client
            new()
            {
                ClientId = "weather-apis-client",

                // secret for authentication
                ClientSecrets = { new Secret("secret".Sha256()) },

                // no interactive user, use the clientid/secret for authentication
                AllowedGrantTypes = GrantTypes.ClientCredentials,

                // scopes that client has access to
                AllowedScopes = { "weather-api", "weather-summary-api" }
            },

            // Interactive web client that's using ASP.NET Core MVC
            new()
            {
                ClientId = "weather-api-mvc-client",

                // secret for authentication
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,
                RequireConsent = true,
                RequirePkce = true,

                // where to redirect to after login
                RedirectUris = { $"{mvcClientUrl}signin-oidc" },

                // where to redirect to after logout
                PostLogoutRedirectUris = { $"{mvcClientUrl}signout-callback-oidc" },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "weather-api",
                    "weather-summary-api"
                },

                AllowOfflineAccess = true
            },

            // An interactive web client that's using JavaScript and Backend for Frontend (BFF) pattern
            new()
            {
                ClientId = "bff-client",

                // secret for authentication
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                // where to redirect to after login
                RedirectUris = { $"{bffClientUrl}signin-oidc" },

                // where to redirect to after logout
                PostLogoutRedirectUris = { $"{bffClientUrl}signout-callback-oidc" },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "weather-api"
                }
            },

            // An interactive React web client using oidc-client-ts library
            new()
            {
                ClientId = "react-client",
                RequireClientSecret = false,
                RequireConsent = true,
                RequirePkce = true,

                AllowedGrantTypes = GrantTypes.Code,

                // where to redirect to after login
                RedirectUris = { $"{reactClientUrl}signin-oidc" },

                // where to redirect to after logout
                PostLogoutRedirectUris = { $"{reactClientUrl}signout-callback-oidc" },

                AllowedCorsOrigins = { reactClientUrl.TrimEnd('/') },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "weather-api"
                },

                AllowOfflineAccess = true
            },

            new()
            {
                ClientId = "wpf-client",
                ClientName = "WPF Client",

                AllowedGrantTypes = GrantTypes.DeviceFlow,
                RequireClientSecret = false,

                AlwaysIncludeUserClaimsInIdToken = true,
                AllowOfflineAccess = true,

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "weather-api"
                }
            },

            new()
            {
                ClientId = "auth-admin-client",

                // secret for authentication
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,
                RequireConsent = true,
                RequirePkce = true,

                // where to redirect to after login
                RedirectUris = { $"{authAdminUrl}signin-oidc" },

                // where to redirect to after logout
                PostLogoutRedirectUris = { $"{authAdminUrl}signout-callback-oidc" },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile
                },

                AllowOfflineAccess = true
            }
        };
    }

    public static void InitializeDatabase(IApplicationBuilder app, IConfiguration configuration)
    {
        using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()!.CreateScope();

        var appContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        appContext.Database.Migrate();

        var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        context.Database.Migrate();

        foreach (var client in GetClients(configuration))
        {
            if (!context.Clients.Any(c => c.ClientId == client.ClientId))
            {
                context.Clients.Add(client.ToEntity());
            }
        }

        foreach (var resource in IdentityResources)
        {
            if (!context.IdentityResources.Any(c => c.Name == resource.Name))
            {
                context.IdentityResources.Add(resource.ToEntity());
            }
        }

        foreach (var resource in ApiScopes)
        {
            if (!context.ApiScopes.Any(c => c.Name == resource.Name))
            {
                context.ApiScopes.Add(resource.ToEntity());
            }
        }

        context.SaveChanges();
    }
}
