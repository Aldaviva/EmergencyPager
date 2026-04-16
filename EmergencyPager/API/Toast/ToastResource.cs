namespace EmergencyPager.API.Toast;

public sealed class ToastResource: WebResource {

    public void map(IEndpointRouteBuilder webapp) => webapp.MapHub<ToastHub>("/pagerduty/toasts");

}