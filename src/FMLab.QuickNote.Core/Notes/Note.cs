namespace FMLab.QuickNote.Core.Notes;

public sealed class Note
{
    public required Guid Id { get; init; }

    public string? Title { get; set; }

    public required string Content { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public bool IsActive => CompletedAt is null;

    /// <summary>Ordenados por <see cref="Comment.CreatedAt"/> ascendente.</summary>
    public IReadOnlyList<Comment> Comments { get; set; } = [];
}
