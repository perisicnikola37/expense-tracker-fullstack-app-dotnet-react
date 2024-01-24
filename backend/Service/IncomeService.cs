using Contracts.Dto;
using Contracts.Filter;
using Domain.Exceptions;
using Domain.Models;
using FluentValidation;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Service;

public class IncomeService(DatabaseContext _context, IValidator<Income> _validator, GetAuthenticatedUserIdService getAuthenticatedUserIdService, ILogger<IncomeService> _logger)
{
	private readonly DatabaseContext _context = _context;
	private readonly IValidator<Income> _validator = _validator;
	private readonly ILogger<IncomeService> _logger = _logger;
	private readonly GetAuthenticatedUserIdService getAuthenticatedUserIdService = getAuthenticatedUserIdService;

	[HttpGet]
	public async Task<PagedResponse<List<Income>>> GetIncomesAsync(PaginationFilter filter)
	{
		try
		{
			var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
			var pagedData = await _context.Incomes
				.Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
				.Take(validFilter.PageSize)
				.ToListAsync();

			return new PagedResponse<List<Income>>(pagedData, validFilter.PageNumber, validFilter.PageSize);
		}
		catch (Exception ex)
		{
			_logger.LogError($"GetIncomesAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<List<Income>> GetLatestIncomesAsync()
	{
		try
		{
			return await _context.Incomes
			.Include(e => e.User)
			.OrderByDescending(e => e.CreatedAt)
			.Take(5)
			.ToListAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError($"GetLatestIncomesAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<Response<Income>?> GetIncomeAsync(int id)
	{
		try
		{
			var income = await _context.Incomes
				.Where(e => e.Id == id)
				.FirstOrDefaultAsync();

			if (income == null)
			{
				return null;
			}

			return new Response<Income>(income);
		}
		catch (Exception ex)
		{
			_logger.LogError($"GetIncomeAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<ActionResult<Income>> CreateIncomeAsync(Income income, ControllerBase controller)
	{
		try
		{
			var validationResult = await _validator.ValidateAsync(income);
			if (!validationResult.IsValid)
			{
				return new BadRequestObjectResult(validationResult.Errors);
			}

			var incomeGroup = await _context.IncomeGroups.FindAsync(income.IncomeGroupId) ?? throw NotFoundException.Create("IncomeGroupId", "Income group not found.");

			var userId = getAuthenticatedUserIdService.GetUserId(controller.User);

			income.UserId = (int)userId!;

			_context.Incomes.Add(income);

			await _context.SaveChangesAsync();

			return new CreatedAtActionResult("GetIncome", "Income", new { id = income.Id }, income);
		}
		catch (Exception ex)
		{
			_logger.LogError($"CreateIncomeAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<IActionResult> UpdateIncomeAsync(int id, Income updatedIncome)
	{
		try
		{
			if (id != updatedIncome.Id)
			{
				return new BadRequestResult();
			}

			_context.Entry(updatedIncome).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!IncomeExists(id))
				{
					return new NotFoundResult();
				}
				throw;
			}

			return new NoContentResult();
		}
		catch (Exception ex)
		{
			_logger.LogError($"UpdateIncomeAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<IActionResult> DeleteIncomeByIdAsync(int id)
	{
		try
		{
			var income = await _context.Incomes.FindAsync(id);

			if (income == null)
			{
				return new NotFoundResult();
			}

			_context.Incomes.Remove(income);
			await _context.SaveChangesAsync();

			return new NoContentResult();
		}
		catch (Exception ex)
		{
			_logger.LogError($"DeleteIncomeByIdAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<ActionResult<int>> GetTotalAmountOfIncomesAsync()
	{
		try
		{
			return await _context.Incomes.CountAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError($"GetTotalAmountOfIncomesAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	private bool IncomeExists(int id)
	{
		try
		{
			return _context.Incomes.Any(e => e.Id == id);
		}
		catch (Exception ex)
		{
			_logger.LogError($"IncomeExists: An error occurred. Error: {ex.Message}");
			throw;
		}
	}
}