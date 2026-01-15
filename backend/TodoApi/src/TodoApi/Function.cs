using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;


// Lambda 関数の JSON 入力を .NET のクラスに変換できるようにするためのアセンブリ属性
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TodoApi;

public class Function
{
    // TodoCreateRequestの型定義
    public record TodoCreateRequest(string title, bool completed);

    // LogsRequestの型定義
    public record LogsRequest(DateTime ts, string type,string text);
    
    // APIGatewayから渡されたrequestを受け取って、HTTPレスポンスを返すハンドラ
    public APIGatewayHttpApiV2ProxyResponse FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,ILambdaContext context)
    {
        // メソッド（GET/POSTを受け取る）
        var method = request?.RequestContext?.Http?.Method;

        // ルーティングを受け取る（/logs or /todos）
        var path = request?.RawPath;

        // CloudWatchにログを表示する
        context.Logger.LogLine($"メソッド'{method}' ルート='{path}'");

        // methodの種類で分岐
        if (method == "POST")
        {
            // 渡されたボディ部分をcloudwatchに表示する
            var body = request.Body;
            context.Logger.LogLine($"body='{request.Body}'");

            // もし、ボディ部分がnullもしくはブランクだったら、コード400を返す
            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest("empty body");
            }

            // もし、パスが/logsだったら
             if (path != null  && path.EndsWith("/logs"))
            {
                
                try
                {
                    LogsRequest? dto;
                    dto = JsonSerializer.Deserialize<LogsRequest>(body);

                    if(dto == null) return BadRequest("invalid json");

                    var created = new { id= 999, dto.ts, dto.type, dto.text };

                    return Ok(created);

                }
                catch(JsonException)
                {
                    return BadRequest("invalid json");
                }
                catch (Exception)
                {
                    return ServerError();
                }


            }
            else if (path == "/todos")
            {
                
                try
                {
                    TodoCreateRequest? dto;
                    dto = JsonSerializer.Deserialize<TodoCreateRequest>(body);

                    if(dto == null) return BadRequest("invalid json");

                    var created = new { id = 999, dto.completed, dto.title };
                    return Ok(created);

                }
                catch(JsonException)
                {
                    return BadRequest("invalid json");
                }
                catch (Exception)
                {
                    return ServerError();
                }

            }
            else
            {
                return  NotFound();

            }
        }

        var todos = new[]
        {
            new { id = 1, title = "sample todo", completed = false },
            new { id = 2, title = "second todo", completed = true },
        };

        return Ok(todos);
    
    }

    private APIGatewayHttpApiV2ProxyResponse Ok(object obj)
    {
        var jsonPost = JsonSerializer.Serialize(obj);

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

    private APIGatewayHttpApiV2ProxyResponse BadRequest(string message)
    {

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 400,
            Body = "{\"error\":\"" + message + "\"}",
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }

    private APIGatewayHttpApiV2ProxyResponse ServerError()
    {
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 500,
            Body = "{\"error\":\"internal error\"}",
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }

    private APIGatewayHttpApiV2ProxyResponse NotFound()
    {
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 404,
            Body = "{\"error\":\"not found\"}",
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}
