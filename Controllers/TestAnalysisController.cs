using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seiun.Models.Responses;
using Seiun.Resources;
using Seiun.Utils.Enums;
using Seiun.Utils;
using Seiun.Services;
using System.Text;
using SixLabors.ImageSharp;

namespace Seiun.Controllers;

[ApiController]
[Route("/api/test-analysis")]
public class TestAnalysisController(
	IAiRequestService aiRequest,
	ILogger<TestAnalysisController> logger,
	IRepositoryService repository)
	: ControllerBase
{
	[HttpPost("correct-assignment", Name = "CorrectAssignment")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[Authorize(Roles = $"{nameof(UserRole.User)},{nameof(UserRole.Creator)},{nameof(UserRole.Admin)},{nameof(UserRole.SuperAdmin)}")]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status403Forbidden)]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status500InternalServerError)]
	public async Task CorrectAssignment(IFormFile? imageFile, CancellationToken cancellationToken = default)
	{
		Response.Headers.Append("Content-Type", "text/event-stream");
		Response.Headers.Append("Cache-Control", "no-cache");
		Response.Headers.Append("Connection", "keep-alive");

		var userId = User.GetUserId();
		if (userId == null)
		{
			var respJson = System.Text.Json.JsonSerializer.Serialize(
			StatusCode(StatusCodes.Status403Forbidden,
			ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status403Forbidden,
			ErrorMessages.Controller.Any.InvalidJwtToken
			)));
			await SseResponse.SseResp(Response, respJson, cancellationToken);
			// 关闭连接
			HttpContext.Abort();
			return;
		}

		if (imageFile == null)
		{
			var respJson = System.Text.Json.JsonSerializer.Serialize(
			BadRequest(ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status400BadRequest,
			ErrorMessages.Controller.Any.FileNotUploaded
			)));
			await SseResponse.SseResp(Response, respJson, cancellationToken);
			HttpContext.Abort();
			return;
		}

		if (imageFile.Length > Constants.TestAnalysis.MaxSseImageSize)
		{
		    var respJson = System.Text.Json.JsonSerializer.Serialize(
		    BadRequest(ResponseFactory.NewFailedBaseResponse(
		    StatusCodes.Status400BadRequest,
		    ErrorMessages.Controller.Any.FileTooLarge
		    )));
		    await SseResponse.SseResp(Response, respJson, cancellationToken);
			HttpContext.Abort();
			return;
		}
		
		var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();
		if (!Constants.Article.AllowedArticleImageExtensions.Contains(fileExtension))
		{
			var respJson = System.Text.Json.JsonSerializer.Serialize(
			BadRequest(ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status400BadRequest,
			ErrorMessages.Controller.Any.FileFormatNotSupported
			)));
			await SseResponse.SseResp(Response, respJson, cancellationToken);
			HttpContext.Abort();
			return;
		}

		await using var imgStream = imageFile.OpenReadStream();
		Image image;
		try
		{
			image = await Image.LoadAsync(imgStream, cancellationToken);
		}
		catch
		{
			var respJson = System.Text.Json.JsonSerializer.Serialize(
			BadRequest(ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status400BadRequest,
			ErrorMessages.Controller.Any.FileFormatNotSupported
			)));
			await SseResponse.SseResp(Response, respJson, cancellationToken);
			HttpContext.Abort();
			return;
		}

		if (image.Width > Constants.TestAnalysis.SseImageMaxWidth ||
		    image.Height > Constants.TestAnalysis.SseImageMaxHeight)
		{
			var respJson = System.Text.Json.JsonSerializer.Serialize(
			BadRequest(ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status400BadRequest,
			ErrorMessages.Controller.Any.ImageSizeTooLarge)));
			await SseResponse.SseResp(Response, respJson, cancellationToken);
			HttpContext.Abort();
			return;
		}

		var correctResult = new StringBuilder();

		try
		{
			await using var processedImageStream = new MemoryStream();
			switch (fileExtension)
			{
				case ".jpg":
				case ".jpeg":
					await image.SaveAsJpegAsync(processedImageStream, cancellationToken);
					break;
				case ".png":
					await image.SaveAsPngAsync(processedImageStream, cancellationToken);
					break;
				case ".webp":
					await image.SaveAsWebpAsync(processedImageStream, cancellationToken);
					break;
			}
			processedImageStream.Seek(0, SeekOrigin.Begin);
			var processedImageBytes = processedImageStream.ToArray();

			// 调用API
			var streamingData = aiRequest.CorrectAssignmentAsync(processedImageBytes, fileExtension, cancellationToken);
			await foreach (var data in streamingData)
			{
				// 检查客户端是否断开连接，断开连接提前结束
				if (cancellationToken.IsCancellationRequested)
				{
					HttpContext.Abort();
					return;
				}

				// 返回批改结果
				await SseResponse.SseResp(Response, data, cancellationToken);
				correctResult.Append(data);
			}
		}
		catch (Exception e)
		{
			logger.LogWarning(e, "User {} fail to correct assignment", userId);
			var respJson = System.Text.Json.JsonSerializer.Serialize(
			StatusCode(StatusCodes.Status500InternalServerError,
			ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status500InternalServerError,
			ErrorMessages.Controller.TestAnalysis.CorrectAssignment)));
			await SseResponse.SseResp(Response, respJson, cancellationToken);
			HttpContext.Abort();
			return;
		}

		// 需复习单词
		try
		{
			// 调用API
			var extractedWords = await aiRequest.ExtractWordsAsync(correctResult, cancellationToken);
			if (extractedWords == null) throw new Exception("User fail to extract words");

			// 查询数据库，是否存在需要复习的单词
			var extractedWordDetails = await repository.WordRepository.GetWordsByWordTextAsync(extractedWords.Words);
			if (extractedWordDetails.Count == 0)
			{
				var notFoundRespJson = System.Text.Json.JsonSerializer.Serialize(
				NotFound(ResponseFactory.NewFailedBaseResponse(
				StatusCodes.Status404NotFound,
				ErrorMessages.Controller.TestAnalysis.NotFoundExtractWords
				)));
				await SseResponse.SseResp(Response, notFoundRespJson, cancellationToken);
				HttpContext.Abort();
				return;
			}
			
			// 返回结果
			var respJson = System.Text.Json.JsonSerializer.Serialize(new ExtractWordDetails()
			{
				WordDetails = extractedWordDetails
			});
			await SseResponse.SseResp(Response, respJson, cancellationToken);
			HttpContext.Abort();
		}
		catch (Exception e)
		{
			logger.LogWarning(e, "User {} fail to extract words", userId);
			var respJson = System.Text.Json.JsonSerializer.Serialize(
			StatusCode(StatusCodes.Status500InternalServerError,
			ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status500InternalServerError,
			ErrorMessages.Controller.TestAnalysis.ExtractWords)));
			await SseResponse.SseResp(Response, respJson, cancellationToken);
			HttpContext.Abort();
		}
	}
}
