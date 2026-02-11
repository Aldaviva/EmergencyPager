using FreshPager.Common.Events;
using SignalRClientGenerator;

namespace FreshPager.Toast.Eventing;

[GenerateSignalRClient(Incoming = [typeof(EventsFromHub)])]
public partial class HubClient;