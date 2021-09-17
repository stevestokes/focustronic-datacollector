using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using Z.Dapper.Plus;
using Environment = System.Environment;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MastertronicMeasurementsAddLambda
{
    public class MastertronicMeasurementsAddFunction
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

                var measurements = JsonConvert.DeserializeObject<IEnumerable<Measurements>>(request.Body);

                // save new data to db
                using (var db = new SqlConnection(connectionString))
                {
                    db.BulkInsert(measurements);
                }

                var response = new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
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