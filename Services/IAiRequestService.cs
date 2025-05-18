using Seiun.Models.Responses;
using System.Text;

namespace Seiun.Services;

public interface IAiRequestService
{
	Task GenerateAiArticleAsync(Guid userId);
	Task GenerateAiFillInBlankAsync(List<string> words, Guid userId, Guid sessionId);
	Task GenerateAiClozeTestAsync(List<string> words, Guid userId, Guid sessionId);
	IAsyncEnumerable<string> CorrectAssignmentAsync(byte[] imagBytes, string fileExtension, CancellationToken cancellationToken);
	Task<MatchExtractWords?> ExtractWordsAsync(StringBuilder correctResult, CancellationToken cancellationToken);
}
