namespace CardStock.Domain.Census;

/// <summary>One raw populations row: a (grader, grade) cell's value at one observation.</summary>
public sealed record CensusObservation(string Grader, short Grade, int Population, DateTimeOffset ObservedAt);
