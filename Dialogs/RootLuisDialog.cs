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
    }

    [LuisModel("112f7706-8420-43e4-a61c-2e90e29fa8a4", "ee098d9468a64996bd899bea824e8be9")]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {

        List<Orders> listOfOrders = new List<Orders>();
        HashSet<string> recommendedStrings = new HashSet<string>();
        HashSet<string> recommendedFoods = new HashSet<string>();




        /*
        [LuisIntent("requestMenu")]
        public async Task RequestMenu(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("We have menu for food and drinks.");
            PromptDialog.Text(context, handleRequestMenu, "What do want to see? - Menu for Food or for Drinks or both ?");
        }

        private async Task handleRequestMenu(IDialogContext context, IAwaitable<string> result)
        {
            var whatToShow = await result; //result.Equals("food");
            string option = whatToShow.ToLower();
            if (option.Contains("eat") || option.Contains("food"))
            {
                await printFoodMenu(context);
            }
            else if (option.Contains("drink") || option.Contains("beverage"))
            {
                await printDrinksMenu(context);
            }
            else if (option.Contains("both") || option.Contains("all"))
            {
                await printFoodMenu(context);
                await printDrinksMenu(context);
            }
            else
            {
                PromptDialog.Text(context, handleRequestMenu, "Sorry, I don't understand this yet! What would you like to know - Menu for food, Menu for Drinks or both ?");
            }

        }

        private async Task printFoodMenu(IDialogContext context)
        {
            string sfm1 = "You can choose from - Cheese, Chicken, Veggie Burgers and Curly, Shoestring, Waffle Fries to eat";
            await context.PostAsync(sfm1);
        }

        private async Task printDrinksMenu(IDialogContext context)
        {
            string sdm1 = "You can choose from - Diet Coke, Coke, Zero Coke and Lemonade to drink.";
            await context.PostAsync(sdm1);
        }

        [LuisIntent("show_food")]
        public async Task ShowFoodMenu(IDialogContext context, LuisResult result)
        {
            await printFoodMenu(context);
        }

        [LuisIntent("show_drinks")]
        public async Task ShowDrinksMenu(IDialogContext context, LuisResult result)
        {
            await printDrinksMenu(context);
        }
        */

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            try
            {
                var userMsg = result.Query;
                if (Regex.IsMatch(userMsg, @"\b(hello|hi|hey)\b", RegexOptions.IgnoreCase))
                {
                    await context.PostAsync("Hey there! I can help you with your food orders. What do you want to eat?");
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

       


        [LuisIntent("order")]
        public async Task Order(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            var entities = new List<EntityRecommendation>(result.Entities);
            var comp = new List<CompositeEntity>(result.CompositeEntities);
            await ExtractEntities(dialogContext, entities, comp, false);
        }

        [LuisIntent("remove")]
        public async Task Remove(IDialogContext dialogContext, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            var entities = new List<EntityRecommendation>(result.Entities);
            var comp = new List<CompositeEntity>(result.CompositeEntities);
            await ExtractEntities(dialogContext, entities, comp, true);
        }


        private async Task ExtractEntities(IDialogContext dialogContext,  List<EntityRecommendation> entities, List<CompositeEntity> comp, Boolean removeFlag)
        {
            string food = "";
            string foodType = "";
            int quantity = 1;

            try{
                List<Orders> orders = getTempOrder(dialogContext, comp, false, recommendedFoods);
                // TODO: Recommendation over multiple items, only first order taken now
                //Orders order = orders.First<Orders>();
                foreach (var order in orders)
                {

                    food = order.name;
                    foodType = order.type;
                    quantity = order.quantity;

                    if (foodType == "")
                    {
                        // Prompt user to specify details when removing
                        if (removeFlag)
                        {
                            await dialogContext.PostAsync("Could you mention the entire order to be removed? We couldn't find a matching order.");
                        }

                        var m1 = "We have veggie, ham and cheese burgers. Which one would you like?";
                        var m2 = "We have curly, shoestring and waffle fries. Which one would you like?";
                        var m3 = "We have small, medium and large coke. Which one would you like?";
                        dialogContext.PrivateConversationData.SetValue("food", food);

                        if (food == "burger" || food == "burgers")
                        {
                            //PromptDialog.Text(dialogContext, ResumeAfterOrderFoodClarification, m1);
                            recommendedStrings.Add(m1);
                            recommendedFoods.Add("burger");
                            recommendedFoods.Add("burgers");

                        }
                        else if (food == "fries")
                        {
                            //PromptDialog.Text(dialogContext, ResumeAfterOrderFoodClarification, m2);
                            recommendedStrings.Add(m2);
                            recommendedFoods.Add("fries");

                        }
                        else if (food == "coke")
                        {
                            //PromptDialog.Text(dialogContext, ResumeAfterOrderFoodClarification, m3);
                            recommendedStrings.Add(m3);
                            recommendedFoods.Add("coke");

                        }
                    }
                    else
                    {
                        addToOrder(listOfOrders, food, foodType, quantity, removeFlag);

                    }
                }

                if (recommendedStrings.Count > 0)
                {
                    string finalStr = "";
                    foreach (var s in recommendedStrings)
                    {
                        finalStr += s;
                    }
                    recommendedStrings.Clear();
                    PromptDialog.Text(dialogContext, ResumeAfterOrderFoodClarification, finalStr);
                } else{
                    PromptDialog.Text(dialogContext, handleOrderConfirm, "Do you want to order anything else?");
                }


            }
            catch(Exception e) {
                var exception = e.HelpLink;
                await dialogContext.PostAsync("Sorry we didn't get that. You can order burgers, fries or coke.");
            }

        }



        private async Task ResumeAfterOrderFoodClarification(IDialogContext context, IAwaitable<string> result)
        {
            var foods = await result;
            var data = await LuisApi.MakeRequest(foods);
            var intentOfResult = data["topScoringIntent"]["intent"].Value<string>();
            if (!data.Equals(null) && intentOfResult.Equals("order"))
            {


                JArray entitiesArrComp = (JArray)data["compositeEntities"];
                List<CompositeEntity> comp = entitiesArrComp.ToObject<List<CompositeEntity>>();
                List<Orders> orders = getTempOrder(context, comp, true, recommendedFoods);




                Boolean flag = false;
                flag = (orders.Count > 0);
                // get quantity with luis call
                if (flag)
                {
                    //if (!data.Equals(null) && intentOfResult.Equals("order"))

                    JArray entitiesArr = (JArray)data["entities"];
                    //JArray entitiesArrComp = (JArray)data["compositeEntities"];
                    List<EntityRecommendation> entities = entitiesArr.ToObject<List<EntityRecommendation>>();
                    //List<CompositeEntity> comp = entitiesArrComp.ToObject<List<CompositeEntity>>();

                    await ExtractEntities(context, entities, comp, false);
                    //await context.Forward(new RootLuisDialog(), this.ResumeAfterNewOrderDialog, message, CancellationToken.None);


                    //foodTypePrompted = getType(context, foods);
                    //context.PrivateConversationData.SetValue("typeFound", "true");
                    //await context.PostAsync($"Great! I'll add {foodTypePrompted} burger to your order");

                    //addToOrder(listOfOrders, "burger", foodTypePrompted, 1);

                    //PromptDialog.Text(context, handleFinalIntent, "Do you want to order anything else from here?");
                }
                else
                {
                    context.PrivateConversationData.SetValue("typeFound", "false");
                    //await context.PostAsync("Did not match with nay food type");
                    PromptDialog.Text(context, ResumeAfterOrderFoodClarification, "I'm sorry I didn't catch you. You can select the ones we have available.");
                }
            } else{
                PromptDialog.Text(context, ResumeAfterOrderFoodClarification, "I'm sorry I didn't catch you. You can select the ones we have available.");
            }
            //context.Wait(MessageReceived);
        }

        private static Boolean CheckTypeValidity(IDialogContext context, string foods)
        {
            Boolean flag = false;
            string food = context.PrivateConversationData.GetValue<String>("food");
            //List<string> foodTypes = FoodMenu.foodDict[food];
            List<string> foodTypes = FoodMenu.foodDict["fries"];
            foodTypes.Concat(FoodMenu.foodDict["coke"]);
            foodTypes.Concat(FoodMenu.foodDict["burger"]);
            foreach (string s in foodTypes)
            {
                if (foods.Contains(s))
                {
                    flag = true;
                }
            }
            return flag;
            //return true;
        }

        private static String getType(IDialogContext context, string foods)
        {
            String flag = "";
            string food = context.PrivateConversationData.GetValue<String>("food");
            List<string> foodTypes = FoodMenu.foodDict[food];
            foreach (string s in foodTypes)
            {
                if (foods.Contains(s))
                {
                    flag = s;
                }
            }
            return flag;
        }

        private static List<Orders> getTempOrder(IDialogContext context, List<CompositeEntity> entities, Boolean validity, HashSet<String> recommended){
            string food = "";
            string type = "";
            int quantity = 1;
            List<Orders> temp = new List<Orders>();
            foreach(var entity in entities){
                //entity.
                foreach(var entityChild in entity.Children){
                    if(entityChild.Type == "food"){
                        food = entityChild.Value;
                    }
                    if (entityChild.Type == "builtin.number")
                    {
                        quantity = Int32.Parse(entityChild.Value);
                    }
                    if (entityChild.Type == "foodtype")
                    {
                        type = entityChild.Value;
                    }
                }
                if(food.Equals("burgers")){
                    food = "burger";
                }
                if (food.Equals("") && !type.Equals(""))
                {

                    //food = context.PrivateConversationData.GetValue<String>("food");

                    food = FoodMenu.hmap[type];
                }
                if (validity){
                    if(!recommended.Contains(food, StringComparer.OrdinalIgnoreCase)){
                        continue;
                    }
                   
                }
               
                if (!food.Equals("") && !type.Equals(""))
                {
                    if (!FoodMenu.hmap[type].Equals(food)) continue;


                }

                if (food.Length > 7)
                {
                    type = food.Substring(0, food.IndexOf('b'));
                    food = "burger";
                }

                if (FoodMenu.foodDict[food].Contains(type))
                {
                    temp.Add(new Orders
                    {
                        name = food,
                        type = type,
                        quantity = quantity
                    });
                } else{
                    temp.Add(new Orders
                    {
                        name = food,
                        type = "",
                        quantity = quantity
                    });
                }

                food = "";
                type = "";
                quantity = 1;
            }
            return temp;
        }

        private static void addToOrder(List<Orders> listOfOrders, String name, String foodType, int quantity, Boolean removeFlag){
            Orders temp = null;
            Boolean flag = false;
            foreach(Orders order in listOfOrders){
                if(name.Equals(order.name) && foodType.Equals(order.type)){
                    if (removeFlag)
                    {
                        order.quantity = order.quantity - quantity;
                        if(order.quantity <= 0){
                            flag = true;
                            temp = order;
                        } else{
                            return;
                        }
                    }
                    else
                    {
                        order.quantity = order.quantity + quantity;
                        return;
                    }
                }
            }
            if(flag){
                listOfOrders.Remove(temp);
                return;
            }
            listOfOrders.Add(new Orders
            {
                name = name,
                type = foodType,
                quantity = quantity
            });
            return;

        }

        public static double getPrice(List<Orders> orders, Orders ord)
        {
            double total = 0.0;
            if(orders == null){
                orders = new List<Orders>();
                orders.Add(ord);
            }
            foreach (Orders order in orders)
            {
                double baseBurger = 2.0;
                double baseFries = 1.0;
                double baseCoke = 0.5;
                if (order.name == "burger" || order.name == "burgers")
                {
                    total = total + (order.quantity * (baseBurger + FoodMenu.foodDict[order.name].IndexOf(order.type) * 1.0));
                }

                if (order.name == "fries")
                {
                    total = total + (order.quantity * (baseFries + FoodMenu.foodDict[order.name].IndexOf(order.type) * 1.0));
                }

                if (order.name == "coke")
                {
                    total = total + (order.quantity * (baseCoke + FoodMenu.foodDict[order.name].IndexOf(order.type) * 1.0));
                }
            }
            return total;
        }


        private async Task handleFinalIntent(IDialogContext context, IAwaitable<string> result)
        {

            var foods = await result;
            List<string> nolist = new List<string>();
            nolist.Add("no");
            nolist.Add("nope");
            nolist.Add("nay");
            nolist.Add("nothing");
            if (nolist.Contains(foods, StringComparer.OrdinalIgnoreCase))
            {
                await context.PostAsync("You can continue to order or exclude items from your order.");
            } else if(foods.Equals("yes")){
                // context.Done("Thanks for ordering with HungryBelly. We'll get back with your order soon!");
                await context.SayAsync("Thank you for ordering with HungryBelly \\m/");
                context.EndConversation("");
            } else{
                await context.PostAsync("I didn't quite catch that. You can continue to order or exclude items from your order.");
            }
        }

        private async Task handleOrderConfirm(IDialogContext context, IAwaitable<string> result)
        {

            var foods = await result;
            String m1 = "Would you like to confirm this order?"+"\n";
            string messageDialog = m1;
            if (foods.Equals("no") || foods.Equals("nope"))
            {
                foreach (var item in listOfOrders)
                {
                    messageDialog += item.quantity + " "+item.type + " " + item.name + " $" + getPrice(null, item) + "\n";
                }

                messageDialog += "Your total comes up to $" + getPrice(listOfOrders, null);
                context.PrivateConversationData.SetValue("orderStatus", "preConfirm");
                PromptDialog.Text(context, handleFinalIntent, messageDialog);
                //await context.PostAsync(m1 + "\n" + messageDialog);
            }



            else
            {
                var data = await LuisApi.MakeRequest(foods);
                var intentOfResult = data["topScoringIntent"]["intent"].Value<string>();
                if (!data.Equals(null) && intentOfResult.Equals("order"))
                {
                    JArray entitiesArr = (JArray)data["entities"];
                    JArray entitiesArrComp = (JArray)data["compositeEntities"];
                    List<EntityRecommendation> entities = entitiesArr.ToObject<List<EntityRecommendation>>();
                    List<CompositeEntity> comp = entitiesArrComp.ToObject<List<CompositeEntity>>();

                    //context.PrivateConversationData.SetValue("custom", true);
                    await ExtractEntities(context, entities, comp, false);
                    //await context.Forward(new RootLuisDialog(), this.ResumeAfterNewOrderDialog, message, CancellationToken.None);

                }
                else
                {
                    PromptDialog.Text(context, handleOrderConfirm, "What do you want to order?");
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
