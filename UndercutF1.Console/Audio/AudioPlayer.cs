using Microsoft.Extensions.Options;

namespace UndercutF1.Console.Audio;

public class AudioPlayer(IOptions<ConsoleOptions> options, ILogger<AudioPlayer> logger)
{
    private ChildProcess? _process = null;

    public bool Playing => !_process?.Completion.IsCompleted ?? false;

    public bool Errored => _process?.Completion.IsFaulted ?? false;

    public void Play(string filePath)
    {
        if (options.Value.PreferFfmpegPlayback || OperatingSystem.IsWindows())
        {
            _process = Run(
                "ffplay",
                "-nodisp",
                "-autoexit",
                "-hide_banner",
                "-loglevel",
                "error",
                filePath
            );
        }
        else if (OperatingSystem.IsMacOS())
        {
            _process = Run("afplay", filePath);
        }
        else if (OperatingSystem.IsLinux())
        {
            _process = Run("mpg123", "--no-control", "--no-visual", "--quiet", filePath);
        }
        else
        {
            _process = null;
            throw new InvalidOperationException(
                "Unable to find a suitable audio playback method for the current operating system"
            );
        }
    }

    public void Stop()
    {
        if (_process is not null && !_process.Completion.IsCompleted)
        {
            logger.LogDebug("Stopping audio playback pid:{PID}", _process.Id);
            _process.Kill();
        }
        else
        {
            logger.LogDebug("No running process to stop");
        }
    }

    private ChildProcess Run(string executable, params string[] args)
    {
        logger.LogDebug(
            "Beginning audio playback with executable {Executable}, arguments: {Arguments}",
            executable,
            string.Join(' ', args)
        );
        var process = new ChildProcessBuilder()
            .WithFileName(executable)
            .WithArguments(args)
            .WithThrowOnError(true)
            .Run();

        process.Completion.ContinueWith(LogProcessException, TaskContinuationOptions.OnlyOnFaulted);

        return process;
    }

    private void LogProcessException(Task failed)
    {
        if (failed.Exception?.InnerException is ChildProcessErrorException ex && ex.ExitCode == 137)
        {
            // Exit code 137 is result of SIGKILL so ignore
            logger.LogDebug(
                failed.Exception,
                "Audio Playback returned 137, considering this a non-error"
            );
            _process = null;
        }
        else
        {
            logger.LogError(failed.Exception, "Audio playback failed, bad exit code");
        }
    }
}
