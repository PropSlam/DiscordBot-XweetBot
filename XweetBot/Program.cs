using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace XweetBot;

public partial class Program {
    public static Task Main( string[] args ) {
        return new Program().MainAsync();
    }

    DiscordSocketClient? _client;

    readonly Regex _urlRegex = new(@"https?://(?!fxtwitter)[^\s]*(twitter|x)[^\s]+/status/[^\s]+");
    readonly string[] _targetUrls = { "x.com", "twitter.com" };

    public async Task MainAsync() {
        var root = Directory.GetCurrentDirectory();
        var dotenv = Path.Combine( root, ".env" );
        DotEnv.Load( dotenv );

        DiscordSocketConfig config = new() {
            UseInteractionSnowflakeDate = false,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
        };
        _client = new DiscordSocketClient( config );
        _client.Log += LogAsync;
        _client.MessageReceived += MessageReceived;

        await _client.LoginAsync( TokenType.Bot, Environment.GetEnvironmentVariable( "XWEETBOT_TOKEN" ) );
        await _client.StartAsync();
        await _client.SetGameAsync( "For X/Twitter Links", null, ActivityType.Watching );

        await LogAsync( new LogMessage( LogSeverity.Info, "MainAsync", "[XweetBot]: Main has started." ) );

        // Block this task until the program is closed.
        await Task.Delay( -1 );
    }

    async Task MessageReceived( SocketMessage message ) {
        if ( _client != null && message.Author.Id == _client.CurrentUser.Id ) return;
        if ( message is SocketUserMessage userMessage ) {
            var urls = ExtractUrl( userMessage.Content );
            if ( urls.Length == 0 ) return;
            var convertedMessage = "";
            foreach ( var url in urls ) {
                if ( string.IsNullOrEmpty( url ) ) continue;
                if ( TryConvertUrl( url, out var converted ) ) {
                    convertedMessage += converted + " ";
                }
            }

            if ( string.IsNullOrEmpty( convertedMessage ) ) return;
            await userMessage.ReplyAsync( convertedMessage );

            // Hide the original message's embed if it's not a DM channel.
            // (you can't hide embeds from other users' DMs)
            if ( userMessage.Channel.GetChannelType() != ChannelType.DM ) {
                await userMessage.ModifyAsync( props => {
                    props.Flags = new Optional<MessageFlags?>(MessageFlags.SuppressEmbeds);
                } );
            }
        }
    }

    string[] ExtractUrl( string message ) {
        var extracted = _urlRegex.Matches( message )
            .Select( m => m.Value )
            .ToArray();
        return extracted.Length > 0 ? extracted : Array.Empty<string>();
    }

    bool TryConvertUrl( string url, out string converted ) {
        foreach ( var turl in _targetUrls ) {
            if ( !url.Contains( turl ) ) continue;
            converted = url.Replace( turl, "fxtwitter.com" );
            LogAsync( new LogMessage( LogSeverity.Info, "TryConvertUrl", "[XweetBot]: Converted Url." ) );
            return true;
        }

        converted = "";
        return false;
    }

    static Task LogAsync( LogMessage msg ) {
        Console.WriteLine( msg.ToString() );
        return Task.CompletedTask;
    }
}