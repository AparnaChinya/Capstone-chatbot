namespace LuisBot

{
	using System;	
	using Microsoft.Bot.Builder.FormFlow;
	public class SelectSpecificFoodItem
	{
		[Prompt("Which one would you like to have?")]
		[Optional]
		public string SpecificFoodType { get; set; }
	}
}