using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdaptiveBotAuthentication.Dialogs
{
    public class RootDialog : AdaptiveDialog
    {
        public RootDialog() : base(nameof(RootDialog))
        {
                Triggers = new List<OnCondition>
{
                new OnUnknownIntent
                {
                    Actions =
                    {
                        new SendActivity("Hi, we are up and running!!"),
                    }
                },
            };
        }
    }
}
