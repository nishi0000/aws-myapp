using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;


// Lambda 関数の JSON 入力を .NET のクラスに変換できるようにするためのアセンブリ属性
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TodoApi;

public class Function
{
    // TodoCreateRequestの型定義
    public record TodoCreateRequest(string title, bool completed);
    private string? supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
    private string? supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");

    // LogsRequestの型定義
    public record LogsRequest(DateTime ts, string type,string text);
    
    // APIGatewayから渡されたrequestを受け取って、HTTPレスポンスを返すハンドラ
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,ILambdaContext context)
    {
        // メソッド（GET/POSTを受け取る）
        var method = request?.RequestContext?.Http?.Method;

        // ルーティングを受け取る（/logs or /todos）
        var path = request?.RawPath;

        // CloudWatchにログを表示する
        context.Logger.LogLine($"SUPABASE_URL exists = {(!string.IsNullOrEmpty(supabaseUrl))}");
        context.Logger.LogLine($"SUPABASE_ANON_KEY exists = {(!string.IsNullOrEmpty(supabaseAnonKey))}");

        // AWSでDB用のキーが設定されているか確認する
        if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseAnonKey)) return ServerError();

        // methodの種類で分岐
        if (method == "POST")
        {
            // 渡されたボディ部分をcloudwatchに表示する
            var body = request.Body;

            // もし、ボディ部分がnullもしくはブランクだったら、コード400を返す
            if (string.IsNullOrWhiteSpace(body)) return BadRequest("empty body");
            context.Logger.LogLine($"body='{body.Length}'");


            // もし、パスが/logsだったら
             if (path != null  && path.EndsWith("/logs"))
            {

                try
                {
                    var endpoint = $"{supabaseUrl.TrimEnd('/')}/rest/v1/logs";
                    
                    // POSTで渡された値をC#で扱える形に変換する
                    LogsRequest? dto = JsonSerializer.Deserialize<LogsRequest>(body);

                    // データが変換できなければエラーとする
                    if(dto == null) return BadRequest("invalid json");

                    // 受け取った値をDBに渡す形に変換する
                    var payload = new { dto.ts, dto.type, dto.text };
                    var json = JsonSerializer.Serialize(payload);

                    using var http = new HttpClient();
                    using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);

                    req.Headers.Add("apikey", supabaseAnonKey);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseAnonKey);
                    req.Headers.Add("Prefer", "return=representation");
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    var res = await http.SendAsync(req);
                    var resBody = await res.Content.ReadAsStringAsync();

                    // エラーが出たらログを残してサーバーエラーとする
                    if (!res.IsSuccessStatusCode)
                    {
                        context.Logger.LogLine($"supabase status={(int)res.StatusCode} body={resBody}");
                        return ServerError();
                    }

                    // 成功したら値を返す
                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        StatusCode = 200,
                        Body = resBody,
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                    };


                    // return Ok(resBody);

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
            else if (path != null  && path.EndsWith("/todos"))
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
        else if(method == "GET")
        {            
            // もし、パスが/logsだったら
            if (path != null  && path.EndsWith("/logs"))
            {

               try
                {
                    var endpoint = $"{supabaseUrl.TrimEnd('/')}/rest/v1/logs?select=*&order=ts.desc,id.desc&limit=20";

                    using var http = new HttpClient();
                    using var req = new HttpRequestMessage(HttpMethod.Get, endpoint);

                    req.Headers.Add("apikey", supabaseAnonKey);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseAnonKey);

                    var res = await http.SendAsync(req);
                    var resBody = await res.Content.ReadAsStringAsync();

                                        // エラーが出たらログを残してサーバーエラーとする
                    if (!res.IsSuccessStatusCode)
                    {
                        context.Logger.LogLine($"supabase status={(int)res.StatusCode} body={resBody.Length}");
                        return ServerError();
                    }

                    // 成功したら値を返す
                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        StatusCode = 200,
                        Body = resBody,
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                    };
                    

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
        var body = JsonSerializer.Serialize(obj);

        return new APIGatewayHttpApiV2ProxyResponse
        {
        StatusCode = 200,
        Body = body,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" }
        }
        };
    }

  private APIGatewayHttpApiV2ProxyResponse Error(int statusCode,string message)
    {
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(new {error = message}),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }

        };
    }

    private APIGatewayHttpApiV2ProxyResponse BadRequest(string message) => Error(400,message);

    private APIGatewayHttpApiV2ProxyResponse ServerError() => Error(500,"internal error");

    private APIGatewayHttpApiV2ProxyResponse NotFound() => Error(404,"not found");

}
