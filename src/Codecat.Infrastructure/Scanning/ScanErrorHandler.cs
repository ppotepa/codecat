using Microsoft.Extensions.Logging;

namespace Codecat.Scanning;

public sealed class ScanErrorHandler<TError> : IErrorHandler<TError>
{
    private readonly ILogger<ScanErrorHandler<TError>> _logger;

    public ScanErrorHandler(ILogger<ScanErrorHandler<TError>> logger)
    {
        _logger = logger;
    }

    public void Handle(TError error, string context)
    {
        _logger.LogWarning("scan error at {Path}: {Error}", context, error);
    }
}
