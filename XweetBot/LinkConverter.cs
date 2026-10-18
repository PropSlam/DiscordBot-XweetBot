namespace XweetBot;

public class LinkConverter {
    public static readonly ReplaceRule[] Rules = new[] {
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
}