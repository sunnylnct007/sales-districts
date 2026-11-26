using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace SalesDistricts.Api.Authorization;

public static class AuthorizationSetup
{
    public static IServiceCollection AddDistrictAuthorization(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var settings = configuration.GetSection(AuthenticationSettings.SectionName).Get<AuthenticationSettings>()
                       ?? new AuthenticationSettings();

        if (settings.Enabled)
        {
            if (string.IsNullOrWhiteSpace(settings.Authority) || string.IsNullOrWhiteSpace(settings.Audience))
                throw new InvalidOperationException(
                    "Authentication:Authority and Authentication:Audience are required when authentication is enabled.");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = settings.Authority;
                    options.Audience = settings.Audience;
                });

            services.AddSingleton<IAuthorizationHandler, CanViewDistrictsHandler>();
            services.AddSingleton<IAuthorizationHandler, CanEditAssignmentsHandler>();
        }
        else
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Authentication can only be disabled in Development.");

            services.AddAuthentication();   // no scheme: every request is anonymous
            services.AddSingleton<IAuthorizationHandler, AllowAllHandler>();
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.CanViewDistricts, p => p.AddRequirements(new CanViewDistrictsRequirement()))
            .AddPolicy(Policies.CanEditAssignments, p => p.AddRequirements(new CanEditAssignmentsRequirement()));

        return services;
    }
}
