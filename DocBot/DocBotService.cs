using MiniSoftware;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace DocBot;

public class DocBotService(
    ITelegramBotClient telegramBotClient,
    ILogger<DocBotService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var offset = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var updates = await telegramBotClient.GetUpdatesAsync(offset, 100, 120,
                cancellationToken: cancellationToken);
            foreach (var update in updates)
            {
                switch (update.Type)
                {
                    case UpdateType.Message when update.Message != null && update.Message.Document == null:
                        await telegramBotClient.SendTextMessageAsync(update.Message!.From.Id,
                            "Just send me a Word file and I will generate it for you! Supported properties: {{title}}",
                            cancellationToken: cancellationToken);
                        break;
                    case UpdateType.Message when update.Message?.Document != null:
                        await ProcessFileAsync(update.Message, cancellationToken);
                        break;
                }

                offset = update.Id + 1;
            }
        }
    }

    private async Task ProcessFileAsync(Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing file from {User}", message.From?.Username ?? message.From?.FirstName);
        var fileInfo = await telegramBotClient.GetFileAsync(message.Document!.FileId, cancellationToken);
        if (!fileInfo.FilePath!.EndsWith(".docx") && !fileInfo.FilePath!.EndsWith(".doc"))
        {
            await telegramBotClient.SendTextMessageAsync(message.From!.Id,
                "Only Word (*.doc/*.docx) files are supported",
                cancellationToken: cancellationToken);
            return;
        }

        using var templateStream = new MemoryStream();
        await telegramBotClient.DownloadFileAsync(fileInfo.FilePath!, templateStream, cancellationToken);
        var template = templateStream.GetBuffer();
        var values = new Dictionary<string, object>() { ["title"] = "DocBot" };
        using var generationStream = new MemoryStream();
        generationStream.SaveAsByTemplate(template, values);
        generationStream.Seek(0, 0);
        await telegramBotClient.SendDocumentAsync(message.From!.Id,
            InputFile.FromStream(generationStream, "result.docx"),
            cancellationToken: cancellationToken);
    }
}