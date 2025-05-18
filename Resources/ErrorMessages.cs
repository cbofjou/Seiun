namespace Seiun.Resources;

public static class ErrorMessages
{
	public static class ValidationError
	{
		public const string PhoneRequired = "error.validation.phone_number_required";
		public const string PasswordRequired = "error.validation.password_required";
		public const string PublicAnnouncementTitleRequired = "error.validation.public_announcement_title_required";
		public const string PublicAnnouncementContentRequired = "error.validation.public_announcement_content_required";
		public const string ArticleContentRequired = "error.validation.article_content_required";
		public const string ArticleTitleRequired = "error.validation.article_title_required";
		public const string ArticleDescriptionRequired = "error.validation.article_description_required";
		public const string AtLeastOnePropertyRequired = "error.validation.at_least_one_property_is_required";
		public const string UserIdRequired = "error.validation.user_id_required";
		public const string WordBookIdRequired = "error.validation.word_book_id_required";
		public const string DailyPlanRequired = "error.validation.user_plan_set_daily_plan_required";
		public const string CommentIdRequired = "error.validation.comment_id_required";
		public const string ContentRequired = "error.validation.Content_required";
		public const string ArticleIdRequired = "erroe.validation.post_id_required";
		public const string WordIdRequired = "error.validation.word_id_required";
		public const string SessionIdRequired = "error.validation.session_id_required";
		public const string ArticleVocabularyRequired = "error.validation.article_vocabulary_required";

		public const string InvalidPhone = "error.validation.invalid_phone_number";
		public const string InvalidEmail = "error.validation.invalid_email";
		public const string InvalidPassword = "error.validation.invalid_password";
		public const string InvalidUserName = "error.validation.invalid_username";
		public const string InvalidLikeCount = "error.validation.invalid_like_count";
		public const string InvalidDisLikeCount = "error.validation.invalid_dislike_count";

		public const string OverPhoneNumberLength = "error.validation.over_phone_number_length";
		public const string OverNickNameLength = "error.validation.over_nickname_length";
		public const string OverUserNameLength = "error.validation.over_username_length";
		public const string OverDescriptionLength = "error.validation.over_description_length";
		public const string OverEmailLength = "error.validation.over_email_length";
		public const string OverContentLength = "error.validation.over_content_length";
		public const string OverWordDefinitionLength = "error.validation.over_word_definition_length";
		public const string OverWordTextLength = "error.validation.over_word_text_length";
		public const string OverPronunciationLength = "error.validation.over_pronunciation_length";
		public const string OverWordExampleSentenceLength = "error.validation.over_word_example_sentence_length";
		public const string OverArticleContentMaxLength = "error.validation.over_article_content_max_length";
		public const string OverImgFileNameLength = "error.validation.over_img_file_name_length";
		public const string OverPublicAnnouncementLength = "error.validation.over_public_announcement_length";
		public const string OverWordBookNameLength = "error.validation.over_word_book_name_length";
		public const string OverArticleTitleMaxLength = "error.validation.over_article_title_max_length";
		public const string OverArticleDescriptionMaxLength = "error.validation.over_article_description_max_length";
		public const string OverArticleVocabularyMaxLength = "error.validation.over_article_vocabulary_max_length";
		public const string OverArticleTagMaxLength = "error.validation.over_article_tag_max_length";
		public const string OverWordPrimaryDefinitionLength = "error.validation.over_word_primary_definition_length";
	}

	public static class Controller
	{
		public static class Any
		{
			public const string ParamValidFailed = "error.controller.any.param_valid_failed";
			public const string FileNotUploaded = "error.controller.any.file_not_uploaded";
			public const string FileTooLarge = "error.controller.any.file_too_large";
			public const string FileFormatNotSupported = "error.controller.any.file_format_not_supported";
			public const string ImageSizeTooLarge = "error.controller.any.image_size_too_large";
			public const string UnknownFileProcessingError = "error.controller.any.unknown_file_processing_error";
			public const string InvalidJwtToken = "error.controller.any.invalid_jwt_token";
			public const string InvalidReqType = "error.controller.any.invalid_req_type";
		}

		public static class User
		{
			public const string UserNotFound = "error.controller.user.not_found";
			public const string UserLoginFailed = "error.controller.user.login_failed";
			public const string ProfileUpdateFailed = "error.controller.user.profile.update_failed";
			public const string PhoneNumberDuplicated = "error.controller.user.register.phone_number_already_exists";
			public const string RegisterFailed = "error.controller.user.register.register_failed";
			public const string UserCheckInFailed = "error.controller.user.checkin.checkin_failed";
		}

		public static class Admin
		{
			public const string ProfileUpdateFailed = "error.controller.admin.profile.update_failed";
			public const string UserLoginFailed = "error.controller.admin.login_failed";
			public const string AdminNotFound = "error.controller.admin.not_found";
			public const string UserListFailed = "error.controller.admin.list.get_userlist_failed";
			public const string NotAdmin = "error.controller.admin.not_admin";
			public const string GetAllWordsFailed = "error.controller.admin.word.get_all_words_failed";
			public const string WordsNotFound = "error.controller.admin.word.words_not_found";
			public const string ArticlesNotFound = "error.controller.admin.article.articles_not_found";
			public const string GetAllArticlesFailed = "error.controller.admin.article.get_all_articles_failed";
			public const string ChangeRoleFailed = "error.controller.admin.role.change_role_failed";
			public const string GetAllRolesFailed = "error.controller.admin.role.get_all_roles_failed";
			public const string RolesNotFound = "error.controller.admin.role.roles_not_found";
		}

		public static class Article
		{
			public const string PermissonDeniedError = "error.controller.article.permission_denied";
			public const string CreateFailed = "error.controller.article.create.create_failed";
			public const string ArticleNotFound = "error.controller.article.not_found";
			public const string DeleteFailed = "error.controller.article.delete.delete_failed";
			public const string PinFailed = "error.controller.article.pin.pin_failed";
			public const string ArticlePinned = "error.controller.article.pin.article_is_pinned";
			public const string PinCancelFailed = "error.controller.article.cancelpin.pin_cancel_failed";
			public const string ArticleNotPinned = "error.controller.article.cancelpin.article_not_pinned";
			public const string UserIdRequired = "error.controller.article.getarticlelist.user_id_required";
			public const string ArticleListNotFound = "error.controller.article.getarticlelist.article_list_not_found";
			public const string InvalidReqType = "error.controller.article.getarticlelist.invalid_reqtype";
			public const string GetArticleListFailed = "error.controller.article.getarticlelist.get_articlelist_failed";
			public const string LikeFailed = "error.controller.article.like.like_failed";
			public const string ArticleLiked = "error.controller.article.like.article_is_liked";
			public const string ArticleNotLiked = "error.controller.article.cancellike.article_not_liked";
			public const string ArticleImgUploadFailed = "error.controller.article.uploadarticleimage.upload_failed";
			public const string AiArticleNotFound = "error.controller.article.getaiarticle.aiarticle_not_found";
		}

		public static class PublicAnnouncement
		{
			public const string PublishFailed = "error.controller.publicannouncement.publish.publish_failed";
			public const string AnnouncementNotFound = "error.controller.publicannouncement.not_found";
			public const string NotAuthorized = "error.controller.publicannouncement.not_authorized";
			public const string DeleteFailed = "error.controller.publicannouncement.delete.delete_failed";
		}

		public static class Comment
		{
			public const string CreateFailed = "error.controller.comment.create_failed";
			public const string CommentNotFound = "error.controller.comment.not_found";
			public const string CommentDeleteFailed = "error.controller.comment.delete_failed";
			public const string AlreadyLiked = "error.controller.comment.already_liked";
			public const string GetLikeFailed = "error.controller.comment.like_failed";
			public const string AlreadyCancelLiked = "error.controller.comment.already_cancel_liked";
			public const string CancelLikeFailed = "error.controller.comment.cancel_like_failed";
			public const string GetDislikeFailed = "error.controller.comment.get_dislike_failed";
			public const string AlreadyDisliked = "error.controller.comment.already_dislike";
			public const string AlreadyCancelDisliked = "error.controller.comment.already_cancel_dislike";
			public const string CancelDislikeFailed = "error.controller.comment.cancel_dislike_failed";
		}

		public static class Reply
		{
			public const string CreateFailed = "error.controller.reply.create_failed";
			public const string ReplyIdRequired = "error.controller.reply.replyid_required";
			public const string DeleteFailed = "error.controller.reply.delete_failed";
			public const string ParentReplyNotFound = "error.controller.reply.parent_reply_not_found";
			public const string ReplyNotFound = "error.controller.reply.not_found";
		}

		public static class Word
		{
			public const string FinishedWordCreatFailed = "error.controller.word.finishedword.create_failed";
			public const string WrongWordCreatFailed = "error.controller.word.errorword.create_failed";
		}

		public static class WordBook
		{
			public const string GetWordBookListFailed = "error.controller.user_plan.get_word_book_failed";
			public const string SelectWordBookFailed = "error.controller.user_plan.select_word_book_failed";
		}

		public static class UserPlan
		{
			public const string UserPlanNotFound = "error.controller.user_plan.not_found";
			public const string CurrentUserPlanNotFound = "error.controller.user_plan.user_plan_not_found";
			public const string UpdatePlanFailed = "error.controller.user_plan.update_plan_failed";
		}

		public static class WordSession
		{
			public const string StartFailed = "error.controller.session.start_failed";
			public const string NotFoundSession = "error.controller.session.not_found_session";
			public const string GetNextWordFailed = "error.controller.session.get_next_word_failed";
			public const string NotFoundStudyingWords = "error.controller.session.not_found_studying_words";
			public const string WordSessionFailOver = "error.controller.session.word_session_fail_over";
		}

		public static class Challenge
		{
			public const string ChallengeNotFound = "error.controller.challenge.not_found";
			public const string GetChallengeSuccess = "error.controller.question.get_challenge_success";
			public const string GetChallengeFailed = "error.controller.challenge.get_challenge_failed";
		}

		public static class MistakeBook
		{
			public const string MistakeWordNotFound = "error.controller.mistake.not_found";
		}

		public static class TestAnalysis
		{
			public const string CorrectAssignment = "error.controller.correct_assignment_failed";
			public const string ExtractWords = "error.controller.extract_words_failed";
			public const string NotFoundExtractWords = "error.controller.sse.not_found_extract_words";
		}
	}
}
