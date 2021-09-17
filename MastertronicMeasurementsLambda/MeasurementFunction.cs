using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using Environment = System.Environment;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MastertronicMeasurementsLambda
{
    public class MastertronicMeasurementFunction 
    {        
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            LambdaLogger.Log($"function start\r\n{JsonConvert.SerializeObject(request)}\r\n");

            try
            {
                var server = Environment.GetEnvironmentVariable("SERVER");
                var database = Environment.GetEnvironmentVariable("DB");
                var username = Environment.GetEnvironmentVariable("USER");
                var password = Environment.GetEnvironmentVariable("PASSWORD");

                var connectionString = $"Server={server};Database={database};User Id={username};password={password};";

                var measurements = new List<MeasurementResponse>();

                var sql = $"select * from Measurements";

                if (request.QueryStringParameters?.ContainsKey("parameter") ?? false)
                {
                    var parameter = string.Empty;

                    request.QueryStringParameters.TryGetValue("parameter", out parameter);

                    if (!string.IsNullOrEmpty(parameter))
                        sql += $" where parameter='{parameter.Replace("'","")}'";
                }

                sql += " order by record_time";

                LambdaLogger.Log(sql + "\r\n");

                using (var db = new SqlConnection(connectionString))
                {
                    var rows = db.Query<MeasurementResponse>(sql);

                    measurements = rows.ToList();
                }

                var response = new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    IsBase64Encoded = false,
                    Body = JsonConvert.SerializeObject(measurements),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}