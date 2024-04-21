using Azure.Storage.Blobs;
using FileTransfer.Library.Common.Settings.BlobSettings;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileTransfer.Library.Application.Commands.BlobStorageCommands.UploadBlob;

internal sealed class UploadBlobCommandHandler : IRequestHandler<UploadBlobCommand>
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IOptions<BlobSettings> _blobSettings;
    private readonly ILogger<UploadBlobCommandHandler> _logger;
    public UploadBlobCommandHandler(
        BlobServiceClient blobServiceClient,
        IOptions<BlobSettings> blobSettings,
        ILogger<UploadBlobCommandHandler> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _blobSettings = blobSettings ?? throw new ArgumentNullException(nameof(blobSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UploadBlobCommand request, CancellationToken cancellationToken)
    {
        if (request.Stream is null)
        {

            _logger.LogError("[Upload Blob]: Stream cannot be null. File: '{file}'", request.FileName);
            return;
        }

        if (!request.Stream.CanRead)
        {
            _logger.LogError("[Upload Blob]: Stream must be readable. File: '{file}'", request.FileName);
            return;
        }

        if (!request.Stream.CanSeek)
        {
            _logger.LogError("[Upload Blob]: Stream must be seekable. File: '{file}'", request.FileName);
            return;
        }

        if (request.Stream.Position is not 0)
            request.Stream.Seek(0, SeekOrigin.Begin);

        var containerClient = _blobServiceClient.GetBlobContainerClient(_blobSettings.Value.Container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClinet = containerClient.GetBlobClient($"{_blobSettings.Value.Directory}/{request.FileName}");
        await blobClinet.UploadAsync(request.Stream, true, cancellationToken);
    }
}