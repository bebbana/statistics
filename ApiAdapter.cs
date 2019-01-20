using Statistics;
using Statistics.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Eshop.Services;
using Statistics.Business.Managers;
using Statistics.Data.Interfaces;
using Statistics.Utils;

namespace EshopNew.Adapters
{
    /// <summary>
    /// class represents adapter for json requests and responses processing
    /// </summary>
    public class ApiAdapter
    {
        private static readonly object messagesLock = new object();
        private static Dictionary<Guid, JToken> messages = new Dictionary<Guid, JToken>();
        private static List<Guid> awaitingMessages = new List<Guid>();
        private static ApiManager ApiManager { get; set; }
        private delegate string ManagerMethod(JToken data);

        /// <summary>
        /// execute incoming json rpc request - recently for test request processing!!!
        /// </summary>
        /// <param name="message">JToken received message</param>
        /// <param name="method">string api method name</param>
        /// <param name="psRepository">ProductStatisticsRepository psRepository</param>
        /// <returns>JToken response message</returns>
        public static JToken ExecuteReceivedMessage(JToken message, string method, IProductStatisticsRepository psRepository)
        {
            JToken result = new JObject();
            switch (method)
            {
                case "gettestdata":
                    ApiManager = new ProductStatisticsManager(psRepository);
                    result = GetInternalResponseData(message, ApiManager.GetTestData);
                    break;
                default:
                    result = ResponseHelper.ErrorResponse(" Unknown method {" + (string)message["method"], 400);                 
                    break;
            }
            return result;
        }

        /// <summary>
        /// Get json rpc response message from Rabbit
        /// </summary>
        /// <param name="method">string api method name</param>
        /// <param name="parameters">Dictionary json rpc message parameters</param>
        /// <returns>string response</returns>
        public async Task<string> GetResponseMessage(string method, Dictionary<string, object> parameters) //original Handle
        {
            await Task.Run(() => { });
            JToken response = new JObject();
            response = await GetResponseFromRabbit(method, parameters);
            return response.ToString();
        }


#pragma warning disable 1998
        /// <summary>
        /// Waiting for response 
        /// search response in messages list by its Guid until timeoputhledej response v messages podle guid až do doby vypršení timeout, after timeout return error response
        /// </summary>
        /// <param name="messageGuid"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private async Task WaitForMessage(Guid messageGuid, string method)
        {
            // Get timeout
            TimeSpan timeout = TimeSpan.FromSeconds(Startup.Timeout);
            DateTime start = DateTime.UtcNow;
            while (!messages.ContainsKey(messageGuid))
            {
                Thread.Sleep(50);
                // Check timeout
                TimeSpan elapsed = DateTime.UtcNow - start;
                if (elapsed.TotalSeconds >= timeout.TotalSeconds) //if timeout was overdrawn
                {
                    lock (messagesLock)
                    {
                        awaitingMessages.Remove(messageGuid);
                        var result = ErrorResponse("Request timeout", 408);
                        messages.Add(messageGuid, result);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// insert received response to messages dictionary for next execution - called in RabbitMessenger class
        /// </summary>
        /// <param name="message">response message</param>
        public static void OnRabbitResponseMessage(JToken message)
        {
            lock (messagesLock)
            {
                Guid guid = Guid.Parse((string)message["id"]);
                // if guid is in awaitingMessages
                if (awaitingMessages.Contains(guid))
                {
                    messages.Add(guid, message);
                }
            }
        }

        #region HELPERS
#pragma warning restore 1998
        /// <summary>
        /// //get response message from messages dictionary by Guid and return it
        /// </summary>
        /// <param name="messageGuid">Guid of json rpc response message</param>
        /// <returns></returns>
        private JToken GetMessage(Guid messageGuid)
        {
            lock (messagesLock)
            {
                JToken message = messages[messageGuid];
                messages.Remove(messageGuid);
                awaitingMessages.Remove(messageGuid);
                return message;
            }
        }

        /// <summary>
        /// initialize request - create new Guid and insert it to awaitingMessages 
        /// for later matching of response with our request
        /// </summary>
        /// <returns>Guid request message id</returns>
        private Guid InitRequest()
        {
            Guid messageGuid = Guid.NewGuid();
            lock (messagesLock)
            {
                awaitingMessages.Add(messageGuid);//first add new guid into awaiting messages
            }
            return messageGuid;
        }

        /// <summary>
        /// get response from RabbitMq
        /// </summary>
        /// <param name="method">string api method name</param>
        /// <param name="parameters"> Dictionary<string, object> json rpc message parameters </param>
        /// <returns>JToken response message</returns>
        private async Task<JToken> GetResponseFromRabbit(string method, Dictionary<string, object> parameters)
        {
            string loweredMethod = method.ToLower();
            /*create new request to send to RabbitMQ*/
            Guid guidId = InitRequest();
            RabbitMessenger.CreateRequest(loweredMethod, parameters, guidId);

            /*waiting for response from RabbitMQ*/
            await WaitForMessage(guidId, loweredMethod);
            return GetMessage(guidId);
        }

        /// <summary>
        /// /get internal data for response message
        /// </summary>
        /// <param name="data">JToken testing data for response from statistics</param>
        /// <param name="method">string method name</param>
        /// <returns></returns>
        private static JToken GetInternalResponseData(JToken data, ManagerMethod method)
        {
            return JObject.Parse(method(data));
        }
        #endregion

    }
}
