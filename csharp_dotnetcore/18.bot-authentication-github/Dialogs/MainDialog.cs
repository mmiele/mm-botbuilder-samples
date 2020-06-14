// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthenticationBot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    public class MainDialog : LogoutDialog
    {
        protected readonly ILogger Logger;

        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(MainDialog), configuration["ConnectionName"])
        {
            Logger = logger;

            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = ConnectionName,
                    Text = "Please Sign In",
                    Title = "Sign In",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                }));

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                LoginStepAsync,
                DisplayTokenPhase1Async,
                DisplayTokenPhase2Async,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse != null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("You are now logged in."), cancellationToken);


                var options = new PromptOptions()
                {
                    Prompt = MessageFactory.Text("What would you like to see?"),
                    RetryPrompt = MessageFactory.Text("That was not a valid choice, please click one of the buttons."),
                    Choices = GetChoices(),
                };

                // Prompt the user with the configured PromptOptions.
                return await stepContext.PromptAsync(nameof(ChoicePrompt), options, cancellationToken);


                // return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = MessageFactory.Text("Would you like to view your token?") }, cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayTokenPhase1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Ok, one second while I retrieve that for you...."), cancellationToken);
            await stepContext.Context.SendActivityAsync(new Activity() { Type = ActivityTypes.Typing}, cancellationToken);

            var result = stepContext.Result as FoundChoice;
            if (result != null)
            {
                var choice = result.Value.ToLower();
                stepContext.Context.TurnState["choice"] = choice;

                // Call the prompt again because we need the token. The reasons for this are:
                // 1. If the user is already logged in we do not need to store the token locally in the bot and worry
                // about refreshing it. We can always just call the prompt again to get the token.
                // 2. We never know how long it will take a user to respond. By the time the
                // user responds the token may have expired. The user would then be prompted to login again.
                //
                // There is no reason to store the token locally in the bot because we can always just call
                // the OAuth prompt to get the token or get a new token if needed.
                return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), cancellationToken: cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayTokenPhase2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse != null)
            {
                var choice = stepContext.Context.TurnState["choice"] as string;

                if (choice.Contains("repositories"))
                {
                    await ShowRepositories(stepContext.Context, tokenResponse.Token);
                }
                else if (choice.Contains("notifications"))
                {
                    await ShowNotifications(stepContext.Context, tokenResponse.Token);
                }
                else if (choice.Contains("user"))
                {
                    await ShowUserInfo(stepContext.Context, tokenResponse.Token);
                }
                else if (choice.Contains("token"))
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here is your token {tokenResponse.Token}"), cancellationToken);
                }
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task ShowRepositories(ITurnContext context, string token)
        {
            var client = new GitHubClient(token);
            var repositories = await client.GetRepositories();

            var attachments = new List<Attachment>();

            foreach (var repository in repositories)
            {
                var card = new HeroCard
                {
                    Text = $"Name: {repository.name}",
                    Subtitle = $"Owner: {repository.owner.login}",
                    Buttons = new List<CardAction>
                        {
                            new CardAction(ActionTypes.OpenUrl, title: "View", value: repository.url.Replace("https://api.github.com/repos", "https://github.com")),
                        }
                };

                attachments.Add(card.ToAttachment());
            }

            await context.SendActivityAsync(MessageFactory.Carousel(attachments));
        }

        private async Task ShowNotifications(ITurnContext context, string token)
        {
            var client = new GitHubClient(token);
            var notifications = await client.GetNotifications(false);

            var attachments = new List<Attachment>();

            foreach (var notification in notifications)
            {
                var card = new HeroCard
                {
                    Text = $"Repository: {notification.repository.name}",
                    Subtitle = $"Subject: {notification.subject.title}",
                    Buttons = new List<CardAction>
                        {
                            new CardAction(ActionTypes.OpenUrl, title: "View", value: notification.subject.url.Replace("https://api.github.com/repos", "https://github.com").Replace("pulls", "pull")),
                        }
                };

                attachments.Add(card.ToAttachment());
            }

            await context.SendActivityAsync(MessageFactory.Carousel(attachments));
        }

        private async Task ShowUserInfo(ITurnContext context, string token)
        {
            var client = new GitHubClient(token);
            var userInfo = await client.GetUser();

            var card = new HeroCard
            {
                Text = $"Your login: {userInfo.login}",
                Subtitle = $"Your email: {userInfo.email}",
            };

            await context.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()));
        }

        private IList<Choice> GetChoices()
        {
            var cardOptions = new List<Choice>()
            {
                new Choice() { Value = "Repositories", Synonyms = new List<string>() { "repositories" } },
                new Choice() { Value = "Notifications", Synonyms = new List<string>() { "notifications" } },
                new Choice() { Value = "User Info", Synonyms = new List<string>() { "user" } },
                new Choice() { Value = "View Token", Synonyms = new List<string>() { "token" } },
            };

            return cardOptions;
        }
    }
}
