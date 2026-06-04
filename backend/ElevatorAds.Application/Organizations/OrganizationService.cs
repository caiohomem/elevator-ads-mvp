using System.Text.RegularExpressions;
using ElevatorAds.Domain.Common;
using ElevatorAds.Application.Organizations.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Organizations;

public sealed class OrganizationService
{
    private static readonly Regex SlugPattern = new("^[a-z0-9-]+$", RegexOptions.Compiled);

    private readonly IOrganizationRepository _repository;

    public OrganizationService(IOrganizationRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<OrganizationDto>> GetAllAsync()
    {
        var orgs = await _repository.GetAllAsync();
        return orgs.Select(Map).ToList();
    }

    public async Task<PagedResult<OrganizationDto>> GetPagedAsync(PagedQuery query)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query);
        var mappedItems = items.Select(Map).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResult<OrganizationDto>(mappedItems, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id)
    {
        var org = await _repository.GetByIdAsync(id);
        return org is null ? null : Map(org);
    }

    public async Task<OrganizationDto?> GetBySlugAsync(string slug)
    {
        var org = await _repository.GetBySlugAsync(slug);
        return org is null ? null : Map(org);
    }

    public async Task<ServiceResult<OrganizationDto>> CreateAsync(CreateOrganizationRequest request)
    {
        var normalizedName = request.Name?.Trim() ?? string.Empty;
        var normalizedSlug = request.Slug?.Trim() ?? string.Empty;

        var error = ValidateRequiredFields(normalizedName, normalizedSlug);
        if (error is not null)
        {
            return ServiceResult<OrganizationDto>.Failure(error);
        }

        if (await _repository.SlugExistsAsync(normalizedSlug))
        {
            return ServiceResult<OrganizationDto>.Failure("Slug must be unique.");
        }

        var now = DateTime.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Slug = normalizedSlug,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status!.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _repository.AddAsync(org);
        return ServiceResult<OrganizationDto>.Success(Map(created));
    }

    public async Task<ServiceResult<OrganizationDto?>> UpdateAsync(Guid id, UpdateOrganizationRequest request)
    {
        var normalizedName = request.Name?.Trim() ?? string.Empty;
        var normalizedSlug = request.Slug?.Trim() ?? string.Empty;

        var error = ValidateRequiredFields(normalizedName, normalizedSlug);
        if (error is not null)
        {
            return ServiceResult<OrganizationDto?>.Failure(error);
        }

        var org = await _repository.GetByIdAsync(id);
        if (org is null)
        {
            return ServiceResult<OrganizationDto?>.Success(null);
        }

        if (await _repository.SlugExistsAsync(normalizedSlug, excludeId: id))
        {
            return ServiceResult<OrganizationDto?>.Failure("Slug must be unique.");
        }

        org.Name = normalizedName;
        org.Slug = normalizedSlug;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            org.Status = request.Status!.Trim();
        }

        org.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(org);
        return ServiceResult<OrganizationDto?>.Success(updated is null ? null : Map(updated));
    }

    public Task<bool> DeleteAsync(Guid id) => _repository.DeleteAsync(id);

    public Task<Guid> EnsureDefaultOrganizationIdAsync(string defaultName, string defaultSlug, CancellationToken cancellationToken = default) =>
        _repository.EnsureDefaultOrganizationIdAsync(defaultName, defaultSlug, cancellationToken);

    private static string? ValidateRequiredFields(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return "Slug is required.";
        }

        if (!SlugPattern.IsMatch(slug))
        {
            return "Slug must contain only lowercase letters, digits, and hyphens.";
        }

        return null;
    }

    private static OrganizationDto Map(Organization org) =>
        new(
            org.Id,
            org.Name,
            org.Slug,
            org.Status,
            org.CreatedAt,
            org.UpdatedAt);

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);
        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
