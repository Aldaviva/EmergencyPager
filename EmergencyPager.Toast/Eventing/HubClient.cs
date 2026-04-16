using EmergencyPager.Common.Events;
using SignalRClientGenerator;

namespace EmergencyPager.Toast.Eventing;

[GenerateSignalRClient(Incoming = [typeof(EventsFromHub)])]
public sealed partial class HubClient;