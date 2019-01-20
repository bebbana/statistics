using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Statistics.Business.Interfaces;
using Statistics.Extensions;
using Statistics.Utils;
using Newtonsoft.Json.Linq;
using static Statistics.CustomClasses.EnumsClass;

namespace Statistics.Controllers
{
    /// <summary>
    /// Controller for eshop product orders statistics management
    /// </summary>
    public class ProductStatisticsController : Controller
    {
        public IProductStatisticsManager Manager { get; private set; }

        /// <summary>
        /// ProductStatisticsController Constructor
        /// </summary>
        public ProductStatisticsController(IProductStatisticsManager productStatisticsManager)
        {
            Manager = productStatisticsManager;
        }

        /// <summary>
        /// Returns eshop ordered product statistics by time interval (day, week or month). Statistic data are
        /// also saved into local Statistics DB.
        /// </summary>
        /// <returns>JToken response message</returns>
        [HttpGet]
        [Route("StatisticsApi/ProductStatistics/{interval}/{now}")]
        public async Task<JToken> ExecuteProductStats(IntervalType interval, DateTime now)
        {
            DateTime dateFrom = interval.GetFromDate(now);
            string methodApiName = interval.GetIntervalMethodName();
            Dictionary<string, object> parameters = new Dictionary<string, object>() { { "dateFrom", dateFrom.ToString() }, { "dateTo", now } };
            try
            {
                JToken eshopResult  = await Manager.ManageProductStatistics(parameters, methodApiName, interval);
                return eshopResult;
            }
            catch (Exception e)
            {
                return ResponseHelper.ErrorResponse("Error ", int.Parse(e.GetType().ToString()));
            }
            
        }
    }
}