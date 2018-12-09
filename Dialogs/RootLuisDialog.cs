namespace HungryBelly.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;
    using LuisBot.Dialogs;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;
    using Newtonsoft.Json.Linq;

    [Serializable]
    public class Orders
    {
        public string name; // eg: Burger
        public string type; //Cheese
        public int quantity;
        public int price;
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

        //[LuisIntent("no_intent")]
        //public async Task no_intent(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        //{
        //    string message = "";
        //    foreach (var item in listOfOrders)
        //    {
        //        message += item.quantity + " " + item.name + " " + "\n";
        //    }
        //    await dialogContext.PostAsync("Thank you for the order!" + message);
        //}


        [LuisIntent("order")]
        public async Task Order(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            foodTypePrompted = "";
            Orders myOrder = new Orders();
            dialogContext.PrivateConversationData.SetValue("food", "burger");
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


            await ExtractEntities(dialogContext, entities);
            //if (!entities.Any((entity) => entity.Type == "food"))
            //{

            //}

        }





        private async Task ExtractEntities(IDialogContext dialogContext, List<EntityRecommendation> entities)
        {
            List<string> food = new List<string>();
            string foodType = "";
            int quantity = 1;
            try{
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
                                quantity = Int32.Parse(entity.Entity);
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

                var m1 = "We have ";
                string messageDialog = "";
                    int count = 0;
                    int size = foodDict[food[0]].Count;
                foreach (var item in foodDict[food[0]])
                {
                        if (count == size - 2)
                        {
                            messageDialog += item + " and ";
                          
                        }
                        else if (count == size - 1)
                        {
                            messageDialog += item + " ";
                        }
                        else
                        {
                            messageDialog += item + ", ";
                        }
                        count++;
                }
                    messageDialog += food[0] + "s.";
                await dialogContext.PostAsync(m1 + " " + messageDialog);
                PromptDialog.Text(dialogContext, ResumeAfterOrderFoodClarification, "What kind of burger do you want to order?");
                foodType = foodTypePrompted;
                /*
                listOfOrders.Add(new Orders
                {
                    name = foodType,
                    quantity = quantity.ToString()
                }
                );*/
            }
            else
            {
                    addToOrder(listOfOrders, "burger", foodType, quantity);
                    /*
                listOfOrders.Add(new Orders
                {
                    name = "burger",
                    type=foodType,
                    quantity =  quantity.ToString()
                }
                );*/

                //await dialogContext.PostAsync("Do you want to order anything else?");
                //dialogContext.PrivateConversationData.SetValue("finalOrder", listOfOrders);

                PromptDialog.Text(dialogContext, handleFinalIntent, "Do you want to order anything else?");
            }
            }
            catch(Exception e){
                await dialogContext.PostAsync("Sorry we didn't get that. We have burgers if you would like some? ");
            }

        }

        /*
        private async Task ResumeAfterOrderFoodClarification(IDialogContext context, IAwaitable<string> result)
        {
            var foods = await result;
            await context.PostAsync($"I see you want to order {foods}");
            foodTypePrompted = foods;
            PromptDialog.Text(context, handleFinalIntent, "Do you want to order anything else from here?");

        }*/

        private async Task ResumeAfterOrderFoodClarification(IDialogContext context, IAwaitable<string> result)
        {
            var foods = await result;

            Boolean flag = false;
            flag = CheckTypeValidity(context, foods);
            // get quantity with luis call
            if (flag)
            {
                foodTypePrompted = getType(context, foods);
                context.PrivateConversationData.SetValue("typeFound", "true");
                await context.PostAsync($"Great! I'll add {foodTypePrompted} burger to your order");
                //foodTypePrompted = foods;
                //await context.SayAsync("Adding to list of orders..");
                addToOrder(listOfOrders, "burger", foodTypePrompted, 1);
                /*
                listOfOrders.Add(new Orders
                {
                    name = "burger",
                    type= foodTypePrompted,
                    quantity = "1"
                }
                );*/
                //await context.PostAsync("\n Do you want to order anything else?");
                PromptDialog.Text(context, handleFinalIntent, "Do you want to order anything else from here?");
            }
            else
            {
                context.PrivateConversationData.SetValue("typeFound", "false");
                //await context.PostAsync("Did not match with nay food type");
                PromptDialog.Text(context, ResumeAfterOrderFoodClarification, "I'm sorry we don't have that. You can select the ones we have available.");
            }
            //context.Wait(MessageReceived);
        }

        private static Boolean CheckTypeValidity(IDialogContext context, string foods)
        {
            Boolean flag = false;
            string food = context.PrivateConversationData.GetValue<String>("food");
            List<string> foodTypes = FoodMenu.foodDict["burger"];
            foreach (string s in foodTypes)
            {
                if (foods.Contains(s))
                {
                    flag = true;
                }
            }
            return flag;
        }

        private static String getType(IDialogContext context, string foods)
        {
            String flag = "";
            string food = context.PrivateConversationData.GetValue<String>("food");
            List<string> foodTypes = FoodMenu.foodDict["burger"];
            foreach (string s in foodTypes)
            {
                if (foods.Contains(s))
                {
                    flag = s;
                }
            }
            return flag;
        }

        private static void addToOrder(List<Orders> listOfOrders, String name, String foodType, int quantity){
            foreach(Orders order in listOfOrders){
                if(name.Equals(order.name) && foodType.Equals(order.type)){
                    order.quantity = order.quantity + quantity;
                    return;
                }
            }
            listOfOrders.Add(new Orders
            {
                name = "burger",
                type = foodType,
                quantity = quantity
            });
            return;

        }




        private async Task handleFinalIntent(IDialogContext context, IAwaitable<string> result)
        {

            var foods = await result;
            //string intent = "";

            //var entities = data["topScoringIntent"]["intent"].Value<string>();)

            //await context.SayAsync("Inside finalIntent"+ intentOfresult);
            string messageDialog = "";
            String m1 = "Thank you for ordering with Hungry Belly. Here is your final order.";
            if (foods.Equals("no"))
            {
                foreach (var item in listOfOrders)
                {
                    messageDialog += item.quantity + " "+item.type + " " + item.name +  "\n";
                }
                await context.PostAsync(m1 + "\n" + messageDialog);
            }



            else
            {
                var data = await LuisApi.MakeRequest(foods);
                var intentOfResult = data["topScoringIntent"]["intent"].Value<string>();
                if (!data.Equals(null) && intentOfResult.Equals("order"))
                {
                    JArray entitiesArr = (JArray)data["entities"];
                    List<EntityRecommendation> entities = entitiesArr.ToObject<List<EntityRecommendation>>();

                    await ExtractEntities(context, entities);
                    //await context.Forward(new RootLuisDialog(), this.ResumeAfterNewOrderDialog, message, CancellationToken.None);

                }
                else
                {
                    PromptDialog.Text(context, handleFinalIntent, "What do you want to order?");
                }

            }
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
