using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace ImoutoPicsFinder.Tests;

public class TelegramAccessPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" , ; \r\n")]
    public void EmptyListDeniesAllMessages(string? setting)
    {
        var policy = new TelegramAccessPolicy(setting);

        Assert.False(policy.IsAllowed(CreateMessage(-1001234567890, 123456789)));
    }

    [Theory]
    [InlineData("-1001234567890", -1001234567890, 999, true)]
    [InlineData("123456789", -1009999999999, 123456789, true)]
    [InlineData("123456789", 123456789, 123456789, true)]
    [InlineData("-1001234567890, 123456789", -1009999999999, 999, false)]
    [InlineData("-1001234567890;\r\n123456789\t123456789", -1009999999999, 123456789, true)]
    public void AllowsMatchingChatOrSender(string setting, long chatId, long senderId, bool allowed)
    {
        var policy = new TelegramAccessPolicy(setting);

        Assert.Equal(allowed, policy.IsAllowed(CreateMessage(chatId, senderId)));
    }

    [Fact]
    public void AllowsChatWithoutSenderButDeniesUnlistedChatAndMissingMessage()
    {
        var policy = new TelegramAccessPolicy("-1001234567890");

        Assert.True(policy.IsAllowed(CreateMessage(-1001234567890, null)));
        Assert.False(policy.IsAllowed(CreateMessage(-1009999999999, null)));
        Assert.False(policy.IsAllowed(null));
        Assert.False(policy.IsAllowed(new Message()));
    }

    [Theory]
    [InlineData("123456789, anyone")]
    [InlineData("*")]
    [InlineData("9223372036854775808")]
    public void RejectsMalformedConfiguration(string setting)
    {
        Assert.Throws<ArgumentException>(() => new TelegramAccessPolicy(setting));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task BothEndpointsIgnoreDeniedAndNonMessageUpdatesWithoutStartingProcessing(
        bool boosty, bool hasMessage)
    {
        var logger = new RecordingLogger();
        var function = new ImoutoPicsFinderFunction(logger, new TelegramAccessPolicy("123456789"));
        var context = new DefaultHttpContext();
        var json = hasMessage
            ? """
              {"update_id":1,"message":{"message_id":1,"chat":{"id":-1009999999999,"type":"supergroup"},"from":{"id":999,"is_bot":false,"first_name":"Test"},"text":"https://boosty.to/example"}}
              """
            : "{\"update_id\":1}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

        if (boosty)
            await function.ProcessBoosty(context.Request);
        else
            await function.Update(context.Request);

        Assert.Empty(logger.Levels);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static Message CreateMessage(long chatId, long? senderId)
        => new()
        {
            Chat = new Chat { Id = chatId, Type = chatId < 0 ? ChatType.Supergroup : ChatType.Private },
            From = senderId.HasValue ? new User { Id = senderId.Value, FirstName = "Test" } : null
        };

    private sealed class RecordingLogger : ILogger<ImoutoPicsFinderFunction>
    {
        public List<LogLevel> Levels { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);
    }
}
