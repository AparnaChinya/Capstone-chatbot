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
using System.Diagnostics;


using System.Net.Http;
using Newtonsoft.Json;

    public static class FoodMenu
    {
        public static Dictionary<string, List<string>> foodDict = new Dictionary<string, List<string>>();
        static FoodMenu(){
            List<string> bur = new List<string>();
            bur.Add("veggie");
            bur.Add("beef");
            bur.Add("cheese");
            foodDict["burger"] = bur;
            foodDict["burgers"] = bur;

            List<string> fries = new List<string>();
            fries.Add("curly");
            fries.Add("waffle");
            fries.Add("shoestring");
            foodDict["fries"] = fries;

            List<string> coke = new List<string>();
            coke.Add("small");
            coke.Add("medium");
            coke.Add("large");
            foodDict["coke"] = coke;


    }
    }

