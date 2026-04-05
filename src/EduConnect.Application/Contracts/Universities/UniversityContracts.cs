namespace EduConnect.Application.Contracts.Universities;

public sealed class UniversityOptionResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Domain { get; init; } = string.Empty;
}
