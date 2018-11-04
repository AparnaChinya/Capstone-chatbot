namespace HungryBelly.Dialogs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using LuisBot;
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
		private int currentState = 0;
		private Boolean displayWelcome = true;
        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
            {
			string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }



        [LuisIntent("no_intent")]
        public async Task handle_no_intent(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {

        }
        [LuisIntent("yes_intent")]
        public async Task handle_yes_intent(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {

        }


        [LuisIntent("order")]
		public async Task Order(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;

			if (currentState == 0)
			{

				if(displayWelcome)
					await dialogContext.PostAsync($"Welcome to chatbot for restaurants! We are analyzing your message: '{message.Text}'...");
				displayWelcome = false;

				var foodQuery = new SelectFoodType();
				EntityRecommendation foodentityRecommendation;

				if (result.TryFindEntity(EntityBurgerType, out foodentityRecommendation))
				{
					foodentityRecommendation.Type = "FoodType";
				}

				var BurgerFormDialog = new FormDialog<SelectFoodType>(foodQuery, this.BuildFoodForm, FormOptions.PromptInStart, result.Entities);

				dialogContext.Call(BurgerFormDialog, this.ResumeAfterFoodFormDialog);
				currentState = 1;
			}
			else if (currentState == 1)
			{
				var specificFoodQuery = new SelectSpecificFoodItem();
				EntityRecommendation foodentityRecommendation;

				if (result.TryFindEntity(EntityBurgerType, out foodentityRecommendation))
				{
					foodentityRecommendation.Type = "SpecificFoodType";
				}

				var BurgerFormDialog = new FormDialog<SelectSpecificFoodItem>(specificFoodQuery, this.DisplayPlacingOrder, FormOptions.PromptInStart, result.Entities);

				dialogContext.Call(BurgerFormDialog, this.ResumeAfterOrderFormDialog);
				
				currentState = 0;
			}

		}


        private IForm<SelectFoodType> BuildFoodForm()
        {
            OnCompletionAsyncDelegate<SelectFoodType> processFoodSearch = async (context, state) =>
            {
                var message = "Searching for different kinds of ";
                if (!string.IsNullOrEmpty(state.FoodType))
                {
                    message += $" {state.FoodType}...";
                }
                await context.PostAsync(message);
            };

            return new FormBuilder<SelectFoodType>()
                .Field(nameof(SelectFoodType.FoodType), (state) => string.IsNullOrEmpty(state.FoodType))
                .OnCompletion(processFoodSearch)
                .Build();
        }


		private IForm<SelectSpecificFoodItem> DisplayPlacingOrder()
		{
			return new FormBuilder<SelectSpecificFoodItem>()
				.Field(nameof(SelectSpecificFoodItem.SpecificFoodType), (state) => string.IsNullOrEmpty(state.SpecificFoodType))
				.Build();
		}

		private async Task ResumeAfterOrderFormDialog(IDialogContext context, IAwaitable<SelectSpecificFoodItem> result)
		{
			try
			{
				var searchQuery = await result;
				String postMessage = searchQuery.SpecificFoodType+" selected, would you like to order something else? If yes, please say I would like to order";
				await context.PostAsync(postMessage);
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

		private async Task ResumeAfterFoodFormDialog(IDialogContext context, IAwaitable<SelectFoodType> result)
        {
            try
            {
                var searchQuery = await result;
				String postMessage = "";
				IEnumerable<Food> foodDisplayList = null;

				switch (searchQuery.FoodType.ToLower()) {
					case "burger":
						foodDisplayList = await this.GetFoodAsync(searchQuery);
						postMessage = "I found "+foodDisplayList.Count()+" burgers.";
						break;
					case "fries":
						foodDisplayList = await this.GetFoodAsync(searchQuery);
						postMessage = "I found "+foodDisplayList.Count()+" fries.";
						break;
					default:
						postMessage = "Unkown option";
						break;
				}

				await context.PostAsync(postMessage);

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();

                foreach (var eachBurger in foodDisplayList)
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
				await context.PostAsync($"Would you like to select one of these?");
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
            await context.PostAsync("Hi! Try asking me things like 'I want to place an order', 'I would like to select burger'");

            context.Wait(this.MessageReceived);
        }


		string[] burgertype = { "Cheese", "Veggie", "Masala" };
		string[] friestype = { "HashBrown", "Garlic", "Nacho", "ShoeString", "Regular","Curly" };
		private async Task<IEnumerable<Food>> GetFoodAsync(SelectFoodType searchQuery)
        {

			var returnList = new List<Food>();
			string[] listToUse = null;
			switch (searchQuery.FoodType.ToLower())
			{
				case "burger":
					listToUse = burgertype;
					break;
				case "fries":
					listToUse = friestype;
					break;
			}
            
            for (int i = 0; i < listToUse.Length; i++)
            {
                var random = new Random(i);
                Food food = new Food()
                {
                    Name = $"{listToUse[i]} {searchQuery.FoodType}",
                    PriceStarting = random.Next(80, 450),
                    Image = $"https://placeholdit.imgix.net/~text?txtsize=35&txt={listToUse[i]}+{i}&w=500&h=260"
                };

                returnList.Add(food);
            }

			returnList.Sort((h1, h2) => h1.PriceStarting.CompareTo(h2.PriceStarting));

            return returnList;
        }

      }
}
