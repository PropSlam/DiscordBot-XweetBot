using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

namespace XweetBot;

public class Program {
    public static Task Main( string[] args ) {
        return new Program().MainAsync();
    }
    
    DiscordSocketClient? _client;

    string _urlRegexString =
        @"https?://[^\s]*(twitter|x)[^\s]+/status/[^\s]+";
    Regex _urlRegex;
    string[] TARGET_URLS = new string[]{ "x.com", "twitter.com" };

    public async Task MainAsync() {
        var root = Directory.GetCurrentDirectory();
        var dotenv = Path.Combine(root, ".env");
        Console.WriteLine($"[MainAsync]: Loading .env file from {dotenv}");
        DotEnv.Load(dotenv);
        
        _urlRegex = new Regex( _urlRegexString );
        
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
        Console.WriteLine( "MainAsync..." );
        
        
        
        // Block this task until the program is closed.
        await Task.Delay( -1 );
    }
    
    async Task MessageReceived( SocketMessage message ) {
        if ( message.Author.Id == _client.CurrentUser.Id ) return;
        var userMessage = message as SocketUserMessage;
        var messageCopy = message.Content;
        Console.WriteLine($"[MessageReceived]");
        var urls = ExtractURL( userMessage.Content );
        if ( urls.Length == 0 ) return;
        var newMessage = "";
        for ( int i = 0; i < urls.Length; i++ ) {
            var url = urls[i];
            if ( string.IsNullOrEmpty( url ) ) continue;
            if ( TryConvertURL( url, out var converted ) ) {
                //Add converted url to new message
                newMessage += converted + " ";
            }
        }
        if ( string.IsNullOrEmpty( newMessage ) ) return;
        Console.WriteLine($"[MessageReceived]: New message: {newMessage}");
        await userMessage.ReplyAsync( newMessage );
    }
    
    string[] ExtractURL( string message ) {
        Console.WriteLine($"[ExtractURL]: Attempting to extract URL from message: {message}");
        string [] extracted = _urlRegex.Matches(message)
            .Cast<Match>()
            .Select(m => m.Value) 
            .ToArray();
        if ( extracted.Length > 0 ) {
            Console.WriteLine($"[ExtractURL]: Extracted URL: {extracted[0]}");
            return extracted;
        }
        Console.WriteLine("[ExtractURL]: No URL extracted.");
        return Array.Empty<string>();
    }

    bool TryConvertURL( string url, out string converted ) {
        //Check if url is x.com or twitter.com and if so, convert to fxtwitter.com
        for ( var i = 0; i < TARGET_URLS.Length; i++ ) {
            if ( url.Contains(TARGET_URLS[i]) ) {
                converted = url.Replace( TARGET_URLS[i], "fxtwitter.com" );
                Console.WriteLine($"[TryConvertURL]: Converted URL: {converted}");
                return true;
            }
        }
        converted = "";
        Console.WriteLine("[TryConvertURL]: No URL converted.");
        return false;
    }
    
    Task Log( LogMessage msg ) {
        Console.WriteLine( msg.ToString() );
        return Task.CompletedTask;
    }
}