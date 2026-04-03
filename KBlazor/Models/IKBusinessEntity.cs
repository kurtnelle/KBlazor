namespace KBlazor.Models;

public interface IKBusinessEntity : IEquatable<IKBusinessEntity>
{
    Guid Id { get; }

    bool IsNew => Id == Guid.Empty;

    string Name { get; }

    string ToString();

    string ToJson() => System.Text.Json.JsonSerializer.Serialize(this);
}
