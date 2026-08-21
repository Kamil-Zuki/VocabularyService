using Grpc.Core;
using Pvs.Media.Grpc;
using VocabularyService.Data.Entities.JsonTypes;
using MediaGrpcClient = Pvs.Media.Grpc.MediaService.MediaServiceClient;

namespace VocabularyService.Services;

public class MediaGrpcClientAdapter : IMediaService
{
    private readonly MediaGrpcClient _client;
    private readonly ILogger<MediaGrpcClientAdapter> _logger;

    public MediaGrpcClientAdapter(
        MediaGrpcClient client,
        ILogger<MediaGrpcClientAdapter> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Guid> UploadImageAsync(Stream data, string contentType, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await data.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);

        var response = await _client.UploadImageAsync(
            new UploadImageRequest
            {
                ImageData = Google.Protobuf.ByteString.CopyFrom(ms.ToArray()),
                ContentType = contentType
            },
            cancellationToken: cancellationToken);

        return Guid.Parse(response.ImageId);
    }

    public async Task<Guid> UploadAudioAsync(Stream data, string contentType, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await data.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);

        var response = await _client.UploadAudioAsync(
            new UploadAudioRequest
            {
                AudioData = Google.Protobuf.ByteString.CopyFrom(ms.ToArray()),
                ContentType = contentType
            },
            cancellationToken: cancellationToken);

        return Guid.Parse(response.AudioId);
    }

    public async Task<string> GetDocumentUrlAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetDocumentUrlAsync(
            new GetDocumentUrlRequest { DocumentId = documentId.ToString() },
            cancellationToken: cancellationToken);
            
        return response.Url;
    }


    public async Task FillCardMediaUrlsAsync(CardMedia? media, CancellationToken cancellationToken = default)
    {
        if (media == null) return;

        if (media.ImageId.HasValue)
        {
            try
            {
                var image = await _client.GetImageUrlAsync(
                    new GetImageUrlRequest { ImageId = media.ImageId.Value.ToString() },
                    cancellationToken: cancellationToken);
                media.ImageUrl = image.Url;
            }
            catch (RpcException ex)
            {
                _logger.LogWarning(ex, "Could not resolve image URL for {ImageId}", media.ImageId);
            }
        }

        if (media.AudioId.HasValue)
        {
            try
            {
                var audio = await _client.GetAudioUrlAsync(
                    new GetAudioUrlRequest { AudioId = media.AudioId.Value.ToString() },
                    cancellationToken: cancellationToken);
                media.AudioUrl = audio.Url;
            }
            catch (RpcException ex)
            {
                _logger.LogWarning(ex, "Could not resolve audio URL for {AudioId}", media.AudioId);
            }
        }
    }
}
