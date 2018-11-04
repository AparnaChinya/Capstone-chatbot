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
        private string previous_state = "";
        private string item_selected = "";
        private List<string> orderList = new List<string>();
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
            if(orderList.Count() != 0)
            {
                string displayOrderText = "Your order:";
                foreach (var item in orderList)
                {
                    displayOrderText = displayOrderText + "\n" + item;

                }
                await dialogContext.PostAsync(displayOrderText);
            }
            else
            {
                string displayTextMessage = "Thank you for your time. You may come back and place an order anytime!";
                await dialogContext.PostAsync(displayTextMessage);
            }
        }

        [LuisIntent("yes_intent")]
        public async Task handle_yes_intent(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            switch(previous_state)
            {
                case "ORDER":
                    await Select(dialogContext, activity, result);
                    break;
                case "SELECT":
                    await Order(dialogContext,activity,result);
                    break;
            }
        }

        public async Task Select(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            previous_state = "SELECT";
            var specificFoodQuery = new SelectSpecificFoodItem();
            EntityRecommendation foodentityRecommendation;

            if (result.TryFindEntity(EntityBurgerType, out foodentityRecommendation))
            {
                foodentityRecommendation.Type = "SpecificFoodType";
            }

            var BurgerFormDialog = new FormDialog<SelectSpecificFoodItem>(specificFoodQuery, this.DisplayPlacingOrder, FormOptions.PromptInStart, result.Entities);

            dialogContext.Call(BurgerFormDialog, this.ResumeAfterOrderFormDialog);


        }

        [LuisIntent("order")]
		public async Task Order(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            previous_state = "ORDER";
            var message = await activity;

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
                //to do: validate the selected item 
                orderList.Add(searchQuery.SpecificFoodType);
                String postMessage = searchQuery.SpecificFoodType+" selected, would you like to order something else?";
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
                        item_selected = "BURGER";
                        foodDisplayList = await this.GetFoodAsync(searchQuery);
						postMessage = "I found "+foodDisplayList.Count()+" burgers.";
						break;
					case "fries":
                        item_selected = "BURGER";
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
                string displayMenuText = "The options are...";
                foreach (var item in foodDisplayList)
                {
                    displayMenuText = displayMenuText + "\n" + item.Name;

                }
                await context.PostAsync(displayMenuText);
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
