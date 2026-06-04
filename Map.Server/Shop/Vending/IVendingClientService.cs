using Map.Server.Entities;

namespace Map.Server.Shop.Vending;

/// <summary>
/// Vending → client emit hub. Mirrors rAthena's <c>clif_vending_*</c> emitters — one method per wire
/// packet. The stall sign is an area broadcast; the open ack is sent to the vendor.
/// </summary>
public interface IVendingClientService
{
    /// <summary>rAthena <c>clif_showvendingboard</c> (ZC_STORE_ENTRY) — show the stall sign over the
    /// vendor to everyone in view, plus <c>clif_openvending_ack</c> success to the vendor.</summary>
    void OpenStall(PlayerEntity vendor, string title);

    /// <summary>rAthena <c>clif_closevendingboard</c> (ZC_DISAPPEAR_ENTRY) — remove the vendor's stall
    /// sign from everyone in view.</summary>
    void CloseStall(PlayerEntity vendor);

    /// <summary>rAthena <c>clif_openvending_ack</c> (ZC_ACK_OPENSTORE2) — open result to the vendor
    /// (0 = success).</summary>
    void OpenAck(PlayerEntity vendor, byte result);
}
