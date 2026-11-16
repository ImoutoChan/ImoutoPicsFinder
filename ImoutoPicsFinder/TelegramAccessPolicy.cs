#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Telegram.Bot.Types;

namespace ImoutoPicsFinder;

public sealed class TelegramAccessPolicy
{
    public const string SettingName = "AllowedTelegramIds";

    private readonly HashSet<long> _allowedIds = new();

    public TelegramAccessPolicy(string? allowedIds)
    {
        if (string.IsNullOrWhiteSpace(allowedIds))
            return;

        foreach (var entry in allowedIds.Split(
                     new[] { ',', ';', ' ', '\t', '\r', '\n' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (!long.TryParse(entry, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                throw new ArgumentException($"{SettingName} must contain integer Telegram IDs.", nameof(allowedIds));

            _allowedIds.Add(id);
        }
    }

    public bool IsAllowed(Message? message)
        => message?.Chat is not null
           && (_allowedIds.Contains(message.Chat.Id)
               || message.From is not null && _allowedIds.Contains(message.From.Id));
}
