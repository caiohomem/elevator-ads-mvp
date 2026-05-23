using System.Net.Mail;
using ElevatorAds.Application.Advertisers.Dtos;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;

namespace ElevatorAds.Application.Advertisers;

public sealed class AdvertiserService
{
    private readonly IAdvertiserRepository _repository;

    public AdvertiserService(IAdvertiserRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<AdvertiserDto>> GetAllAsync()
    {
        var advertisers = await _repository.GetAllAsync();
        return advertisers.Select(Map).ToList();
    }

    public async Task<AdvertiserDto?> GetByIdAsync(Guid id)
    {
        var advertiser = await _repository.GetByIdAsync(id);
        return advertiser is null ? null : Map(advertiser);
    }

    public async Task<ServiceResult<AdvertiserDto>> CreateAsync(CreateAdvertiserRequest request)
    {
        var error = Validate(request.Name, request.ContactEmail);
        if (error is not null)
        {
            return ServiceResult<AdvertiserDto>.Failure(error);
        }

        var now = DateTime.UtcNow;
        var advertiser = new Advertiser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            LegalName = request.LegalName.Trim(),
            TaxId = request.TaxId.Trim(),
            ContactName = request.ContactName.Trim(),
            ContactEmail = request.ContactEmail.Trim(),
            Phone = request.Phone.Trim(),
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _repository.AddAsync(advertiser);
        return ServiceResult<AdvertiserDto>.Success(Map(created));
    }

    public async Task<ServiceResult<AdvertiserDto?>> UpdateAsync(Guid id, UpdateAdvertiserRequest request)
    {
        var error = Validate(request.Name, request.ContactEmail);
        if (error is not null)
        {
            return ServiceResult<AdvertiserDto?>.Failure(error);
        }

        var advertiser = await _repository.GetByIdAsync(id);
        if (advertiser is null)
        {
            return ServiceResult<AdvertiserDto?>.Success(null);
        }

        advertiser.Name = request.Name.Trim();
        advertiser.LegalName = request.LegalName.Trim();
        advertiser.TaxId = request.TaxId.Trim();
        advertiser.ContactName = request.ContactName.Trim();
        advertiser.ContactEmail = request.ContactEmail.Trim();
        advertiser.Phone = request.Phone.Trim();
        advertiser.Status = request.Status;
        advertiser.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(advertiser);
        return ServiceResult<AdvertiserDto?>.Success(updated is null ? null : Map(updated));
    }

    public Task<bool> DeleteAsync(Guid id) => _repository.DeleteAsync(id);

    private static string? Validate(string name, string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (!string.IsNullOrWhiteSpace(contactEmail))
        {
            try
            {
                _ = new MailAddress(contactEmail.Trim());
            }
            catch (FormatException)
            {
                return "ContactEmail must be a valid email address.";
            }
        }

        return null;
    }

    private static AdvertiserDto Map(Advertiser advertiser) =>
        new(
            advertiser.Id,
            advertiser.Name,
            advertiser.LegalName,
            advertiser.TaxId,
            advertiser.ContactName,
            advertiser.ContactEmail,
            advertiser.Phone,
            advertiser.Status,
            advertiser.CreatedAt,
            advertiser.UpdatedAt);

    public sealed record ServiceResult<T>(bool IsSuccess, string? Error, T? Value)
    {
        public static ServiceResult<T> Success(T? value) => new(true, null, value);

        public static ServiceResult<T> Failure(string error) => new(false, error, default);
    }
}
