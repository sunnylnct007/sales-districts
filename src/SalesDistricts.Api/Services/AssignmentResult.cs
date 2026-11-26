using SalesDistricts.Api.Contracts;

namespace SalesDistricts.Api.Services;

public abstract record AssignmentResult
{
    public sealed record Success(DistrictDetailDto District) : AssignmentResult;
    public sealed record NotFound(string Reason) : AssignmentResult;
    public sealed record Invalid(string Reason) : AssignmentResult;
    public sealed record Conflict(string Reason) : AssignmentResult;
}
