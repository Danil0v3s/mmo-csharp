using Core.Database.Entities;

namespace Char.Server.Services;

/// <summary>
/// GP-AUCTION — builds the "Auction Manager" mail that delivers an auctioned item (full fidelity)
/// and/or zeny on auction completion (rAthena <c>mail_sendmail</c> from the auction end/close paths).
/// Shared by the gRPC completion handlers (close / buy-now / cancel) and the expiry-timer sweep so
/// the delivery is identical regardless of how the auction ends.
/// </summary>
internal static class AuctionDelivery
{
    public static MailEntity BuildMail(AuctionEntity a, int destId, string destName, string title,
        string body, bool withItem, uint zeny)
    {
        var mail = new MailEntity
        {
            SendId = 0,
            SendName = "Auction Manager",
            DestId = destId,
            DestName = Clip(destName, 30),
            Title = Clip(title, 40),
            Message = Clip(body, 200),
            Zeny = zeny,
            Time = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Status = 0,
            Type = 0,
        };
        if (withItem && a.NameId > 0)
            mail.Attachments.Add(new MailAttachmentEntity
            {
                Index = 0,
                NameId = a.NameId,
                Amount = 1,
                Refine = a.Refine,
                Attribute = a.Attribute,
                Identify = 1,
                Card0 = a.Card0, Card1 = a.Card1, Card2 = a.Card2, Card3 = a.Card3,
                OptionId0 = a.OptionId0, OptionVal0 = a.OptionVal0, OptionParm0 = a.OptionParm0,
                OptionId1 = a.OptionId1, OptionVal1 = a.OptionVal1, OptionParm1 = a.OptionParm1,
                OptionId2 = a.OptionId2, OptionVal2 = a.OptionVal2, OptionParm2 = a.OptionParm2,
                OptionId3 = a.OptionId3, OptionVal3 = a.OptionVal3, OptionParm3 = a.OptionParm3,
                OptionId4 = a.OptionId4, OptionVal4 = a.OptionVal4, OptionParm4 = a.OptionParm4,
                UniqueId = a.UniqueId,
                Bound = 0,
                EnchantGrade = a.EnchantGrade,
            });
        return mail;
    }

    private static string Clip(string? s, int max)
    {
        s ??= string.Empty;
        return s.Length <= max ? s : s[..max];
    }
}
