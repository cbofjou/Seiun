using System.Text.Json.Serialization;
using Seiun.Entities;

namespace Seiun.Models.Responses;

#region Match Extract Words

public class MatchExtractWords
{
	[JsonPropertyName("words")] public required List<string> Words { get; set; }
}

public class ExtractWordDetails
{
	public required List<WordEntity> WordDetails { get; set; }
}

#endregion
