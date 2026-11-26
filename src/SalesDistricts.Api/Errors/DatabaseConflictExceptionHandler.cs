using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;

namespace SalesDistricts.Api.Errors;

// The service checks the rules first; this catches what only the database saw, e.g. two users at once.
public sealed class DatabaseConflictExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    // Unique and primary keys, foreign keys and checks, and the triggers' own errors.
    private static readonly HashSet<int> ConflictErrors = [2601, 2627, 547, 50001, 50002];

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not SqlException { Number: var number } sqlException || !ConflictErrors.Contains(number))
            return false;

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflicting change",
                // Trigger messages are business rules; raw constraint messages would expose the schema.
                Detail = number >= 50000
                    ? sqlException.Message
                    : "The change conflicts with the current data. Reload and try again."
            }
        });
    }
}
