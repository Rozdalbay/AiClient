namespace AiDesktopClient.Contracts;

// стоимость считает тоже бэкенд: по прайсу модели и честному usage; фронт - чисто витрина, не считай деньги на клиенте, скурвишься
public sealed class CostInfoDto
{
    public decimal InputCost { get; init; }
    public decimal OutputCost { get; init; }
    public decimal TotalCost => InputCost + OutputCost;
}
