using Kasa;

namespace EmergencyPager.Data;

public delegate IEnumerable<KasaController> KasaControllerFactory(string pagerdutySubdomain);

public abstract class KasaController: IDisposable {

    public abstract string id { get; }
    public abstract IKasaOutletBase outlet { get; }

    public void Dispose() {
        outlet.Dispose();
        GC.SuppressFinalize(this);
    }

}

internal sealed class KasaSingleOutletController(IKasaOutlet outlet): KasaController {

    public override string id { get; } = outlet.Hostname;
    public override IKasaOutlet outlet { get; } = outlet;

}

internal sealed class KasaMultiOutletController(IMultiSocketKasaOutlet outlet, int socket): KasaController {

    public override string id { get; } = $"{outlet.Hostname}:{socket}";
    public override IMultiSocketKasaOutlet outlet { get; } = outlet;
    public int socket { get; } = socket;

}