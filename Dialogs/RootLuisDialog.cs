namespace HungryBelly.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;

    [LuisModel("2acfc32a-8667-431b-80da-e60ef10ac430", "b1446c3d2381426db9261a550b9f99bd")]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        private const string EntityBurgerType = "BurgerType";


        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
            {
            string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("order")]
        public async Task Order(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            await dialogContext.PostAsync($"Welcome to chatbot for restaurants! We are analyzing your message: '{message.Text}'...");

            var foodQuery = new OrderFood();
            EntityRecommendation foodentityRecommendation;

            if(result.TryFindEntity(EntityBurgerType, out foodentityRecommendation))
            {
                foodentityRecommendation.Type = "FoodType";
            }

            var BurgerFormDialog = new FormDialog<OrderFood>(foodQuery, this.BuildFoodForm, FormOptions.PromptInStart, result.Entities);

            dialogContext.Call(BurgerFormDialog, this.ResumeAfterFoodFormDialog);

        }

        private IForm<OrderFood> BuildFoodForm()
        {
            OnCompletionAsyncDelegate<OrderFood> processFoodSearch = async (context, state) =>
            {
                var message = "Searching for different kinds of burger";
                if (!string.IsNullOrEmpty(state.FoodType))
                {
                    message += $" in {state.FoodType}...";
                }
                await context.PostAsync(message);
            };

            return new FormBuilder<OrderFood>()
                .Field(nameof(OrderFood.FoodType), (state) => string.IsNullOrEmpty(state.FoodType))
                .OnCompletion(processFoodSearch)
                .Build();
        }

        private async Task ResumeAfterFoodFormDialog(IDialogContext context, IAwaitable<OrderFood> result)
        {
            try
            {
                var searchQuery = await result;

                var burgers = await this.GetBurgersAsync(searchQuery);

                await context.PostAsync($"I found {burgers.Count()} burgers. Which would you like to have?");

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();

                foreach (var eachBurger in burgers)
                {
                    HeroCard heroCard = new HeroCard()
                    {
                        Title = eachBurger.Name,
                        //Subtitle = $"{eachBurger.Rating} starts. {eachBurger.NumberOfReviews} reviews. From ${eachBurger.PriceStarting} per night.",
                        Images = new List<CardImage>()
                        {
                            new CardImage() { Url = eachBurger.Image }
                        },
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "More details",
                                Type = ActionTypes.OpenUrl,
                                Value = $"https://www.bing.com/search?q=burgers+" //+ HttpUtility.UrlEncode(eachBurger.Location)
                            }
                        }
                    };

                    resultMessage.Attachments.Add(heroCard.ToAttachment());
                }

                await context.PostAsync(resultMessage);
            }
            catch (FormCanceledException ex)
            {
                string reply;

                if (ex.InnerException == null)
                {
                    reply = "You have canceled the operation.";
                }
                else
                {
                    reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                }

                await context.PostAsync(reply);
            }
            finally
            {
                context.Done<object>(null);
            }
        }

       [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Hi! Try asking me things like 'search hotels in Seattle', 'search hotels near LAX airport' or 'show me the reviews of The Bot Resort'");

            context.Wait(this.MessageReceived);
        }
        private async Task<IEnumerable<Burger>> GetBurgersAsync(OrderFood searchQuery)
        {
            var hotels = new List<Burger>();
            string[] diffBurgers = {"Cheese","Veggie","Masala"};
            // Filling the hotels results manually just for demo purposes
            for (int i = 0; i < diffBurgers.Length; i++)
            {
                var random = new Random(i);
                Burger burgers = new Burger()
                {
                    Name = $"{diffBurgers[i]} {searchQuery.FoodType}",
                    PriceStarting = random.Next(80, 450),
                    Image = $"https://placeholdit.imgix.net/~text?txtsize=35&txt={diffBurgers[i]}+{i}&w=500&h=260"
                };

                hotels.Add(burgers);
            }

            hotels.Sort((h1, h2) => h1.PriceStarting.CompareTo(h2.PriceStarting));

            return hotels;
        }

      }
}
