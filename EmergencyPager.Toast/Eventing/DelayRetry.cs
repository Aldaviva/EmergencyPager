using Microsoft.AspNetCore.SignalR.Client;

namespace EmergencyPager.Toast.Eventing;

public class DelayRetry(Func<long, TimeSpan> delay): IRetryPolicy {

    public TimeSpan? NextRetryDelay(RetryContext retryContext) => delay(retryContext.PreviousRetryCount);

}