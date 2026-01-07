using System.Text.Json.Nodes;
using SharpWebview;
using SharpWebview.Content;
using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class AccountLogin(Formula1Account accountService, ILogger<AccountLogin> logger)
{
    public async Task<Formula1Account.TokenPayload?> LoginAsync(Action<LoginStatus> onStatusUpdate)
    {
        await MainThreadDispatch.Execute(async () => await LoginCoreAsync(onStatusUpdate));
        return accountService.Payload;
    }

    public async Task LogoutAsync()
    {
        var configFileJson = await ReadConfigFileAsync();
        configFileJson.Remove(nameof(ConsoleOptions.Formula1AccessToken));

        await File.WriteAllTextAsync(
            ConsoleOptions.ConfigFilePath,
            configFileJson.ToJsonString(ConsoleSerializerContext.Pretty.Options)
        );
    }

    private async Task LoginCoreAsync(Action<LoginStatus> onStatusUpdate)
    {
        var token = LoginWithWebView(onStatusUpdate);

        if (token is null)
            return;

        // Read in the existing config file, then write out the file including the access token
        // We read the file rather than just save the config to try and avoid changing other contents in the file
        // e.g. we might have config set by environment variables that shouldn't end up in the file
        // or the file might have keys in it that we don't read in to config, but we shouldn't remove from the file.
        var configFileJson = await ReadConfigFileAsync();
        configFileJson[nameof(ConsoleOptions.Formula1AccessToken)] = token;
        await File.WriteAllTextAsync(
            ConsoleOptions.ConfigFilePath,
            configFileJson.ToJsonString(ConsoleSerializerContext.Pretty.Options)
        );

        accountService.Refresh(token);

        if (accountService.IsAuthenticated != Formula1Account.AuthenticationResult.Success)
        {
            onStatusUpdate(LoginStatus.Failed);
        }
    }

    private string? LoginWithWebView(Action<LoginStatus> onStatusUpdate)
    {
        if (OperatingSystem.IsLinux())
        {
            // Workaround for Nvidia driver issues and Wayland
            // See https://github.com/JustAman62/undercut-f1/issues/144
            Environment.SetEnvironmentVariable("WEBKIT_DISABLE_DMABUF_RENDERER", "1");
        }

        using var webView = new Webview(debug: false, interceptExternalLinks: false);

        var cookie = default(string);

        webView
            .SetTitle("Login to Formula 1")
            .SetSize(1024, 768, WebviewHint.None)
            .SetSize(400, 400, WebviewHint.Min)
            .Bind(
                "sendLoginCookie",
                (id, req) =>
                {
                    // The params are sent as an array of strings
                    // We know theres only one element, so strip the array start and end chars to get the element.
                    cookie = req[2..^2];

                    logger.LogDebug("F1 account cookie received from WebView binding");
                    onStatusUpdate(LoginStatus.TokenReceived);

                    webView.Terminate();
                }
            )
            .InitScript(GetInitScript())
            .Navigate(new UrlContent("https://account.formula1.com/#/en/login"))
            // Run() blocks until the WebView is closed.
            .Run();

        if (cookie is null)
        {
            logger.LogDebug("No cookie set when WebView closed");
            onStatusUpdate(LoginStatus.Failed);
            return null;
        }

        onStatusUpdate(LoginStatus.Complete);

        return cookie;
    }

    private static string GetInitScript()
    {
        var cookieHandling = """
            function getCookie(name) {
                return (name = (document.cookie + ';').match(new RegExp(name + '=.*;'))) && name[0].split(/=|;/)[1];
            }

            var previousCookie = "";
            setInterval(() => {
                let cookie = getCookie('login-session');
                if (cookie && previousCookie !== cookie) {
                    sendLoginCookie(cookie);
                    previousCookie = cookie;
                    document.body.insertAdjacentText('afterbegin', 'Login Complete, you may now close the browser');
                }
            }, 1000);
            """;

        if (OperatingSystem.IsMacOS())
        {
            // WebView on Mac doesn't handle keyboard shortcuts properly, so add handling manually
            // See https://github.com/webview/webview/issues/403#issuecomment-787569812
            // And https://github.com/facebook/sapling/commit/3c9d72bc43b17abe4a89cef63f22eee8a60269c2
            return $$"""
                {{cookieHandling}}

                window.addEventListener('keypress', (event) => {
                    if (!event.metaKey) { return; }
                    switch (event.key) {
                        case 'c':
                            document.execCommand('copy');
                            event.preventDefault();
                            return;
                        case 'x':
                            document.execCommand('cut');
                            event.preventDefault();
                            return;
                        case 'v':
                            document.execCommand('paste');
                            event.preventDefault();
                            return;
                        case 'a':
                            document.execCommand('selectAll');
                            event.preventDefault();
                            return;
                        case 'z':
                            document.execCommand('undo');
                            event.preventDefault();
                            return;
                    }
                });
                """;
        }
        else
        {
            return cookieHandling;
        }
    }

    private static async Task<JsonObject> ReadConfigFileAsync()
    {
        var configFileContents = await File.ReadAllTextAsync(ConsoleOptions.ConfigFilePath);
        return JsonNode
                .Parse(configFileContents, new() { PropertyNameCaseInsensitive = true })
                ?.AsObject()
            ?? throw new InvalidOperationException("Unable to parse config JSON");
    }

    public enum LoginStatus
    {
        TokenReceived,
        Failed,
        Complete,
    }
}
