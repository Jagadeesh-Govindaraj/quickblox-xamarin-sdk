﻿using System;
using System.Linq;
using Quickblox.Sdk.Modules.ChatXmppModule;
using XamainForms.Qmunicate.Repository;
using Xamarin.Forms;
using System.Xml.Linq;
using System.Diagnostics;

namespace XamainForms.Qmunicate.Pages
{
    public partial class ChatsPage : ContentPage
    {
        public ChatsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (Device.OS == TargetPlatform.iOS)
            {
                Title = "Chats";
            }

            listView.ItemTapped += OnItemTapped;

            var dialogs = await App.QbProvider.GetDialogs();
            listView.ItemsSource = dialogs;

            Database.Instance().SaveAllDialogs(dialogs);
            Database.Instance().SubscribeForDialogs(OnDialogsChanged);

            ConnetToXmpp();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Database.Instance().UnSubscribeForDialogs(OnDialogsChanged);
        }

        private void OnDialogsChanged()
        {
            var dialogs = Database.Instance().GetDialogs();
            listView.ItemsSource = dialogs;
        }

        private void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            var dialogItem = e.Item as DialogTable;
            App.Navigation.PushAsync(new ChatPage(dialogItem.DialogId));
        }

        private void ConnetToXmpp()
        {
            if (!App.QbProvider.GetXmppClient().IsConnected)
            {
                var userSetting = Database.Instance().GetUserSettingTable();
                App.QbProvider.GetXmppClient().MessageReceived -= OnMessageReceived;
                App.QbProvider.GetXmppClient().MessageReceived += OnMessageReceived;

                App.QbProvider.GetXmppClient().ErrorReceived -= OnError;
                App.QbProvider.GetXmppClient().ErrorReceived += OnError;

                App.QbProvider.GetXmppClient().StatusChanged += OnStatusChanged;
                App.QbProvider.GetXmppClient().StatusChanged += OnStatusChanged;
                App.QbProvider.GetXmppClient().Connect(App.QbProvider.UserId, userSetting.Password);
            }
        }

        private void OnStatusChanged(object sender, StatusEventArgs statusEventArgs)
        {
            Debug.WriteLine("Xmpp Status: " + statusEventArgs.Jid + " Status: " + statusEventArgs.Status.Availability);
        }

        private void OnError(object sender, ErrorEventArgs errorsEventArgs)
        {
            Debug.WriteLine("Xmpp Error: " + errorsEventArgs.Exception + " Reason: " + errorsEventArgs.Reason);
        }

        private async void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            if (messageEventArgs.Message.MessageType == Quickblox.Sdk.Modules.ChatXmppModule.Models.MessageType.Chat ||
                messageEventArgs.Message.MessageType == Quickblox.Sdk.Modules.ChatXmppModule.Models.MessageType.Groupchat)
            {
                var messageTable = new MessageTable();
                messageTable.SenderId = messageEventArgs.Message.From.GetUserId();
                messageTable.RecepientId = messageEventArgs.Message.To.GetUserId();
                messageTable.Text = messageEventArgs.Message.Body;
                messageTable.DialogId = messageEventArgs.Message.Thread;

                if (!string.IsNullOrWhiteSpace(messageEventArgs.Message.ExtraParameter))
                {
                    XDocument xDoc = XDocument.Parse(messageEventArgs.Message.ExtraParameter);
                    var messageId = xDoc.Descendants(XName.Get("message_id", "jabber:client")).FirstOrDefault();
                    if (messageId != null)
                    {
                        messageTable.MessageId = messageId.Value;
                    }
                }

                Database.Instance().SaveMessage(messageTable);

                var dialog = Database.Instance().GetDialog(messageTable.DialogId);
                if (dialog == null)
                {
                    dialog = await App.QbProvider.GetDialog(new int[] { messageTable.SenderId, messageTable.RecepientId });
                }

                if (dialog != null)
                {
                    dialog.LastMessage = messageEventArgs.Message.Body;
                    dialog.LastMessageSent = DateTime.UtcNow;

                    if (dialog.UnreadMessageCount != null)
                    {
                        dialog.UnreadMessageCount++;
                    }
                    else
                    {
                        dialog.UnreadMessageCount = 1;
                    }

                    Database.Instance().SaveDialog(dialog);
                }
            }
        }
    }
}
