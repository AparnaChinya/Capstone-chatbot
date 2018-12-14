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
    public static Dictionary<String, String> hmap = new Dictionary<string, string>();
    public static List<string> nolist = new List<string>();
    public static List<string> yeslist = new List<string>();
    static FoodMenu(){

        hmap.Add("veggie", "burger");
        hmap.Add("ham", "burger");
        hmap.Add("cheese", "burger");

        hmap.Add("shoestring", "fries");
        hmap.Add("curly", "fries");
        hmap.Add("waffle", "fries");

        hmap.Add("small", "coke");
        hmap.Add("medium", "coke");
        hmap.Add("large", "coke");

        List<string> bur = new List<string>();
            bur.Add("veggie");
            bur.Add("ham");
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
                    
            nolist.Add("no");
            nolist.Add("nope");
            nolist.Add("nay");
            nolist.Add("nothing");
            nolist.Add("none");
            nolist.Add("thats it");

            yeslist.Add("yes");
            yeslist.Add("yup");
            yeslist.Add("yep");
            yeslist.Add("ya");
            yeslist.Add("yeah");
            yeslist.Add("confirm");
    }
    }

