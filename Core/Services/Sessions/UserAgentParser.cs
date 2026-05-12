namespace Core.Services.Sessions;

public static class UserAgentParser
{
    public static ParsedUserAgent Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return new ParsedUserAgent("Неизвестно", "Неизвестно", "Неизвестно");
        }

        var browser = ParseBrowser(userAgent);
        var operatingSystem = ParseOperatingSystem(userAgent);
        var device = ParseDevice(userAgent);

        return new ParsedUserAgent(browser, operatingSystem, device);
    }

    private static string ParseBrowser(string userAgent)
    {
        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            return "Edge";
        }

        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
        {
            return "Firefox";
        }

        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("CriOS/", StringComparison.OrdinalIgnoreCase))
        {
            return "Chrome";
        }

        if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase))
        {
            return "Safari";
        }

        return "Неизвестно";
    }

    private static string ParseOperatingSystem(string userAgent)
    {
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "Android";
        }

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase))
        {
            return "iOS";
        }

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            return "Windows";
        }

        if (userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase))
        {
            return "macOS";
        }

        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            return "Linux";
        }

        return "Неизвестно";
    }

    private static string ParseDevice(string userAgent)
    {
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
        {
            return "Планшет";
        }

        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
        {
            return "Мобильное";
        }

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            return "Компьютер";
        }

        return "Неизвестно";
    }
}
