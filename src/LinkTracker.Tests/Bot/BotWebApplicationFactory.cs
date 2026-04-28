using LinkTracker.Bot.Core.BackgroundServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NSubstitute.Exceptions;
using Telegram.Bot;

namespace LinkTracker.Tests.Bot;

public class BotWebApplicationFactory : WebApplicationFactory<LinkTracker.Bot.App.Program>
{
    public ITelegramBotClient BotMock { get; } = Substitute.For<ITelegramBotClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        if (builder.GetSetting("KafkaConsumerOptions:KafkaConsumerEnabled") == null)
        {
            builder.UseSetting("KafkaConsumerOptions:KafkaConsumerEnabled", "false");
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITelegramBotClient>();
            services.AddSingleton(BotMock);
        });
    }
}