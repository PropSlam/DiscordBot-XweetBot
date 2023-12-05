using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace XweetBot;

public partial class Program {
    public static Task Main( string[] args ) {
        return new Program().MainAsync();
    }
    
    DiscordSocketClient? _client;

    const string URL_REGEX_STRING = @"https?://[^\s]*(twitter|x)[^\s]+/status/[^\s]+";

    readonly Regex _urlRegex = MyRegex();
    readonly string[] _targetUrls = new string[]{ "x.com", "twitter.com" };

    public async Task MainAsync() {
        var root = Directory.GetCurrentDirectory();
        var dotenv = Path.Combine(root, ".env");
        DotEnv.Load(dotenv);
        
        DiscordSocketConfig config = new() {
            UseInteractionSnowflakeDate = false,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
        };
        _client = new DiscordSocketClient( config );
        _client.Log += Log;
        _client.MessageReceived += MessageReceived;
        
        await _client.LoginAsync( TokenType.Bot, Environment.GetEnvironmentVariable("XWEETBOT_TOKEN")  );
        await _client.StartAsync();
        await _client.SetGameAsync( "For X/Twitter.com", null, ActivityType.Watching );
        
        var log = new LogMessage( LogSeverity.Debug, "MainAsync", "MainAsync Started." );
        await Log( log );
        
        // Block this task until the program is closed.
        await Task.Delay( -1 );
    }
    
    async Task MessageReceived( SocketMessage message ) {
        if ( _client != null && message.Author.Id == _client.CurrentUser.Id ) return;
        if ( message is SocketUserMessage userMessage ) {
            var urls = ExtractURL( userMessage.Content );
            if ( urls.Length == 0 ) return;
            var newMessage = "";
            foreach ( var url in urls ) {
                if ( string.IsNullOrEmpty( url ) ) continue;
                if ( TryConvertURL( url, out var converted ) ) {
                    newMessage += converted + " ";
                }
            }
            if ( string.IsNullOrEmpty( newMessage ) ) return;
            await userMessage.ReplyAsync( newMessage );
        }
    }
    
    string[] ExtractURL( string message ) {
        var extracted = _urlRegex.Matches(message)
            .Cast<Match>()
            .Select(m => m.Value) 
            .ToArray();
        return extracted.Length > 0 ? extracted : Array.Empty<string>();
    }

    bool TryConvertURL( string url, out string converted ) {
        foreach ( var turl in _targetUrls ) {
            if ( !url.Contains( turl ) ) continue;
            converted = url.Replace( turl, "fxtwitter.com" );
            Console.WriteLine($"[TryConvertURL]: Converted URL: {converted}");
            return true;
        }
        converted = "";
        return false;
    }
    
    Task Log( LogMessage msg ) {
        Console.WriteLine( msg.ToString() );
        return Task.CompletedTask;
    }

    [GeneratedRegex(@"https?://[^\s]*(twitter|x)[^\s]+/status/[^\s]+")]
    private static partial Regex MyRegex();
}