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

namespace AlkatronicAddMeasurementLambda
{
    public class AlkatronicAddMeasurementFunction
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

                var measurements = JsonConvert.DeserializeObject<IEnumerable<AlkatronicMeasurements>>(request.Body);

                // save new data to db
                using (var db = new SqlConnection(connectionString))
                {
                    db.BulkInsert(measurements);
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonConvert.SerializeObject(new { message = "OK" }),
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}