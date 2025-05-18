using Microsoft.IdentityModel.Tokens;

namespace Seiun.Utils;

public static class Constants
{
	public static class Email
	{
		public const int MaxLength = 256;
	}

	public static class User
	{
		public const int MaxDescriptionLength = 256;
		public const int MaxUserNameLength = 16;
		public const int MaxNickNameLength = 16;
		public const int MaxPhoneNumberLength = 15;
		public const int MaxAvatarSize = 8 * 1024 * 1024; // 8MB
		public const int AvatarMaxWidth = 1024;
		public const int AvatarMaxHeight = 1024;
		public const int AvatarStorageSize = 256;
		public static readonly string[] AllowedAvatarExtensions = [".jpg", ".jpeg", ".png", "webp"];
	}

	public static class Token
	{
		public const int TokenExpirationTime = 7 * 24; // 7 days
		public const string SignAlgorithm = SecurityAlgorithms.HmacSha256;
	}

	public static class BucketNames
	{
		public const string Avatar = "avatars";
		public const string ArticleImages = "article-images";
		public const string WordMnemonicImage = "word-mnemonic-image";
		public const string WordAudio = "word-audio";
	}

	public static class Article
	{
		public const int MaxArticleDescriptionLength = 200;
		public const int MaxArticleTitleLength = 200;
		public const int MaxArticleVocabularyLength = 5000;
		public const int MaxArticleImageSize = 8 * 1024 * 1024; // 8MB
		public const int ArticleImageMaxWidth = 3 * 1024;
		public const int ArticleImageMaxHeight = 3 * 1024;
		public static readonly string[] AllowedArticleImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
		public const int MaxArticleContentLength = 5000;
		public const int MaxImgFileNameLength = 1000;
		public const int MaxArticleTagLength = 50;
	}

	public static class PublicAnnotation
	{
		public const int MaxAnnotationLength = 500;
	}

	public static class Word
	{
		public const int MaxWordTextLength = 100;
		public const int MaxWordPronunciationLength = 100;
		public const int MaxWordDefinitionLength = 500;
		public const int MaxWordExampleSentenceLength = 250;
		public const int MaxWordPrimaryDefinitionLength = 250;
	}

	public static class WordBookName
	{
		public const int MaxWordBookNameLength = 50;
	}

	public static class TestAnalysis
	{
		public const int MaxSseImageSize = 3 * 1024 * 1024; // 1MB
		public const int SseImageMaxWidth = 3 * 1024;
		public const int SseImageMaxHeight = 3 * 1024;
	}
}
