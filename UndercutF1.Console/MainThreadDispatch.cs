using System.Collections.Concurrent;

namespace UndercutF1.Console;

/// <summary>
/// Some actions in UndercutF1 need to be executed on the main application thread, due to OS requirements.
/// <see cref="MainThreadDispatch"/> allows <see cref="Func{TResult}"/>s to be queued up and executed on the main thread.
/// </summary>
public static class MainThreadDispatch
{
    private static BlockingCollection<(TaskCompletionSource, Func<Task>)> _channel = new();

    public static async Task Execute(Func<Task> func)
    {
        var tcs = new TaskCompletionSource();
        _channel.Add((tcs, func));
        await tcs.Task;
    }

    public static async Task RunLoop(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            // To allow the login command to spawn a webview, the main threads apartment state must be set to STA.
            // For some reason, that is only allowed after setting it to Unknown first.
            Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (tcs, func) = _channel.Take(cancellationToken);
                try
                {
                    await func();
                    tcs.SetResult();
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected, do nothing
        }
    }
}
