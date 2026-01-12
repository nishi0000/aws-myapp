using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Collections.Generic;
using System.Text.Json;


// Lambda 関数の JSON 入力を .NET のクラスに変換できるようにするためのアセンブリ属性
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TodoApi;

public class Function
{
    public record TodoCreateRequest(string title, bool completed);
    
    public APIGatewayHttpApiV2ProxyResponse FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,ILambdaContext context)
    {
        

        var method = request?.RequestContext?.Http?.Method;
        context.Logger.LogLine($"メソッド'{method}' リクエストID='{request?.RawPath}'");

        if (method == "POST")
        {
            var body = request.Body;
            context.Logger.LogLine($"body='{request.Body}'");

            if (string.IsNullOrWhiteSpace(body))
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 400,
                    Body = "{\"error\":\"empty body\"}",
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            TodoCreateRequest? dto;

            try
            { 

                dto = JsonSerializer.Deserialize<TodoCreateRequest>(request.Body);
            }
            catch(JsonException)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 400,
                    Body = "{\"error\":\"invalid json\"}",
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
            catch (Exception)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 500,
                    Body = "{\"error\":\"internal error\"}",
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            if(dto == null)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 400,
                    Body = "{\"error\":\"empty body\"}",
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            };



            // まずは固定レスポンスでOK（POSTが通った確認用）
            var created = new { id = 999, title = dto.title, completed = dto.completed };
            var jsonPost = JsonSerializer.Serialize(created);

            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 200,
                Body = jsonPost,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }



        var todos = new[]
        {
            new { id = 1, title = "sample todo", completed = false },
            new { id = 2, title = "second todo", completed = true },
        };

        var json = JsonSerializer.Serialize(todos);

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = json,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            }
        };
    
    }
}
