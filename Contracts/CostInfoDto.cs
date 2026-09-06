namespace AiDesktopClient.Contracts;

/// Backend calculates cost from model pricing and actual token usage — frontend only displays it.
public sealed class CostInfoDto
{
    public decimal InputCost { get; init; }
    public decimal OutputCost { get; init; }
    public decimal TotalCost => InputCost + OutputCost;
}
