using System.ComponentModel.DataAnnotations;

namespace Fluffy.Data.Entities;

public abstract class DataAccessObject
{
    [Key] public Guid Id { get; set; }

    public Guid? TenantId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}