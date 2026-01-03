
namespace Orders.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}
