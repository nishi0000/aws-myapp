using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Collections.Generic;
using System.Text.Json;


// Lambda 関数の JSON 入力を .NET のクラスに変換できるようにするためのアセンブリ属性
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TodoApi;

public class Function
{
    
public APIGatewayProxyResponse FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,ILambdaContext context)
{

    var method = request?.RequestContext?.Http?.Method;
    context.Logger.LogLine($"Method(v2)='{method}' Path='{request?.RawPath}'");

    if (method == "POST")
    {
        // まずは固定レスポンスでOK（POSTが通った確認用）
        var created = new { id = 999, title = "posted todo", completed = false };
        var jsonPost = JsonSerializer.Serialize(created);

        return new APIGatewayProxyResponse
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

    return new APIGatewayProxyResponse
    {
        StatusCode = 200,
        Body = json,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" }
        }
    };
}}
