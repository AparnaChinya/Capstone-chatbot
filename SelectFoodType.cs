namespace HungryBelly
{
    using System;
    using Microsoft.Bot.Builder.FormFlow;

    [Serializable]
    public class SelectFoodType
    {
        [Prompt("What would you like to have?")]
        [Optional]
        public string FoodType { get; set; }

    }
}