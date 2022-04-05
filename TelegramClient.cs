using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace WpfBotMessages
{
    public class TelegramClient
    {
        private MainWindow win;

        private static ObservableCollection<Message> Messages = new ObservableCollection<Message>();
        public ObservableCollection<Message> botMess { get; set; }

        private static string Token = System.IO.File.ReadAllText(@"C:\source\token.txt");

        private static TelegramBotClient MyBot;

        public void StartBot()
        {
            MyBot = new TelegramBotClient(Token);

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            MyBot.StartReceiving(
                    UpdateAsync,
                    ErrorMessage,
                    receiverOptions);
               
            cts.Cancel(); //Отпрака отмены для остановки бота
        }

        async Task UpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
            {
                return;
            }

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;
            var name = update.Message.From.FirstName;
            var type = update.Message.Type;
            switch (update.Message.Type)
            {
                case MessageType.Text:
                    {
                        if (messageText != null)
                        {
                            switch (messageText)
                            {
                                case "/start":
                                    {
                                        await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: "\n Вас приветствует бот Andy" +
                                                  "\n отправьте сообщение в приложение WPF",
                                            cancellationToken: cancellationToken,
                                            replyMarkup: GetButtonsText());
                                        break;
                                    }
                                case "Курс валют":
                                case "/exchangeRates":
                                    {
                                        await MyBot.SendTextMessageAsync(
                                                chatId: chatId,
                                                text: "Курс валют",
                                                cancellationToken: cancellationToken,
                                                replyMarkup: GetButtonsExchange());
                                        break;
                                    }
                                case "Основное меню":
                                    {
                                        await botClient.SendTextMessageAsync(
                                                chatId: chatId,
                                                text: "Основное меню",
                                                cancellationToken: cancellationToken,
                                                replyMarkup: GetButtonsText());
                                        break;
                                    }
                                case "USD":
                                case "EUR":
                                case "GBP":
                                case "CHF":
                                    {
                                        string valueCurrency = GetExchange(messageText);

                                        await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: messageText + " - " + valueCurrency + " RUB",
                                            cancellationToken: cancellationToken,
                                            replyMarkup: GetButtonsExchange());
                                        break;
                                    }
                                case "Список команд":
                                case "/help":
                                    {
                                        await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: "\n /start - стартовое меню" +
                                                  "\n /exchangeRates - курс валют" +
                                                  "\n /help - список команд",
                                            cancellationToken: cancellationToken,
                                            replyMarkup: GetButtonsText());
                                        break;
                                    }
                                case "Загруженные файлы":
                                    {
                                        GetDownloadFiles(chatId, MyBot);
                                        break;
                                    }
                                default:
                                    {
                                        await botClient.SendTextMessageAsync(
                                             chatId: chatId,
                                             text: "\n Выберите действие с помощью кнопок или команду",
                                             cancellationToken: cancellationToken,
                                             replyMarkup: GetButtonsText());
                                        break;
                                    }
                            }                    
                        }
                        Debug.WriteLine($"Принято сообщение: '{messageText}'" +
                                        $" из чата с номером: {chatId} от {name}.");
                        win.Dispatcher.Invoke(() =>
                        {
                            Messages.Add(new Message(chatId, name, messageText));
                        });

                        Thread.Sleep(1000);
                        break;
                    }
                case MessageType.Audio:
                    {
                        DownLoadAsync(update.Message.Audio.FileId, update.Message.Audio.FileName, MyBot);
                        break;
                    }
                case MessageType.Document:
                    {
                        DownLoadAsync(update.Message.Document.FileId, update.Message.Document.FileName, MyBot);
                        break;
                    }
                case MessageType.Photo:
                    {
                        var msg = GetJToken();
                        foreach (var m in msg)
                        {
                            string fileId = m["message"]["photo"][0]["file_id"].ToString();
                            string fileName = fileId.Substring(0, 7);
                            DownLoadAsync(fileId, fileName, MyBot);
                        }
                        break;
                    }
                case MessageType.Sticker:
                    {
                        DownLoadAsync(update.Message.Sticker.FileId, update.Message.Sticker.FileId, MyBot);
                        break;
                    }
                case MessageType.Video:
                    {
                        DownLoadAsync(update.Message.Video.FileId, update.Message.Video.FileId.Substring(0, 7) + ".mp4", MyBot);
                        break;
                    }
                case MessageType.Unknown:
                    {
                        await MyBot.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вы отправили неизвестный тип",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetButtonsText());
                        break;
                    }
            }
            // Ответ бота
            await MyBot.SendTextMessageAsync(
            chatId: chatId,
            text: "Вы отправили:\n" + "Тип сообщения:" + type,
            cancellationToken: cancellationToken);

            Thread.Sleep(1000);
        }

        static Task ErrorMessage(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Получение курса валюты
        /// </summary>
        /// <param name="messageText"></param>
        /// <returns></returns>
        private static string GetExchange(string messageText)
        {
            using (WebClient wc = new WebClient())
            {
                string link = "http://www.finmarket.ru/currency/rates/";

                string html = wc.DownloadString(link);

                char[] letters = messageText.ToCharArray();
                char letterOne = letters[0];
                char letterTwo = letters[1];
                char letterThree = letters[2];

                string expr = String.Format("[{0}][{1}][{2}]\\/.+?,\\d{3}", letterOne, letterTwo, letterThree, "{4}");

                Regex reg = new Regex(expr);

                Match mc = reg.Match(html);

                string text = mc.Value.ToString();
                int lastInd = text.LastIndexOf('>', text.Length - 1);
                string valueCurrency = text.Substring(lastInd + 1, 7);
                return valueCurrency;
            }
        }
        /// <summary>
        /// Кнопки основного меню
        /// </summary>
        /// <returns></returns>
        private static IReplyMarkup GetButtonsText()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
                           {
                            new KeyboardButton[] { "Курс валют" },
                            new KeyboardButton[] { "Список команд" },
                            new KeyboardButton[] { "Загруженные файлы" }
                            })
            {
                ResizeKeyboard = true
            };

            return replyKeyboardMarkup;
        }
        /// <summary>
        /// Кнопки получения курса валют
        /// </summary>
        /// <returns></returns>
        private static IReplyMarkup GetButtonsExchange()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
                           {
                            new KeyboardButton[] { "USD", "EUR" },
                            new KeyboardButton[] { "GBP", "CHF" },
                            new KeyboardButton[] { "Основное меню" }
                            })
            {
                ResizeKeyboard = true
            };

            return replyKeyboardMarkup;
        }

        /// Получение JSON объекта
        /// </summary>
        /// <returns></returns>
        static JToken GetJToken()
        {
            WebClient webClient = new WebClient() { Encoding = Encoding.UTF8 };
            int updateId = 0;
            string url = $@"https://api.telegram.org/bot{Token}/getUpdates?offset={updateId}";
            var r = webClient.DownloadString(url);
            return JObject.Parse(r)["result"];
        }
        /// <summary>
        /// Загрузка файлов в папку приложения
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="path"></param>
        /// <param name="botClient"></param>
        async static void DownLoadAsync(string fileId, string path, ITelegramBotClient botClient)
        {
            var file = await botClient.GetFileAsync(fileId);
            FileStream fileStream = new FileStream("_" + path, FileMode.Create);
            await botClient.DownloadFileAsync(file.FilePath, fileStream);
            fileStream.Close();
            fileStream.Dispose();
        }
        /// <summary>
        /// Просмотр отправленных файлов
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="botClient"></param>
        static async void GetDownloadFiles(long chatId, ITelegramBotClient botClient)
        {
            string currentDir = Environment.CurrentDirectory;
            string[] files = Directory.GetFiles(currentDir, "_*");
            var info = new DirectoryInfo(currentDir);
            FileInfo[] fileInfoList = info.GetFiles("_*");

            if (fileInfoList.Length != 0)
            {
                foreach (var fileInfo in fileInfoList)
                {
                    using Stream stream = System.IO.File.OpenRead(fileInfo.FullName);
                    Telegram.Bot.Types.Message message = await botClient.SendDocumentAsync(
                         chatId: chatId,
                         document: new InputOnlineFile(content: stream, fileName: fileInfo.Name),
                         caption: fileInfo.Name
                     );
                }
            }
            else
            {
                await MyBot.SendTextMessageAsync(chatId, $"Файлы еще не загружены", replyMarkup: GetButtonsText());
            }

        }

        public TelegramClient(MainWindow win)
        {
            this.botMess = Messages;
            this.win = win;

            Thread thread = new Thread(new ThreadStart(StartBot));
            thread.Start();
        }

        public void SendMessage(string chatId, string Text)
        {
            long id = Convert.ToInt64(chatId);

            MyBot.SendTextMessageAsync(id, Text, replyMarkup: GetButtonsText());
        }
    }
}