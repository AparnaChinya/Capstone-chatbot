namespace HungryBelly.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;


    [Serializable]
    public class Orders
    {
        public string name; // eg: Burger
        public string type; //Cheese
        public string quantity;
        public string price;
    }

    [LuisModel("2acfc32a-8667-431b-80da-e60ef10ac430", "b1446c3d2381426db9261a550b9f99bd")]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {

        Dictionary<string, List<string>> foodDict = new Dictionary<string, List<string>>();
        List<Orders> listOfOrders = new List<Orders>();
        public string foodTypePrompted = "";


        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            try
            {
                var userMsg = result.Query;
                // Do some basic keyword checking
                if (Regex.IsMatch(userMsg, @"\b(hello|hi|hey)\b", RegexOptions.IgnoreCase))
                {
                    //TODO : More user friendly message needed
                    await context.PostAsync("Hey there! I can help you with your food orders! What do you want to eat?");
                }
                else if (Regex.IsMatch(userMsg, @"\b(thank|thanks)\b", RegexOptions.IgnoreCase))
                {
                    await context.PostAsync("You're welcome.");
                }
                else if (Regex.IsMatch(userMsg, @"\b(bye|goodbye)\b", RegexOptions.IgnoreCase))
                {
                    await context.PostAsync("Okay, bye for now! Visit us again.");
                }
                else
                {
                    await context.PostAsync("Hmm I'm not sure what you want. Still learning, sorry!");
                }
            }
            catch (Exception)
            {
                await context.PostAsync("Argh something went wrong :( Sorry about that.");
            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("no_intent")]
        public async Task no_intent(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            string message = "";
            foreach (var item in listOfOrders)
            {
                message += item.quantity + " " + item.name + " " + "\n";
            }
            await dialogContext.PostAsync("Thank you for the order!" + message);
        }


        [LuisIntent("order")]
        public async Task Order(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            foodTypePrompted = "";
            Orders myOrder = new Orders();
            //TODO: Add this dictionary globally
            List<string> bur = new List<string>();
            bur.Add("veggie");
            bur.Add("masala");
            bur.Add("cheese");
            foodDict["burger"] = bur;

            List<string> sal = new List<string>();
            sal.Add("ceaser");
            sal.Add("garden");
            sal.Add("spicy");
            foodDict["salad"] = sal;

            var message = await activity;
            var entities = new List<EntityRecommendation>(result.Entities);

            List<string> food = new List<string>();
            string foodType = "";
            string quantity = "";

            if (!entities.Any((entity) => entity.Type == "food"))
            {
                foreach (var entity in entities)
                {
                    switch (entity.Type.ToLower())
                    {
                        case "food":
                            {
                                if (entity.Entity.ToLower().Contains("burger"))
                                {
                                    food.Add("burger");
                                }
                                if (entity.Entity.ToLower().Contains("salad"))
                                {
                                    food.Add("salad");
                                }
                                if (entity.Entity.ToLower().Contains("soup"))
                                {
                                    food.Add("soup");
                                }
                                if (entity.Entity.ToLower().Contains("fries"))
                                {
                                    food.Add("fries");
                                }
                                break;
                            }
                        case "builtin.number":
                            {
                                quantity = entity.Entity;
                                break;
                            }
                    }
                }

                foreach (var item in food)
                {
                    var x = entities.Select(a => a).Where(b => b.Type.ToLower().Contains(item.ToLower()));
                    if (x.Count() != 0 && foodDict.ContainsKey(item))
                    {
                        foodType = x.First().Entity;
                    }
                }

                if (foodType == "")
                {

                    //TODO: this is not optimized, write proper logic

                    var m1 = "We have different kinds of " + food[0];
                    string messageDialog = "";
                    foreach (var item in foodDict[food[0]])
                    {
                        messageDialog += item + "\n";
                    }
                    await dialogContext.PostAsync(m1 + "\n" + messageDialog);
                    PromptDialog.Text(dialogContext, ResumeAfterOrderFoodClarification, "What kind do you want to order?");
                    foodType = foodTypePrompted;
                    listOfOrders.Add(new Orders
                    {
                        name = foodType,
                        quantity = quantity.ToString()
                    }
                    );
                }
                else
                {
                    listOfOrders.Add(new Orders
                    {
                        name = foodType,
                        quantity = quantity.ToString()
                    }
                    );
                    await dialogContext.PostAsync("Do you want to order anything else?");
                }
            }

        }

        private async Task AskForMoreFood(IDialogContext context, IAwaitable<bool> result)
        {
            var confirmation = await result;
            string message = "";
            //foreach (var item in listOfOrders)
            //{
            //    message += item.quantity + " " + item.name + " " + "\n";
            //}
            await context.PostAsync(confirmation ? "What do you want to order?\n" : "You are done\n");
        }

        private async Task ResumeAfterOrderFoodClarification(IDialogContext context, IAwaitable<string> result)
        {
            var foods = await result;
            await context.PostAsync($"I see you want to order {foods}");
            foodTypePrompted = foods;
            await context.PostAsync("\n Do you want to order anything else?");

        }


        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            //TODO: Display the menu
            await context.PostAsync("Let me show you the menu!");
            context.Wait(this.MessageReceived);
        }
    }
}
