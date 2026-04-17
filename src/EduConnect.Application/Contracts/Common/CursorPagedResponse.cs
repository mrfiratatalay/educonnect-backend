namespace EduConnect.Application.Contracts.Common;

public sealed class CursorPagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];

    public DateTime? NextCursor { get; init; }

    public bool HasMore { get; init; }

    public int TotalCount { get; init; }
}
