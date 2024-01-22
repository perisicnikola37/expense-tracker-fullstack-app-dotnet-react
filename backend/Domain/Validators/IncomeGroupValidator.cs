namespace Domain.Validators;

using Domain.Models;
using FluentValidation;

public class IncomeGroupValidator : AbstractValidator<IncomeGroup>
{
    public IncomeGroupValidator()
    {
        RuleFor(incomeGroup => incomeGroup.Name).NotNull().MinimumLength(2).MaximumLength(255);
        RuleFor(incomeGroup => incomeGroup.Description).NotNull().MinimumLength(8).MaximumLength(255);
    }
}
