namespace Portfolio.Domain.Common;

/// <summary>
/// Bir iş kuralı ihlal edildiğinde fırlatılır (ör. zorunlu alanın boş bırakılması).
/// </summary>
public sealed class DomainException(string message) : Exception(message);
