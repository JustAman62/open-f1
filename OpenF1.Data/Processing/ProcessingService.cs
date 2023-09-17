using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public sealed class ProcessingService
{
    private readonly IEnumerable<IProcessor> _processors;
    private readonly ILogger<ProcessingService> _logger;

    public ProcessingService(IEnumerable<IProcessor> processors, ILogger<ProcessingService> logger)
    {
        _processors = processors;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        foreach (var processor in _processors)
        {
            await processor.StartAsync().ConfigureAwait(false);
        }
    }
}
