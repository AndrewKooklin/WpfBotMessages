using System;
using System.IO;
using System.Windows;
using System.Text.Json;

namespace WpfBotMessages
{
    public partial class MainWindow : Window
    {
        TelegramClient telegramClient;
        public MainWindow()
        {
            InitializeComponent();

            telegramClient = new TelegramClient(this);
            ListMessages.ItemsSource = telegramClient.botMess;
        }

        private void buttonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (ListMessages.SelectedItems.Count != 0)
            {
                if (textMessage.Text.Length != 0)
                {
                    telegramClient.SendMessage(sendMessage.Text, textMessage.Text);
                    textMessage.Text = "";
                    MessageBox.Show("Сообщение отправлено", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Введите сообщение", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Выберите получателя из входящих сообщений", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonSaveMessage_Click(object sender, RoutedEventArgs e)
        {
            if (ListMessages.SelectedItems.Count != 0)
            {
                SaveLabel.Visibility = Visibility.Collapsed;

                Message items = (Message)(ListMessages.SelectedItem);
                string fName = items.FName;

                string path = $"{Environment.CurrentDirectory}\\{fName}.json";

                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
                {
                    JsonSerializer.SerializeAsync(fs, telegramClient.botMess);
                }
            }
            else
            {
                SaveLabel.Content = "Выберите сообщение для сохранения !";
                SaveLabel.Visibility = Visibility;
            }
        }
    }
}
