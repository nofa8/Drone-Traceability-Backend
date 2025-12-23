using Serilog.Sinks.SystemConsole.Themes;

namespace dTITAN.Backend.Logging;

public static class AnsiConsoleThemes
{
    // Common ANSI colors and helpers
    public static class AnsiColors
    {
        public const string Reset = "\x1b[0m";
        public const string Black = "\x1b[30m";
        public const string Red = "\x1b[31m";
        public const string Green = "\x1b[32m";
        public const string Yellow = "\x1b[33m";
        public const string Blue = "\x1b[34m";
        public const string Magenta = "\x1b[35m";
        public const string Cyan = "\x1b[36m";
        public const string White = "\x1b[37m";

        public const string BrightBlack = "\x1b[90m";
        public const string BrightRed = "\x1b[91m";
        public const string BrightGreen = "\x1b[92m";
        public const string BrightYellow = "\x1b[93m";
        public const string BrightBlue = "\x1b[94m";
        public const string BrightMagenta = "\x1b[95m";
        public const string BrightCyan = "\x1b[96m";
        public const string BrightWhite = "\x1b[97m";
    }

    public static AnsiConsoleTheme Custom { get; } = new AnsiConsoleTheme(
        new Dictionary<ConsoleThemeStyle, string>
        {
            [ConsoleThemeStyle.Text] = AnsiColors.Reset,
            [ConsoleThemeStyle.SecondaryText] = AnsiColors.BrightBlack,
            [ConsoleThemeStyle.TertiaryText] = AnsiColors.BrightBlack,
            [ConsoleThemeStyle.Invalid] = AnsiColors.Magenta,
            [ConsoleThemeStyle.Null] = AnsiColors.BrightBlack,
            [ConsoleThemeStyle.Name] = AnsiColors.Reset,
            [ConsoleThemeStyle.String] = AnsiColors.Cyan,
            [ConsoleThemeStyle.Number] = AnsiColors.BrightMagenta,
            [ConsoleThemeStyle.Boolean] = AnsiColors.Magenta,
            [ConsoleThemeStyle.Scalar] = AnsiColors.White,
            [ConsoleThemeStyle.LevelVerbose] = AnsiColors.Cyan,
            [ConsoleThemeStyle.LevelDebug] = AnsiColors.Cyan,
            [ConsoleThemeStyle.LevelInformation] = AnsiColors.Green,
            [ConsoleThemeStyle.LevelWarning] = AnsiColors.Yellow,
            [ConsoleThemeStyle.LevelError] = AnsiColors.Red,
            [ConsoleThemeStyle.LevelFatal] = AnsiColors.BrightRed,
        });
}
