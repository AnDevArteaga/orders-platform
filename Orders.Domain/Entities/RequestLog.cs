namespace Orders.Domain.Entities;

public class RequestLog
{
    public Guid Id { get; private set; }
    public Guid? MerchantId { get; private set; }
    public Guid? ApiKeyId { get; private set; }
    public string Method { get; private set; } = "";
    public string Path { get; private set; } = "";
    public int StatusCode { get; private set; }
    public long DurationMs { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RequestLog() { }

    public RequestLog(
        Guid id,
        Guid? merchantId,
        Guid? apiKeyId,
        string method,
        string path,
        int statusCode,
        long durationMs)
    {
        Id = id;
        MerchantId = merchantId;
        ApiKeyId = apiKeyId;
        Method = method;
        Path = path;
        StatusCode = statusCode;
        DurationMs = durationMs;
        CreatedAt = DateTime.UtcNow;
    }
}
