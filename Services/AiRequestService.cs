using OpenAI;
using System.ClientModel;
using OpenAI.Chat;
using RestSharp;
using System.Text.Json;
using Seiun.Entities;
using Seiun.Models.Responses;
using SixLabors.ImageSharp;
using Seiun.Utils.Enums;
using System.Runtime.CompilerServices;
using System.Text;

namespace Seiun.Services;

public class AiRequestService(
	IServiceScopeFactory serviceScopeFactory,
	ILogger<AiRequestService> logger,
	ICurrentGenerateTaskService currentGenerateTaskService)
	: IAiRequestService
{
	private readonly IConfigurationRoot _secretConfig = new ConfigurationBuilder()
		.SetBasePath(Directory.GetCurrentDirectory())
		.AddJsonFile("secret.json", false, true)
		.Build();

	private readonly IConfigurationRoot _aiConfig = new ConfigurationBuilder()
		.SetBasePath(Directory.GetCurrentDirectory())
		.AddJsonFile("appsettings.json", false, true)
		.Build();

	# region Generate Ai Article

	// 生成文章和封面
	public async Task GenerateAiArticleAsync(Guid userId)
	{
		using var scope = serviceScopeFactory.CreateScope();
		var repository = scope.ServiceProvider.GetRequiredService<IRepositoryService>();

		var articleApiKey = _secretConfig["DeepSeek:ApiKey"] ?? throw new ArgumentException(_secretConfig["DeepSeek:ApiKey"]);
		var articleEndPoint = _aiConfig["GenerateAiArticle:ArticleEndpoint"] ?? throw new ArgumentException(_aiConfig["GenerateAiArticle:ArticleEndpoint"]);
		var articleModel = _aiConfig["GenerateAiArticle:ArticleModel"] ?? throw new ArgumentException(_aiConfig["GenerateAiArticle:ArticleModel"]);
		var imageApiKey = _secretConfig["CA:ApiKey"] ?? throw new ArgumentException(_secretConfig["CA:ApiKey"]);
		var imageEndPoint = _aiConfig["GenerateAiArticle:ImageEndpoint"] ?? throw new ArgumentException(_aiConfig["GenerateAiArticle:ImageEndpoint"]);
		var imageModel = _aiConfig["GenerateAiArticle:ImageModel"] ?? throw new ArgumentException(_aiConfig["GenerateAiArticle:ImageModel"]);
		
		// 获得最新学习单词
		var latestFinishedWordGroup = await repository.FinishedWordRepository.GetLatestFinishedWordIdAsync(userId);
		if (latestFinishedWordGroup == null)
		{
			logger.LogWarning("User {} failed generate ai article", userId);
			return;
		}

		var latestFinishedWordEntities = latestFinishedWordGroup.ToList();
		var latestFinishedWords =
			(await repository.WordRepository.GetByGuidsAsync([.. latestFinishedWordEntities.Select(x => x.WordId)]))
			.ToList();
		var words = latestFinishedWords.Select(x => x.WordText).ToList();
		var random = new Random();
		words = words.OrderBy(_ => random.Next()).ToList();

		// 生成文章
		var prompt = string.Join("|", words);
		var clientOptions = new OpenAIClientOptions
		{
			Endpoint = new Uri(articleEndPoint)
		};
		var clientCredentials = new ApiKeyCredential($"{articleApiKey}");
		var client = new OpenAIClient(clientCredentials, clientOptions).GetChatClient(articleModel);
		const string systemPrompt = """
		                            请根据我提供的使用 | 分隔的英文单词，不区分大小写，生成一篇英文文章，帮助学习这些单词。
		                            title,description,content,tag,vocabulary; content 必须使用 Markdown 语法文本, vocabulary 必须使用形如例子的 Markdown 语法文本, 其他部分以纯文本返回，tag为单个不超过50个字母的单词。
		                            文章中也可以使用一些学习的单词的一些词性变换和语法词组，学习的单词和相关语法,词性变换，词组加粗。

		                            EXAMPLE INPUT:
		                            adventure|challenge|journey|explore|courage

		                            EXAMPLE JSON OUTPUT:
		                            {
		                                "title": "The Thrilling Adventure of a Lifetime", 
		                                "description": "An engaging story about a traveler's adventurous journey, using key vocabulary in a natural context.",
		                                "content": "Once upon a time, a young traveler decided to **explore** the mysterious lands beyond his village. He knew that the **journey** ahead would be full of **challenges**, but his **courage** pushed him forward...\n",
		                                "tag": "Adventure",
		                                "vocabulary": "**adventure**: An exciting or unusual experience, often involving risk and exploration. It can refer to a daring journey or an exciting event.\n\n**challenge**: A difficult task or problem that requires effort and determination to overcome. It can also mean calling someone to a competition or dispute.\n\n"
		                            }
		                            """;
		var userPrompt = $"{prompt}";
		var completionOptions = new ChatCompletionOptions
		{
			Temperature = 1.5f,
			ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
		};
		var messages = new ChatMessage[]
		{
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(userPrompt)
		};
		ChatCompletion completion = await client.CompleteChatAsync(messages, completionOptions);
		var aiArticleJson = completion.Content[0].Text;
		var aiArticle = JsonSerializer.Deserialize<MatchAiArticle>(aiArticleJson);
		if (aiArticle == null)
		{
			logger.LogWarning("User {} failed generate ai article", userId);
			return;
		}

		// 生成封面
		var aiArticleToRequest = aiArticle.Content.Length > 900 ? aiArticle.Content[..900] : aiArticle.Content;

		var coverClient = new RestClient(imageEndPoint);
		var coverRequest = new RestRequest
		{
			Method = Method.Post
		};
		coverRequest.AddHeader("Authorization", $"Bearer {imageApiKey}");
		coverRequest.AddHeader("Content-Type", "application/json");

		var coverBody = new
		{
			prompt = $"Generate an image based on the following English article: {aiArticleToRequest}",
			n = 1,
			model = imageModel,
			size = "1024x1024"
		};

		coverRequest.AddJsonBody(coverBody);

		var coverResponse = await coverClient.ExecuteAsync<MatchAiArticleCover>(coverRequest);
		if (!coverResponse.IsSuccessful || coverResponse.Data == null)
		{
			logger.LogWarning("User {} failed generate ai cover", userId);
			return;
		}

		var arCoverUrl = coverResponse.Data.Data[0].Url;

		// 下载图片
		var imageClient = new RestClient(arCoverUrl);
		var imageRequest = new RestRequest
		{
			Method = Method.Get
		};
		var imageResponse = await imageClient.ExecuteAsync(imageRequest);
		if (!imageResponse.IsSuccessful || imageResponse.RawBytes == null)
		{
			logger.LogWarning("User {} failed upload cover image", userId);
			return;
		}

		var imageBytes = imageResponse.RawBytes;

		// 处理图片
		string articleImgName;
		try
		{
			await using var imageStream = new MemoryStream(imageBytes);
			var image = await Image.LoadAsync(imageStream);
			await using var processedImageStream = new MemoryStream();
			await image.SaveAsWebpAsync(processedImageStream);
			processedImageStream.Seek(0, SeekOrigin.Begin);
			articleImgName =
				await repository.ArticleRepository.UploadArticleImgAsync(processedImageStream);
		}
		catch
		{
			logger.LogWarning("User {} failed handle cover image", userId);
			return;
		}

		// 存储ai文章
		var aIArticleEntity = new AiArticleEntity
		{
			UserId = userId,
			Title = aiArticle.Title,
			Description = aiArticle.Description,
			Content = aiArticle.Content,
			Vocabulary = aiArticle.Vocabulary,
			SessionId = latestFinishedWordGroup.Key,
			CoverFileName = articleImgName,
			Tag = aiArticle.Tag
		};
		repository.AiArticleRepository.Create(aIArticleEntity);
		if (!await repository.AiArticleRepository.SaveAsync())
			logger.LogWarning("User {} failed generate ai article", userId);

		currentGenerateTaskService.DeleteUserId(userId, TaskType.AiArticle);
	}

	# endregion

	# region Generate Ai Fill In Blank

	// 生成选词填空
	public async Task GenerateAiFillInBlankAsync(List<string> words, Guid userId, Guid sessionId)
	{
		using var scope = serviceScopeFactory.CreateScope();
		var repository = scope.ServiceProvider.GetRequiredService<IRepositoryService>();

		var apiKey = _secretConfig["CA:ApiKey"] ?? throw new ArgumentException(_secretConfig["CA:ApiKey"]);
		var endPoint = _aiConfig["GenerateAiFillInBlankAsync:EndPoint"] ?? throw new ArgumentException(_aiConfig["GenerateAiFillInBlankAsync:EndPoint"]);
		var model = _aiConfig["GenerateAiFillInBlankAsync:Model"] ?? throw new ArgumentException(_aiConfig["GenerateAiFillInBlankAsync:Model"]);

		var wordText = string.Join("|", words.Take(15));

		var clientOptions = new OpenAIClientOptions
		{
			Endpoint = new Uri(endPoint)
		};
		var clientCredentials = new ApiKeyCredential($"{apiKey}");
		var client = new OpenAIClient(clientCredentials, clientOptions).GetChatClient(model);
		const string systemPrompt = """
		                            我将提供给你几个英语单词，使用｜分隔，请你将这些单词作为考察内容出一篇选词填空题，帮助用户巩固单词记忆。不要出现连续的填空，使用JSON格式回复。
		                            EXAMPLE INPUT: 
		                            abandon|benevolent|courage|diligent|endeavor
		                            EXAMPLE JSON OUTPUT:
		                            {
		                                "type": 2,
		                                "content": "In the pursuit of our dreams, we often face challenges that test our {$1}. Some may choose to {$2}, overwhelmed by difficulties, while others push forward with determination. A {$3} person is not only hardworking but also persistent, ensuring that every effort counts. Throughout history, great leaders have demonstrated {$4} by standing firm in the face of adversity. Their {$5} actions have inspired many to pursue their goals, knowing that success comes from continuous effort and resilience.",
		                                "selections": ["abandon", "benevolent", "courage", "diligent", "endeavor"],
		                                "answers": {
		                                    "1": "courage",
		                                    "2": "abandon",
		                                    "3": "diligent",
		                                    "4": "endeavor",
		                                    "5": "benevolent"
		                                }
		                            }
		                            """;
		var userPrompt = $"{wordText}";
		var completionOptions = new ChatCompletionOptions
		{
			ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
		};
		var messages = new ChatMessage[]
		{
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(userPrompt)
		};
		ChatCompletion completion = await client.CompleteChatAsync(messages, completionOptions);

		var clozeTest = new ChallengeEntity
		{
			UserId = userId,
			SessionId = sessionId,
			Type = ChallengeType.FillInBlank,
			ChallengeJson = completion.Content[0].Text
		};

		repository.ChallengeRepository.Create(clozeTest);
		if (!await repository.ChallengeRepository.SaveAsync())
			logger.LogWarning("User {} failed generate fill in blank test", userId);
	}

	# endregion

	# region Generate Ai Cloze Test

	// 生成完形填空
	public async Task GenerateAiClozeTestAsync(List<string> words, Guid userId, Guid sessionId)
	{
		using var scope = serviceScopeFactory.CreateScope();
		var repository = scope.ServiceProvider.GetRequiredService<IRepositoryService>();

		var apiKey = _secretConfig["CA:ApiKey"] ?? throw new ArgumentException(_secretConfig["CA:ApiKey"]);
		var endPoint = _aiConfig["GenerateAiClozeTest:EndPoint"] ?? throw new ArgumentException(_aiConfig["GenerateAiClozeTest:EndPoint"]);
		var model = _aiConfig["GenerateAiClozeTest:Model"] ?? throw new ArgumentException(_aiConfig["GenerateAiClozeTest:Model"]);

		var random = new Random();
		words = words.OrderBy(_ => random.Next()).ToList();
		var wordText = string.Join('|', words.Take(15));

		var clientOptions = new OpenAIClientOptions
		{
			Endpoint = new Uri(endPoint)
		};
		var clientCredentials = new ApiKeyCredential($"{apiKey}");
		var client = new OpenAIClient(clientCredentials, clientOptions).GetChatClient(model);
		const string systemPrompt = """
		                            我将提供给你几个英语单词，使用｜分隔，请你将这些单词作为考察内容出一篇完型填空题，帮助用户巩固单词记忆。不要出现连续的填空，analysis使用中文。使用JSON格式回复。
		                            EXAMPLE INPUT: 
		                            go|park|cup|quiet|window|enjoyed
		                            EXAMPLE JSON OUTPUT:
		                            {
		                                "type": 1,
		                                "content": "John was very {$1} to visit the {$2} in the {$3} because he had heard so much about it. When he finally arrived, he {$4} to see how beautiful it was. He quickly {$5} his camera and began taking pictures. By the end of the day, he felt {$6} to have experienced such an amazing place.",
		                                "selections": {
		                                    "1": ["excited", "bored", "surprised", "nervous"],
		                                    "2": ["museum", "library", "restaurant", "park"],
		                                    "3": ["morning", "afternoon", "evening", "night"],
		                                    "4": ["laughed", "cried", "jumped", "gasped"],
		                                    "5": ["opened", "closed", "dropped", "grabbed"],
		                                    "6": ["proud", "tired", "excited", "relaxed"]
		                                },
		                                "answers": {
		                                    "1": "excited",
		                                    "2": "museum",
		                                    "3": "morning",
		                                    "4": "gasped",
		                                    "5": "opened",
		                                    "6": "proud"
		                                },
		                                "analysis": {
		                                    "1": "**'excited'**（兴奋的）表达了 John 对即将访问博物馆的积极期待。这个词反映了他对即将到来的旅行充满热情和兴趣。其他选项 **'bored'**（无聊的）、**'surprised'**（惊讶的）和 **'nervous'**（紧张的）都不适合，因为它们分别描述的是负面的或过度的情绪，无法体现 John 的兴奋情感。",
		                                    "2": "**'museum'**（博物馆）是一个典型的文化学习场所，John 前往该地有很强的文化和探索动机。其他选项如 **'library'**（图书馆）、**'restaurant'**（餐馆）和 **'park'**（公园）虽然也可以是有趣的地方，但都与 John 的目的不符，缺乏文化氛围。",
		                                    "3": "**'morning'**（早晨）通常是一天中充满新鲜感的时间，许多人会选择在清晨参观博物馆，以避免人流高峰。其他选项 **'afternoon'**（下午）、**'evening'**（傍晚）和 **'night'**（夜晚）都不如早晨适合进行此类活动，尤其是博物馆通常在早晨开放。",
		                                    "4": "**'gasped'**（倒吸一口气）是对美丽事物的一种自然反应，表示 John 对博物馆的美感震撼。其他选项 **'laughed'**（笑）、**'cried'**（哭泣）和 **'jumped'**（跳）虽然可以表达情感，但都不符合这种静态的震惊反应。",
		                                    "5": "**'opened'**（打开）最符合上下文，John 迅速取出他的相机并开始拍照。其他选项 **'closed'**（关闭）、**'dropped'**（掉落）和 **'grabbed'**（抓住）都不符合这个动作，因为它们描述的是不合适的动作。",
		                                    "6": "**'proud'**（骄傲的）意味着 John 参观了博物馆并从中学到了很多，他为自己能够亲自体验这一过程而感到骄傲。其他选项 **'tired'**（累的）、**'excited'**（激动的）和 **'relaxed'**（放松的）虽然也有一定的情感关联，但 **'proud'** 更符合这个积极的情感状态。"
		                                }
		                            }

		                            """;
		var userPrompt = $"{wordText}";
		var completionOptions = new ChatCompletionOptions
		{
			ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
		};
		var messages = new ChatMessage[]
		{
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(userPrompt)
		};
		ChatCompletion completion = await client.CompleteChatAsync(messages, completionOptions);

		var clozeTest = new ChallengeEntity
		{
			UserId = userId,
			SessionId = sessionId,
			Type = ChallengeType.Cloze,
			ChallengeJson = completion.Content[0].Text
		};

		repository.ChallengeRepository.Create(clozeTest);
		if (!await repository.ChallengeRepository.SaveAsync())
			logger.LogWarning("User {} failed generate ai cloze test", userId);

		currentGenerateTaskService.DeleteUserId(userId, TaskType.Challenge);
	}

	# endregion

	#region CorrectAssignment

	public async IAsyncEnumerable<string> CorrectAssignmentAsync(
		byte[] imagBytes,
		string fileExtension,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var apiKey = _secretConfig["CA:ApiKey"] ?? throw new ArgumentException(_secretConfig["CA:ApiKey"]);
		var endPoint = _aiConfig["CorrectAssignment:EndPoint"] ?? throw new ArgumentException(_aiConfig["CorrectAssignment:EndPoint"]);
		var model = _aiConfig["CorrectAssignment:Model"] ?? throw new ArgumentException(_aiConfig["CorrectAssignment:Model"]);
		
		var clientOptions = new OpenAIClientOptions
		{
			Endpoint = new Uri(endPoint)
		};
		var clientCredentials = new ApiKeyCredential($"{apiKey}");
		var client = new OpenAIClient(clientCredentials, clientOptions).GetChatClient(model);

		// string.Concat() 拼接字符串
		// string.AsSpan() 提取字符串
		var mediaType = string.Concat("image/", fileExtension.AsSpan(1));

		const string systemPrompt = """
		                            你是一个老师，需要根据我的需求批改作业。
		                            如果作业有错，以 markdown格式文本 返回题目答案以及总结分析每题详细的错误原因，
		                            如果作业没有错误，直接以 markdown格式文本 返回题目答案。
		                            注意联系整个语句，注意语法。
		                            EXAMPLE Text OUTPUT:
		                            ### 正确答案

		                            ## 错误原因分析

		                            """;

		var messages = new ChatMessage[]
		{
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(
			ChatMessageContentPart.CreateTextPart("请帮我批改一下这个题目"),
			ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imagBytes), mediaType)
			)
		};

		var completionStreaming = client.CompleteChatStreamingAsync(messages).WithCancellation(cancellationToken);

		await foreach (var completionUpdate in completionStreaming)
		{
			if (completionUpdate.ContentUpdate.Count > 0)
			{
				yield return completionUpdate.ContentUpdate[0].Text;
			}
		}
	}

	#endregion

	#region ExtractWordsAsync

	public async Task<MatchExtractWords?> ExtractWordsAsync(StringBuilder correctResult, CancellationToken cancellationToken)
	{
		var apiKey = _secretConfig["CA:ApiKey"] ?? throw new ArgumentException(_secretConfig["CA:ApiKey"]);
		var endPoint = _aiConfig["ExtractWords:EndPoint"] ?? throw new ArgumentException(_aiConfig["ExtractWords:EndPoint"]);
		var model = _aiConfig["ExtractWordsAsync:Model"] ?? throw new ArgumentException(_aiConfig["ExtractWordsAsync:Model"]);
		
		var clientOptions = new OpenAIClientOptions
		{
			Endpoint = new Uri(endPoint)
		};
		var clientCredentials = new ApiKeyCredential($"{apiKey}");
		var client = new OpenAIClient(clientCredentials, clientOptions).GetChatClient(model);
		const string systemPrompt = """
		                            帮我从作业批改信息中提取单词。
		                            to,for,a,an,be,the等量词、介词不要提取。
		                            EXAMPLE JSON OUTPUT:
		                            {
		                                "words":[
		                                    "单词1",
		                                    "单词2“
		                                ]
		                            }
		                            """;

		var messages = new ChatMessage[]
		{
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(
			ChatMessageContentPart.CreateTextPart("请帮我从这个作业批改信息中提取出主要单词，to,for,a,an等量词、介词不要提取。"),
			ChatMessageContentPart.CreateTextPart(correctResult.ToString())
			)
		};

		var completionOptions = new ChatCompletionOptions
		{
			Temperature = 0.2f,
			ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
		};

		ChatCompletion completion = await client.CompleteChatAsync(messages, completionOptions, cancellationToken);

		var wordJson = completion.Content[0].Text;
		var extractedWords = JsonSerializer.Deserialize<MatchExtractWords>(wordJson);
		
		return extractedWords;
	}

	#endregion
}
