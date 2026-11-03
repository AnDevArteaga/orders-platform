using Orders.Domain.Common;

namespace Orders.Domain.Entities;

public class WebhookDelivery : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public Guid? OrderId { get; private set; }
    public string Type { get; private set; } = "";
    public string Payload { get; private set; } = "";
    public int Attempts { get; private set; }
    public int? LastStatusCode { get; private set; }
    public bool Delivered { get; private set; }

    private WebhookDelivery() { }

    public WebhookDelivery(Guid merchantId, Guid? orderId, string type, string payload)
    {
        MerchantId = merchantId;
        OrderId = orderId;
        Type = type;
        Payload = payload;
    }

    public void RecordAttempt(int statusCode, bool delivered)
    {
        Attempts += 1;
        LastStatusCode = statusCode;
        Delivered = delivered;
    }
}
