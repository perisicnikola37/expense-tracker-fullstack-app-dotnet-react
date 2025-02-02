using Contracts.Dto;
using Contracts.Filter;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Models;
using FluentValidation;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Service;

public class IncomeService(
	DatabaseContext context,
	IValidator<Income> validator,
	IGetAuthenticatedUserIdService getAuthenticatedUserId,
	ILogger<IncomeService> logger,
	IHttpContextAccessor httpContextAccessor) : IIncomeService
{
	[HttpGet]
	public async Task<PagedResponseDto<List<IncomeResponseDto>>> GetIncomesAsync(
	PaginationFilterDto filter)
	{
		try
		{
			var authenticatedUserId = getAuthenticatedUserId.GetUserId(httpContextAccessor.HttpContext.User);

			string description = httpContextAccessor.HttpContext.Request.Query["description"]!;
			string minAmount = httpContextAccessor.HttpContext.Request.Query["minAmount"]!;
			string maxAmount = httpContextAccessor.HttpContext.Request.Query["maxAmount"]!;
			string incomeGroupId = httpContextAccessor.HttpContext.Request.Query["incomeGroupId"]!;

			var validFilter = new PaginationFilterDto(filter.PageNumber, filter.PageSize);

			var query = context.Incomes
				  .Where(e => e.UserId == authenticatedUserId)
				  .ApplyFilter(e => e.Description.Contains(description), !string.IsNullOrWhiteSpace(description))
				  .ApplyFilter(e => e.Amount >= float.Parse(minAmount), !string.IsNullOrWhiteSpace(minAmount))
				  .ApplyFilter(e => e.Amount <= float.Parse(maxAmount), !string.IsNullOrWhiteSpace(maxAmount))
				  .ApplyFilter(e => e.IncomeGroupId == int.Parse(incomeGroupId), !string.IsNullOrWhiteSpace(incomeGroupId));

			var totalRecords = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalRecords / validFilter.PageSize);

			var pagedData = await query
				.OrderByDescending(e => e.CreatedAt)
				.Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
				.Take(validFilter.PageSize)
				.Select(e => new IncomeResponseDto
				{
					Id = e.Id,
					Description = e.Description,
					Amount = e.Amount,
					CreatedAt = e.CreatedAt,
					IncomeGroupId = e.IncomeGroupId,
					IncomeGroup = (e
						.IncomeGroup != null
						? new IncomeGroupDto
						{
							Id = e.IncomeGroup.Id,
							Name = e.IncomeGroup.Name,
							Description = e.IncomeGroup.Description
						}
						: null)!,
					UserId = e.UserId
				})
				.ToListAsync();

			var baseUri = new Uri(httpContextAccessor.HttpContext.Request.Scheme + "://" + httpContextAccessor.HttpContext.Request.Host.Value);
			var currentPageUri = new Uri(httpContextAccessor.HttpContext.Request.Path, UriKind.Relative);
			var nextPageUri = new Uri(baseUri,
				$"{currentPageUri}?pageNumber={validFilter.PageNumber + 1}&pageSize={validFilter.PageSize}");
			var previousPageUri = new Uri(baseUri,
				$"{currentPageUri}?pageNumber={validFilter.PageNumber - 1}&pageSize={validFilter.PageSize}");

			return new PagedResponseDto<List<IncomeResponseDto>>(pagedData, validFilter.PageNumber,
			 validFilter.PageSize)
			{
				PageNumber = validFilter.PageNumber,
				PageSize = validFilter.PageSize,
				FirstPage = new Uri(baseUri, $"{currentPageUri}?pageNumber=1&pageSize={validFilter.PageSize}"),
				LastPage =
				 new Uri(baseUri, $"{currentPageUri}?pageNumber={totalPages}&pageSize={validFilter.PageSize}"),
				TotalPages = totalPages,
				TotalRecords = totalRecords,
				NextPage = validFilter.PageNumber < totalPages ? nextPageUri : null,
				PreviousPage = validFilter.PageNumber > 1 ? previousPageUri : null
			};
		}
		catch (Exception ex)
		{
			logger.LogError($"GetIncomesAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<object> GetLatestIncomesAsync()
	{
		try
		{
			var authenticatedUserId = getAuthenticatedUserId.GetUserId(httpContextAccessor.HttpContext.User);

			var highestIncome = await context.Incomes
				.Where(i => i.CreatedAt >= DateTime.UtcNow.AddDays(-7) && i.UserId == authenticatedUserId)
				.OrderByDescending(i => i.Amount)
				.Select(i => i.Amount)
				.FirstOrDefaultAsync();

			var latestIncomes = await context.Incomes
				.Where(i => i.UserId == authenticatedUserId)
				.Include(e => e.IncomeGroup)
				.OrderByDescending(e => e.CreatedAt)
				.Take(5)
				.ToListAsync();

			var response = new
			{
				highestIncome,
				incomes = latestIncomes
			};

			return response;
		}
		catch (Exception ex)
		{
			logger.LogError($"GetLatestIncomesAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<ActionResult<Income>> GetIncomeAsync(int id)
	{
		try
		{
			var income = await context.Incomes
				.Where(e => e.Id == id)
				.FirstOrDefaultAsync();

			if (income == null) return null!;

			return new OkObjectResult(income);
		}
		catch (Exception ex)
		{
			logger.LogError($"GetIncomeAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<ActionResult<Income>> CreateIncomeAsync(Income income)
	{
		try
		{
			var validationResult = await validator.ValidateAsync(income);
			if (!validationResult.IsValid) return new BadRequestObjectResult(validationResult.Errors);

			_ = await context.IncomeGroups.FindAsync(income.IncomeGroupId) ??
				throw NotFoundException.Create("IncomeGroupId", "Income group not found.");

			var userId = getAuthenticatedUserId.GetUserId(httpContextAccessor.HttpContext.User);

			income.UserId = (int)userId!;

			context.Incomes.Add(income);
			await context.SaveChangesAsync();

			return new CreatedAtActionResult("GetIncome", "Income", new { id = income.Id }, income);
		}
		catch (Exception ex)
		{
			logger.LogError($"CreateIncomeAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<IActionResult> UpdateIncomeAsync(int id, Income income)
	{
		try
		{
			if (id != income.Id) return new BadRequestResult();

			var authenticatedUserId = getAuthenticatedUserId.GetUserId(httpContextAccessor.HttpContext.User);

			// Check if authenticatedUserId has a value
			if (authenticatedUserId.HasValue)
			{
				// Attach authenticated user id
				income.UserId = authenticatedUserId.Value;

				context.Entry(income).State = EntityState.Modified;

				try
				{
					await context.SaveChangesAsync();
				}
				catch (ConflictException)
				{
					if (!IncomeExists(id)) return new NotFoundResult();
					throw new ConflictException();
				}

				return new NoContentResult();
			}

			return new BadRequestResult();
		}
		catch (Exception ex)
		{
			logger.LogError($"UpdateIncomeAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<IActionResult> DeleteIncomeByIdAsync(int id)
	{
		try
		{
			var income = await context.Incomes.FindAsync(id);

			if (income == null) return new NotFoundResult();

			context.Incomes.Remove(income);
			await context.SaveChangesAsync();

			return new NoContentResult();
		}
		catch (Exception ex)
		{
			logger.LogError($"DeleteIncomeByIdAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<ActionResult<int>> GetTotalAmountOfIncomesAsync()
	{
		try
		{
			return await context.Incomes.CountAsync();
		}
		catch (Exception ex)
		{
			logger.LogError($"GetTotalAmountOfIncomesAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	public async Task<IActionResult> DeleteAllIncomesAsync()
	{
		try
		{
			var authenticatedUserId = getAuthenticatedUserId.GetUserId(httpContextAccessor.HttpContext.User);

			if (!authenticatedUserId.HasValue) return new BadRequestResult();

			var incomesToDelete = await context.Incomes
				.Where(e => e.UserId == authenticatedUserId.Value)
				.ToListAsync();

			if (incomesToDelete.Count == 0) return new NotFoundResult();

			context.Incomes.RemoveRange(incomesToDelete);
			await context.SaveChangesAsync();

			return new NoContentResult();
		}
		catch (Exception ex)
		{
			logger.LogError($"DeleteAllIncomesAsync: An error occurred. Error: {ex.Message}");
			throw;
		}
	}

	private bool IncomeExists(int id)
	{
		try
		{
			return context.Incomes.Any(e => e.Id == id);
		}
		catch (Exception ex)
		{
			logger.LogError($"IncomeExists: An error occurred. Error: {ex.Message}");
			throw;
		}
	}
}