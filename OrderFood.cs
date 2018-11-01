namespace HungryBelly
{
    using System;
    using Microsoft.Bot.Builder.FormFlow;

    [Serializable]
    public class OrderFood
    {
        [Prompt("What would you like to have?")]
        [Optional]
        public string FoodType { get; set; }

    }
}