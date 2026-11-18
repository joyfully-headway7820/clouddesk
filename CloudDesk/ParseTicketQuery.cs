using System.Globalization;

namespace CloudDesk;

public static class ParseTicketQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxPage = 1_000_000;

    public static TicketQueryResult Parse(IQueryCollection query)
    {
        var status = ReadOptional(query, "status");
        if (status is not null && !TicketStatus.IsAllowed(status))
            return TicketQueryResult.Fail("status должен быть new, in_progress или done.");

        int? priority = null;
        var priorityText = ReadOptional(query, "priority");
        if (priorityText is not null)
        {
            if (!int.TryParse(priorityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPriority)
                || parsedPriority is < 1 or > 3)
                return TicketQueryResult.Fail("priority должен быть целым числом от 1 до 3.");

            priority = parsedPriority;
        }

        var sort = ReadOptional(query, "sort")?.ToLowerInvariant() ?? "desc";
        if (sort is not ("asc" or "desc"))
            return TicketQueryResult.Fail("sort должен быть asc или desc.");

        if (!TryReadInt(query, "page", 1, MaxPage, out var page, out var pageError))
            return TicketQueryResult.Fail(pageError!);

        if (!TryReadInt(query, "pageSize", DefaultPageSize, MaxPageSize, out var pageSize, out var pageSizeError))
            return TicketQueryResult.Fail(pageSizeError!);

        return TicketQueryResult.Ok(new TicketQuery(
            status,
            priority,
            ReadOptional(query, "q"),
            sort,
            page,
            pageSize));
    }

    private static string? ReadOptional(IQueryCollection query, string key)
    {
        if (!query.TryGetValue(key, out var values))
            return null;

        var text = values.ToString().Trim();
        return text.Length == 0 ? null : text;
    }

    private static bool TryReadInt(
        IQueryCollection query,
        string key,
        int fallback,
        int max,
        out int value,
        out string? error)
    {
        var text = ReadOptional(query, key);
        if (text is null)
        {
            value = fallback;
            error = null;
            return true;
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < 1 || value > max)
        {
            error = $"{key} должен быть целым числом от 1 до {max}.";
            return false;
        }

        error = null;
        return true;
    }
}
