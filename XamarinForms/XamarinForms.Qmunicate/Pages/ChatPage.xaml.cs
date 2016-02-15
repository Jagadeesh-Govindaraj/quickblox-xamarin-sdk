﻿using Quickblox.Sdk.Modules.ChatXmppModule.ExtraParameters;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using System;
using System.Linq;
using XamarinForms.Qmunicate.Repository;
using Xamarin.Forms;
using Quickblox.Sdk.Modules.ChatModule.Models;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;

namespace XamarinForms.Qmunicate.Pages
{
    public partial class ChatPage : ContentPage
    {
        private string dialogId;
		private bool isLoaded;
		DialogTable dialog;
		List<Quickblox.Sdk.Modules.UsersModule.Models.User> users;

        public ChatPage(string dialogId)
        {
            InitializeComponent();
            this.dialogId = dialogId;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

			if (isLoaded)
				return;
			
            this.IsBusy = true;

			dialog = Database.Instance().GetDialog(dialogId);
            chatNameLabel.Text = dialog.Name;

			if (dialog.DialogType == DialogType.Group) {
				if (!string.IsNullOrEmpty (dialog.Photo)) {
					chatPhotoImage.Source = ImageSource.FromUri (new Uri (dialog.Photo));
				} else {
					chatPhotoImage.Source = Device.OnPlatform (iOS: ImageSource.FromFile ("Images/ic_launcher.png"),
						Android: ImageSource.FromFile ("ic_launcher.png"),
						WinPhone: ImageSource.FromFile ("Images/ic_launcher.png"));
				}
			} else {
				chatPhotoImage.Source = Device.OnPlatform (iOS: ImageSource.FromFile ("Images/ic_launcher.png"),
					Android: ImageSource.FromFile ("ic_launcher.png"),
					WinPhone: ImageSource.FromFile ("Images/ic_launcher.png"));
			}

			users = await App.QbProvider.GetUsersByIds (dialog.OccupantIds.Split(',').Select(id => Int32.Parse(id)));

			List<MessageTable> messages;
			try {
			   messages = await App.QbProvider.GetMessages(dialogId);
			} catch (Exception ex) {
				await App.Current.MainPage.DisplayAlert ("Error", ex.ToString(), "Ok");
				return;
			}

            if (messages != null)
            {
                messages = messages.OrderBy(message => message.DateSent).ToList();
					
				foreach (var message in messages) {
					if (message.SenderId == App.QbProvider.UserId) {
						message.RecepientFullName = "My";
					} else {
						var user = users.FirstOrDefault (u => u.Id == message.SenderId);
						if (user != null) {
							message.RecepientFullName = user.FullName;
						}
					}
				}

                Database.Instance().SaveAllMessages(dialogId, messages);

				var template = new DataTemplate (typeof(TextCell));
				template.SetBinding (ImageCell.TextProperty, "RecepientFullName");
				template.SetBinding (ImageCell.DetailProperty, "Text");
				listView.ItemTemplate = template;
                listView.ItemsSource = messages;

				//if(messages != null && messages.Any())
//					listView.ScrollTo (messages [messages.Count - 1], ScrollToPosition.Start, false);
            }

            Database.Instance().SubscribeForMessages(OnMessagesChanged);

            sendButton.Clicked += SendClicked;

            this.IsBusy = false;
			this.isLoaded = true;
        }

        private async void SendClicked(object sender, EventArgs e)
        {
			if (dialog != null && 
				(dialog.DialogType == DialogType.Group ||
				 dialog.DialogType == DialogType.PublicGroup)) {
				await App.Current.MainPage.DisplayAlert ("Error", "Comming soon. Use private chat for testing.", "Ok");
				return;
			}
            
			var message = messageEntry.Text.Trim();
            if (!string.IsNullOrWhiteSpace(message))
            {
				if (dialog.DialogType == DialogType.Private) {
					var opponentId = dialog.OccupantIds.Split (',').Select (Int32.Parse).First (id => id != App.QbProvider.UserId);

					var m = new MessageTable ();
					m.RecepientId = opponentId;
					m.SenderId = (int)App.QbProvider.UserId;
					m.Text = message;
					m.DialogId = dialogId;
					m.RecepientFullName = "My";
					m.ID = Database.Instance ().SaveMessage (m);

					dialog.LastMessage = m.Text;
					dialog.LastMessageSent = DateTime.UtcNow;
					Database.Instance ().SaveDialog (dialog);

					var chatMessageExtraParameter = new ChatMessageExtraParameter (dialogId, true);
					try {
						App.QbProvider.GetXmppClient ().SendMessage (opponentId, message, chatMessageExtraParameter.Build(), dialogId, null);
					} catch (Exception ex) {
						await App.Current.MainPage.DisplayAlert ("Error", ex.ToString(), "Ok");
					}

					messageEntry.Text = "";
				}
            }
        }

        private void OnMessagesChanged()
        {
            var messages = Database.Instance().GetMessages(dialogId);
            listView.ItemsSource = messages;

			//if (messages != null && messages.Count > 10)
				//listView.ScrollTo (messages [0], ScrollToPosition.Start, true);
        }
    }
}
