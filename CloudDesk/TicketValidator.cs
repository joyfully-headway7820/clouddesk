namespace CloudDesk;

public static class TicketValidator
{
    public static string? Validate(TicketWrite write, bool statusRequired)
    {
        if (write.Title is null || write.Title.Trim().Length is < 3 or > 120)
            return "title должен быть от 3 до 120 символов.";

        if (string.IsNullOrWhiteSpace(write.Description))
            return "description обязателен.";

        if (write.Priority is null || write.Priority is < 1 or > 3)
            return "priority должен быть целым числом от 1 до 3.";

        if (string.IsNullOrWhiteSpace(write.Status))
        {
            if (statusRequired)
                return "status обязателен: new, in_progress или done.";

            return null;
        }

        if (!TicketStatus.IsAllowed(write.Status.Trim()))
            return "status должен быть new, in_progress или done.";

        return null;
    }
}
