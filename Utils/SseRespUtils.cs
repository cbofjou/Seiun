using System.Text;

namespace Seiun.Utils;

public static class SseResponse
{
	public static async Task SseResp(HttpResponse httpResp, string respString, CancellationToken cancellationToken = default)
	{
		// CancellationToken 监听客户端连接，如果连接断开，则CancellationToken.isCancellationRequested为true
		// 取消发送
		var messageData = $"data: {respString}\n\n";
		var messageBytes = Encoding.UTF8.GetBytes(messageData);
		var buffer = new ReadOnlyMemory<byte>(messageBytes, 0, messageBytes.Length);
		await httpResp.Body.WriteAsync(buffer, cancellationToken);
		await httpResp.Body.FlushAsync(cancellationToken);
		await Task.Delay(200, cancellationToken);
	}
}

