using DocBot;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);
var token = builder.Configuration.GetSection("Telegram:Token").Get<string>();
builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient) =>
        new TelegramBotClient(
            new TelegramBotClientOptions(string.IsNullOrWhiteSpace(token)
                ? throw new Exception("Configure Telegram:Token")
                : token),
            httpClient));
builder.Services.AddHostedService<DocBotService>();

var host = builder.Build();
host.Run();