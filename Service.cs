// Copyright (c) Timofei Zhakov. All rights reserved.

using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using System.Text.RegularExpressions;

namespace HttpCatBot
{
    public class Service : BackgroundService
    {
        private readonly ILogger logger;
        private readonly ITelegramBotClient botClient;
        private readonly IStatusCodeProvider statusCodeProvider;

        private static readonly Regex numberRegex = new Regex("[0-9]{3}");

        public Service(ILogger<Service> logger, ITelegramBotClient botClient, IStatusCodeProvider statusCodeProvider)
        {
            this.logger = logger;
            this.botClient = botClient;
            this.statusCodeProvider = statusCodeProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Bot starting receiving events");

            await botClient.ReceiveAsync(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                null, stoppingToken
            );

            logger.LogInformation("Bot stopped receiving events");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            try
            {
                Message? message = update.Message;
                if (message != null)
                {
                    long chatId = message.Chat.Id;
                    string? messageText = message.Text;

                    if (messageText != null)
                    {
                        try
                        {
                            if (messageText == "/start")
                            {
                                await ProcessStartMessageAsync(chatId, cancellationToken);
                            }
                            else if (messageText == "/random")
                            {
                                await SendStatusCodeAsync(chatId, statusCodeProvider.GetRandomStatusCode(), cancellationToken);
                            }
                            else
                            {
                                Match match = numberRegex.Match(messageText);
                                if (match.Success)
                                {
                                    StatusCodeData? statusCodeData = statusCodeProvider.GetStatusCode(int.Parse(match.Value));
                                    if (statusCodeData == null)
                                    {
                                        await SendErrorMessage(chatId, cancellationToken);
                                    }
                                    else
                                    {
                                        await SendStatusCodeAsync(chatId, statusCodeData, cancellationToken);
                                    }
                                }
                                else
                                {
                                    await SendErrorMessage(chatId, cancellationToken);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error while processing message: '{messageText}'.", ex);
                        }

                        logger.LogInformation("Successfully processed message: '{messageText}' from {userId}", messageText, chatId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while handling update:");
            }
        }

        private async Task SendStatusCodeAsync(long chatId, StatusCodeData statusCode, CancellationToken cancellationToken)
        {
            await botClient.SendPhotoAsync(chatId, InputFile.FromUri("https://http.cat/" + statusCode.StatusCode),
                                           caption: string.Format("{0}", statusCode.Description),
                                           parseMode: ParseMode.Markdown,
                                           cancellationToken: cancellationToken);
        }

        private async Task ProcessStartMessageAsync(long chatId, CancellationToken cancellationToken)
        {
            // TODO:
            await botClient.SendPhotoAsync(chatId, InputFile.FromUri("https://http.cat/300"),
                                           caption: "Hello, cat!\n\nPlease enter status code or /random to get random status code.\n\nFor example, try `404`",
                                           parseMode: ParseMode.Markdown,
                                           cancellationToken: cancellationToken);
        }

        private async Task SendErrorMessage(long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendPhotoAsync(chatId, InputFile.FromUri("https://http.cat/400"),
                                           caption: "Bad request! Please enter a valid status code or type /random",
                                           cancellationToken: cancellationToken);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError("Telegram error: {exception}", exception);
            return Task.CompletedTask;
        }
    }
}