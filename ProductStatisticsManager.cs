using EshopNew.Adapters;
using Newtonsoft.Json.Linq;
using Statistics.Business.Interfaces;
using Statistics.Data.DBEntities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Statistics.Data.Interfaces;
using static Statistics.CustomClasses.EnumsClass;


namespace Statistics.Business.Managers
{
    /// <summary>
    /// Class for eshop product statistics management
    /// </summary>
    public class ProductStatisticsManager : ApiManager, IProductStatisticsManager
    {
        public IProductStatisticsRepository PsRepository { get; private set; }

        private string className;

        /// <summary>
        /// ProductStatisticsManager Constructor
        /// </summary>
        /// <param name="psRepository">DI - service ProductStatisticsRepository</param>
        public ProductStatisticsManager(IProductStatisticsRepository psRepository)
        {
            className = GetType().Name;
            PsRepository = psRepository;
        }

        #region MAIN
        /// <summary>
        /// manage product order statistics from eshop
        /// </summary>
        /// <param name="parameters">Dictionary<string, object> parameteres of json rpc request</param>
        /// <param name="method">string eshop api method</param>
        /// <param name="interval">IntervalType interval (day, week or month)</param>
        /// <returns>JToken response message</returns>
        public async Task<JToken> ManageProductStatistics(Dictionary<string, object> parameters, string method, IntervalType interval )
        {
            JToken responseJToken = new JObject();
            responseJToken = await GetProductStatistics(parameters, method);

            //if error response
            if (responseJToken["error"] != null && responseJToken["error"].HasValues)
            {
                string error = (string)responseJToken["error"]["message"];
                Startup.Sentry.LogError(error, parameters);
            }
            else //if response has no errors
            {
                //save  data to db
                Dictionary<string, Dictionary<string, string>> resultDictionary = responseJToken["result"].ToObject<Dictionary<string, Dictionary<string, string>>>();
                if (resultDictionary.Count > 0)
                {
                    SaveStatistics(resultDictionary, parameters, interval, method);
                }
            }
            return responseJToken;
        }

        #endregion

        #region HELPERS

        /// <summary>
        /// get product statistics from eshop api
        /// </summary>
        /// <param name="parameters">Dictionary<string, object> parameters from json rpc request</param>
        /// <param name="method">string api method name</param>
        /// <returns>JToken result message from eshop api</returns>
        private async Task<JToken> GetProductStatistics(Dictionary<string, object> parameters, string method)
        {
            ApiAdapter apiAdapter = new ApiAdapter();

            //wait for response from RabbitMQ
            string result = await apiAdapter.GetResponseMessage(method.ToLower(), parameters);
            if (result != null)
            {
                return JToken.Parse(result);
            }
            else
            {
                NullReferenceException e = new NullReferenceException("Data nelze načíst, kontaktujte prosím administrátora.");
                Startup.Sentry.CreateSentryTags(className, method);
                Startup.Sentry.LogException(e, parameters);
                throw e;
            }
        }

        /// <summary>
        /// Saves eshop product orders statistics to local db
        /// </summary>
        /// <param name="resultDictionary">Dictionary<string, Dictionary<string, string>> resultDictionary section result from rpc json response</param>
        /// <param name="parameters">request parameters</param>
        /// <param name="interval">request interval (day, week, month)</param>
        /// <param name="method">eshop api method</param>
        private void SaveStatistics(Dictionary<string, Dictionary<string, string>> resultDictionary, Dictionary<string, object> parameters, IntervalType interval, string method)
        {

            //save into local db
            foreach (KeyValuePair<string, Dictionary<string, string>> row in resultDictionary)
            {
                //create new productStatistics entity
                ProductStatistics productStatistics = new ProductStatistics()
                {
                    DateFrom = Convert.ToDateTime(parameters["dateFrom"]),
                    DateTo = Convert.ToDateTime(parameters["dateTo"]),
                    TimeScaleType = interval,
                    ProductId = Int32.Parse(row.Key),
                    ProductName = row.Value["ProductName"],
                    ProductUnitsSold = Int32.Parse(row.Value["Count"])
                };
                try
                {
                    PsRepository.Insert(productStatistics);
                }
                catch (Exception e)
                {
                    Startup.Sentry.CreateSentryTags(className, method);
                    Startup.Sentry.LogException(e, parameters);
                    throw new Exception("Chyba ukládání dat.");
                }
            }
        }

        #endregion

        #region TEST METHODS
        /// <summary>
        /// just returns testing data response, normally could be called from statistics api controller with real parameters
        /// </summary>
        /// <param name="token"></param>
        /// <returns>test json rpc object</returns>
        public override string GetTestData(JToken token)
        {
            JToken response = new JObject() {
                { "jsonrpc", "2.0" },
                { "id", token["id"] },
            };
            response["result"] = new JObject() { { "test", "blabla" } };
            string json = response.ToString(Newtonsoft.Json.Formatting.None);
            return json;
        }

        #endregion
    }
}
