using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace XweetBot;

public record ReplaceRule( string NewDomain, Regex MatchRegex, Regex ReplaceRegex ) {
    public IEnumerable<string> ExtractUrls( string messageContent ) {
        var matches = MatchRegex.Matches( messageContent );
        return matches.Select( match => ReplaceRegex.Replace( match.Value, NewDomain ) );
    }
}

public partial class Program {
    public static Task Main( string[] args ) {
        return new Program().MainAsync();
    }

    DiscordSocketClient? _client;

    readonly ReplaceRule[] _rules = new[] {
        // Twitter replace rules
        new ReplaceRule(
            NewDomain: "fxtwitter.com/",
            MatchRegex: new(@"https?:\/\/(x|twitter)\.com\/(\w){1,15}\/status\/[^\s]+"),
            ReplaceRegex: new(@"(x|twitter)\.com\/")
        ),

        // Reddit replace rules
        new ReplaceRule(
            NewDomain: "rxddit.com/",
            MatchRegex: new(@"https?:\/\/(redd.it|(\w+\.)?reddit.com\/(r|u|user)\/\w+\/(s|comments))\/[^\s]+"),
            ReplaceRegex: new(@"(((\w+\.)?reddit\.com)|(redd\.it))\/")
        ),
    };

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
        await _client.SetGameAsync( "For X/Twitter/Reddit Links", null, ActivityType.Watching );

        await LogAsync( new LogMessage( LogSeverity.Info, "MainAsync", "[XweetBot]: Main has started." ) );

        // Block this task until the program is closed.
        await Task.Delay( -1 );
    }

    async Task MessageReceived( SocketMessage message ) {
        if ( _client != null && message.Author.Id == _client.CurrentUser.Id ) return;
        if ( message is SocketUserMessage userMessage ) {
            var convertedUrls = _rules.SelectMany( rule => rule.ExtractUrls( userMessage.Content ) );
            if ( !convertedUrls.Any() ) return;

            // Send replies to the user, one for each converted URL.
            foreach ( var url in convertedUrls ) {
                await userMessage.ReplyAsync( url );
                await LogAsync( new LogMessage( LogSeverity.Info, "MessageReceived", $"[XweetBot]: Converted URL: {url}" ) );
            }

            // Hide the original message's embed if it's not a DM channel.
            // (you can't hide embeds from other users' DMs)
            if ( userMessage.Channel.GetChannelType() != ChannelType.DM ) {
                // Sometimes immediately suppressing the embeds doesn't work.
                // Maybe waiting 500ms first will help?
                await Task.Delay( 500 );
                await userMessage.ModifyAsync( props => {
                    props.Flags = new Optional<MessageFlags?>( MessageFlags.SuppressEmbeds );
                } );
            }
        }
    }

    static Task LogAsync( LogMessage msg ) {
        Console.WriteLine( msg.ToString() );
        return Task.CompletedTask;
    }
}