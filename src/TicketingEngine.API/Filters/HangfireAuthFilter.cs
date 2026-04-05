using Hangfire.Dashboard;

namespace TicketingEngine.API.Filters;

public sealed class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        // In production: check for Admin role
        return http.User.IsInRole("Admin")
            || http.Request.Host.Host == "localhost";
    }
}
