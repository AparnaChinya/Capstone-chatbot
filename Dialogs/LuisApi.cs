using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LuisBot.Dialogs
{
    public static class LuisApi
    {
        //static void Main(string[] args)
        //{
        //    MakeRequest("I want to order");
        //    Console.WriteLine("Hit ENTER to e<xit...");
        //    Console.ReadLine();
        //}

        public  async static Task<JObject> MakeRequest(String query)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(query);

            // This app ID is for a public sample app that recognizes requests to turn on and turn off lights
            var luisAppId = "2acfc32a-8667-431b-80da-e60ef10ac430";
            var endpointKey = "9cd99bc8b2844b11b5ef6b5791a64b5b";

            // The request header contains your subscription key
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", endpointKey);

            // The "q" parameter contains the utterance to send to LUIS
            queryString["q"] = query;

            // These optional request parameters are set to their default values
            queryString["timezoneOffset"] = "0";
            queryString["verbose"] = "false";
            queryString["spellCheck"] = "false";
            queryString["staging"] = "false";

            var endpointUri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/" + luisAppId + "?" + queryString;
            var response = await client.GetAsync(endpointUri);

            var strResponseContent = await response.Content.ReadAsStringAsync();

            var data = (JObject)JsonConvert.DeserializeObject(strResponseContent);
            return data;
            //var intent = data["topScoringIntent"]["intent"].Value<string>();
            //var entities = data["topScoringIntent"]["intent"].Value<string>();

            //return intent;
            //return intent;
            // Display the JSON result from LUIS

        }
    }
}
