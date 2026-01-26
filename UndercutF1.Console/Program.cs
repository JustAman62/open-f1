using System.Runtime.InteropServices;
using UndercutF1.Console;

var cts = new CancellationTokenSource();

// Create a separate thread for the main application
var commandThread = new Thread(async () =>
{
    await CommandHandler.Parse(args, cts.Token);
    cts.Cancel();
})
{
    Name = "CommandHandler",
};

commandThread.Start();

// Explicitly handle SIGTERM (^C) to ensure a graceful shutdown
// (and because one is no longer registered by dotnet by default)
using var termSignalRegistration = PosixSignalRegistration.Create(
    PosixSignal.SIGINT,
    (_) => cts.Cancel()
);

// Leave the main thread to handle any actions that must be run on the main thread only.
await MainThreadDispatch.RunLoop(cts.Token);
