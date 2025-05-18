using Seiun.Services;
using Seiun.Utils;
using Seiun.Resources;
using Seiun.Utils.Enums;
using Seiun.Models.Responses;
using Seiun.Models.Parameters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Seiun.Entities;

namespace Seiun.Controllers;

[ApiController]
[Route("api/session")]
public class WordSessionController(
	ILogger<WordSessionController> logger,
	IRepositoryService repository,
	ICurrentStudySessionService currentStudySession,
	IAiRequestService aiRequest,
	ICurrentGenerateTaskService currentGenerateTaskService)
	: ControllerBase
{
	/// <summary>
	/// 开始学习单词会话
	/// </summary>
	/// <returns>会话信息</returns>
	[HttpPost("init", Name = "InitStudy")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[Authorize(Roles =
		$"{nameof(UserRole.User)},{nameof(UserRole.Creator)},{nameof(UserRole.Admin)},{nameof(UserRole.SuperAdmin)}")]
	[ProducesResponseType(typeof(StartStudyResp), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ContinueStudyResp), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(StartStudyResp), StatusCodes.Status403Forbidden)]
	[ProducesResponseType(typeof(StartStudyResp), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(StartStudyResp), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> Init()
	{
		var userId = User.GetUserId();
		if (userId == null)
			return StatusCode(StatusCodes.Status403Forbidden,
			StartStudyResp.Fail(
			StatusCodes.Status403Forbidden,
			ErrorMessages.Controller.Any.InvalidJwtToken
			));

		var selectedPlan = await repository.UserPlansRepository.GetUserPlanAsync(userId.Value);
		if (selectedPlan == null)
			return NotFound(StartStudyResp.Fail(
			StatusCodes.Status404NotFound,
			ErrorMessages.Controller.UserPlan.UserPlanNotFound
			));

		var existingSession = await repository.SessionRepository.GetSessionByUserIdAsync(userId.Value);
		if (existingSession != null)
		{
			var existingQueue = currentStudySession.GetAllWords(existingSession.Id);
			if (existingQueue != null)
				return Ok(ContinueStudyResp.Success(existingSession.Id, existingQueue, existingSession));
		}

		var wordQueue = new Queue<WordEntity>();

		var reviewingWordCount = 0;
		var reviewingWordIds = await repository.WrongWordRepository.GetErrorWordIdsByUserIdAsync(userId.Value);
		if (reviewingWordIds != null && reviewingWordIds.Count != 0)
		{
			reviewingWordCount = reviewingWordIds.Count;
			var reviewingWords =
				(await repository.WordRepository.GetReviewingWordsByGuidsAsync(reviewingWordIds)).ToList();

			foreach (var studyingWord in reviewingWords)
			{
				wordQueue.Enqueue(studyingWord);
			}

			repository.WrongWordRepository.BulkDelete(reviewingWordIds);
			if (!await repository.WrongWordRepository.SaveAsync())
			{
				logger.LogWarning("User {} start study session failed", userId);
				return StatusCode(StatusCodes.Status500InternalServerError,
				StartStudyResp.Fail(
				StatusCodes.Status500InternalServerError,
				ErrorMessages.Controller.WordSession.StartFailed
				));
			}
		}

		var studyWords =
			await repository.WordWordBookRepository.GetUnfinishedWordsByPlanAsync(selectedPlan.WordBookId,
			selectedPlan.DailyPlan,
			userId.Value);
		if ((studyWords == null || studyWords.Count == 0) && reviewingWordCount == 0)
		{
			logger.LogWarning("No studying words found for {}", selectedPlan.WordBookId);
			return NotFound(StartStudyResp.Fail(
			StatusCodes.Status404NotFound,
			ErrorMessages.Controller.WordSession.NotFoundStudyingWords
			));
		}

		var random = new Random();
		studyWords = studyWords?.OrderBy(_ => random.Next()).ToList();

		var studyingWordCount = studyWords?.Count ?? 0;
		if (studyWords != null)
			foreach (var word in studyWords)
			{
				wordQueue.Enqueue(word);
			}

		var session = new WordSessionEntity
		{
			UserId = userId.Value,
			ReviewingCount = reviewingWordCount,
			StudyingCount = studyingWordCount,
			ReviewingWords = reviewingWordIds,
			StudyingWords = studyWords?.Select(x => x.Id).ToList()
		};

		repository.SessionRepository.Create(session);
		var newSessionResult = currentStudySession.AddSession(session.Id, wordQueue);
		if (!newSessionResult || !await repository.SessionRepository.SaveAsync())
		{
			logger.LogWarning("User {} start study session failed", userId);
			return StatusCode(StatusCodes.Status500InternalServerError,
			StartStudyResp.Fail(
			StatusCodes.Status500InternalServerError,
			ErrorMessages.Controller.WordSession.StartFailed
			));
		}

		if (studyWords == null)
			return Ok(StartStudyResp.Success(session.Id, reviewingWordCount, studyingWordCount, wordQueue));

		// // 额外线程开始生成题目
		// if (!currentGenerateTaskService.InsertUserId(userId.Value, TaskType.Challenge))
		// {
		//     return Ok(StartStudyResp.Success(session.Id, reviewingWordCount, studyingWordCount, wordQueue));
		// }

		var words = studyWords.Select(x => x.WordText).ToList();
		// _ = Task.Run(() => aiRequest.GenerateAiFillInBlankAsync(words, userId.Value));
		_ = Task.Run(() => aiRequest.GenerateAiClozeTestAsync(words, userId.Value, session.Id));
		return Ok(StartStudyResp.Success(session.Id, reviewingWordCount, studyingWordCount, wordQueue));
	}

	/// <summary>
	/// 获取下一个单词
	/// </summary>
	/// <param name="sessionId">会话ID</param>
	/// <returns>下一个单词信息</returns>
	[HttpGet("next-word", Name = "GetNextWord")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[Authorize(Roles =
		$"{nameof(UserRole.User)},{nameof(UserRole.Creator)},{nameof(UserRole.Admin)},{nameof(UserRole.SuperAdmin)}")]
	[ProducesResponseType(typeof(GetNextWordResp), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(GetNextWordResp), StatusCodes.Status403Forbidden)]
	[ProducesResponseType(typeof(GetNextWordResp), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(GetNextWordResp), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> GetNextWord([FromQuery] Guid sessionId)
	{
		var userId = User.GetUserId();
		if (userId == null)
			return StatusCode(StatusCodes.Status403Forbidden,
			GetNextWordResp.Fail(
			StatusCodes.Status403Forbidden,
			ErrorMessages.Controller.Any.InvalidJwtToken
			));

		var session = await repository.SessionRepository.GetByIdAsync(sessionId);
		if (session == null || session.UserId != userId.Value)
			return NotFound(GetNextWordResp.Fail(
			StatusCodes.Status404NotFound,
			ErrorMessages.Controller.WordSession.NotFoundSession
			));

		var word = currentStudySession.GetNextWord(sessionId);
		if (word != null)
		{
			var distractorWordIds = word.WordDistractors.Select(d => d.DistractorId).ToList();
			var distractorWords = (await repository.WordRepository.GetByGuidsAsync(distractorWordIds)).ToList();
			if (distractorWords.Count == 0)
			{
				logger.LogWarning("User {} get next word failed", userId);
				return StatusCode(StatusCodes.Status500InternalServerError,
				GetNextWordResp.Fail(
				StatusCodes.Status500InternalServerError,
				ErrorMessages.Controller.WordSession.GetNextWordFailed
				));
			}

			var reviewingWordCount = session.ReviewingWords?.Count ?? 0;
			var studyingWordCount = session.StudyingWords?.Count ?? 0;

			return Ok(GetNextWordResp.Success(word, distractorWords, reviewingWordCount, studyingWordCount));
		}

		// 下一个单词为空，表示会话已经结束

		// 生成AI文章
		if (currentGenerateTaskService.InsertUserId(userId.Value, TaskType.AiArticle)) _ = Task.Run(() => aiRequest.GenerateAiArticleAsync(userId.Value));

		// 打卡
		var lastCheckIn = await repository.UserCheckInRepository.LastCheckInAsync(userId.Value);
		if (lastCheckIn == null || DateTimeOffset.UtcNow.Date != lastCheckIn.CreatedAt.Date)
		{
			var userCheckInEntity = new UserCheckInEntity
			{
				UserId = userId.Value
			};

			repository.UserCheckInRepository.Create(userCheckInEntity);
			if (!await repository.UserCheckInRepository.SaveAsync())
			{
				logger.LogWarning("User {} failed check in", userId);
				return StatusCode(StatusCodes.Status500InternalServerError,
				GetNextWordResp.Fail(
				StatusCodes.Status500InternalServerError,
				ErrorMessages.Controller.User.UserCheckInFailed
				));
			}
		}

		// 删除会话
		currentStudySession.RemoveSession(session.Id);

		// 返回会话结束信息
		return Ok(ResponseFactory.NewSuccessBaseResponse(SuccessMessages.Controller.WordSession.WordSessionOver));
	}

	/// <summary>
	/// 提交单词结果
	/// </summary>
	/// <param name="wordResultDto"></param>
	/// <returns>操作结果</returns>
	[HttpPost("correct", Name = "Correct")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[Authorize(Roles =
		$"{nameof(UserRole.User)},{nameof(UserRole.Creator)},{nameof(UserRole.Admin)},{nameof(UserRole.SuperAdmin)}")]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status403Forbidden)]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> Correct([FromBody] WordResultDto wordResultDto)
	{
		var userId = User.GetUserId();
		if (userId == null)
			return StatusCode(StatusCodes.Status403Forbidden,
			ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status403Forbidden,
			ErrorMessages.Controller.Any.InvalidJwtToken
			));

		var session = await repository.SessionRepository.GetByIdAsync(wordResultDto.SessionId);
		if (session == null || session.UserId != userId.Value)
			return StatusCode(StatusCodes.Status404NotFound,
			ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status404NotFound,
			ErrorMessages.Controller.WordSession.NotFoundSession
			));

		if (session.ReviewingWords != null && session.ReviewingWords.Contains(wordResultDto.WordId))
			session.ReviewingWords.Remove(wordResultDto.WordId);

		if (session.StudyingWords != null && session.StudyingWords.Contains(wordResultDto.WordId))
		{
			session.StudyingWords.Remove(wordResultDto.WordId);
			var finishedRecord = new FinishedWordRecordEntity
			{
				UserId = userId.Value,
				SessionId = wordResultDto.SessionId,
				WordId = wordResultDto.WordId
			};
			repository.FinishedWordRepository.Create(finishedRecord);
			if (!await repository.FinishedWordRepository.SaveAsync())
			{
				logger.LogWarning("User {} failed finish word", userId);
				return StatusCode(StatusCodes.Status500InternalServerError,
				ResponseFactory.NewFailedBaseResponse(
				StatusCodes.Status500InternalServerError,
				ErrorMessages.Controller.Word.FinishedWordCreatFailed
				));
			}
		}

		repository.SessionRepository.Update(session);
		if (!await repository.SessionRepository.SaveAsync())
		{
			logger.LogWarning("User {} failed finish word", userId);
			return StatusCode(StatusCodes.Status500InternalServerError,
			ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status500InternalServerError,
			ErrorMessages.Controller.Word.FinishedWordCreatFailed
			));
		}

		currentStudySession.DeleteCorrectWord(session.Id);
		return Ok(ResponseFactory.NewSuccessBaseResponse(SuccessMessages.Controller.Word.FinishedWordCreatSuccess));
	}

	/// <summary>
	/// 提交单词错误
	/// </summary>
	/// <param name="wordResultDto"></param>
	/// <returns>操作结果</returns>
	[HttpPost("wrong", Name = "Wrong")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[Authorize(Roles =
		$"{nameof(UserRole.User)},{nameof(UserRole.Creator)},{nameof(UserRole.Admin)},{nameof(UserRole.SuperAdmin)}")]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status403Forbidden)]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(BaseResp), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> Wrong([FromBody] WordResultDto wordResultDto)
	{
		var userId = User.GetUserId();
		if (userId == null)
			return StatusCode(StatusCodes.Status403Forbidden,
			ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status403Forbidden,
			ErrorMessages.Controller.Any.InvalidJwtToken
			));
		var session = await repository.SessionRepository.GetByIdAsync(wordResultDto.SessionId);
		if (session == null || session.UserId != userId.Value)
			return StatusCode(StatusCodes.Status404NotFound,
			ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status404NotFound,
			ErrorMessages.Controller.WordSession.NotFoundSession
			));

		var mistake = new MistakeBookEntity
		{
			UserId = userId.Value,
			WordId = wordResultDto.WordId
		};
		repository.MistakeBookRepository.Create(mistake);
		if (!await repository.MistakeBookRepository.SaveAsync())
		{
			logger.LogError("User {} wrong word {} failed", userId, wordResultDto.WordId);
			return StatusCode(StatusCodes.Status500InternalServerError,
			ResponseFactory.NewFailedBaseResponse(
			StatusCodes.Status500InternalServerError,
			ErrorMessages.Controller.Word.WrongWordCreatFailed
			));
		}

		var errorRecord = new WrongWordRecordEntity
		{
			UserId = userId.Value,
			SessionId = wordResultDto.SessionId,
			WordId = wordResultDto.WordId
		};
		repository.WrongWordRepository.Create(errorRecord);

		currentStudySession.InsertErrorWord(session.Id);
		if (await repository.WrongWordRepository.SaveAsync())
			return Ok(ResponseFactory.NewSuccessBaseResponse(
			SuccessMessages.Controller.Word.WrongWordRecordCreatSuccess));

		logger.LogError("User {} wrong word {} failed", userId, wordResultDto.WordId);
		return StatusCode(StatusCodes.Status500InternalServerError,
		ResponseFactory.NewFailedBaseResponse(
		StatusCodes.Status500InternalServerError,
		ErrorMessages.Controller.Word.WrongWordCreatFailed
		));
	}
}
