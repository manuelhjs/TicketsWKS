namespace Tickets.Application.Common;

/// <summary>Opción genérica valor/texto para poblar selects del front-end.</summary>
public sealed class SelectOption
{
    public string Value { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
}
