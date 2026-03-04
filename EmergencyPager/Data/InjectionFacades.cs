using Kasa;
using Pager.Duty;

namespace EmergencyPager.Data;

internal delegate IPagerDuty PagerDutyFactory(string integrationKey);

public delegate IEnumerable<KasaController> KasaControllerFactory(string pagerdutySubdomain);

public abstract class KasaController: IDisposable {

    public abstract string id { get; }
    public abstract IKasaOutletBase outlet { get; }

    public void Dispose() {
        outlet.Dispose();
        GC.SuppressFinalize(this);
    }

}

internal class KasaSingleOutletController(IKasaOutlet outlet): KasaController {

    public override string id { get; } = outlet.Hostname;
    public override IKasaOutlet outlet { get; } = outlet;

}

internal class KasaMultiOutletController(IMultiSocketKasaOutlet outlet, int socket): KasaController {

    public override string id { get; } = $"{outlet.Hostname}:{socket}";
    public override IMultiSocketKasaOutlet outlet { get; } = outlet;
    public int socket { get; } = socket;

}