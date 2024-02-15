// Copyright (c) Timofei Zhakov. All rights reserved.

using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace HttpCatBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            IHostBuilder builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureHostConfiguration(builder =>
            {
                builder.AddUserSecrets(typeof(Program).Assembly);
            });

            builder.ConfigureServices((context, services) =>
            {
                services.Configure<Config>(context.Configuration.GetSection(Config.Name));

                services.AddSingleton<IStatusCodeProvider, StatusCodeProvider>();

                services.AddHostedService<Service>();

                services.AddHttpClient("telegram_bot_client")
                    .AddTypedClient<ITelegramBotClient>((httpClient, serviceProvider) =>
                    {
                        Config botConfig = serviceProvider.GetRequiredService<IOptions<Config>>().Value;

                        TelegramBotClientOptions options = new TelegramBotClientOptions(botConfig.ApiKey);
                        return new TelegramBotClient(options, httpClient);
                    });
            });

            IHost app = builder.Build();

            app.Run();
        }
    }
}