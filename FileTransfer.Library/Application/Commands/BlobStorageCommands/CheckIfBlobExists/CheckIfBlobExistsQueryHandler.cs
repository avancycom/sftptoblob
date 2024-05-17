using Azure.Storage.Blobs;
using FileTransfer.Library.Common.Settings.BlobSettings;
using MediatR;
using Microsoft.Extensions.Options;

namespace FileTransfer.Library.Application.Commands.BlobStorageCommands.CheckIfBlobExists;

internal sealed class CheckIfBlobExistsQueryHandler : IRequestHandler<CheckIfBlobExistsQuery, bool>
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IOptions<BlobSettings> _blobSettings;
    public CheckIfBlobExistsQueryHandler(
        BlobServiceClient blobServiceClient,
        IOptions<BlobSettings> blobSettings)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _blobSettings = blobSettings ?? throw new ArgumentNullException(nameof(blobSettings));
    }

    public async Task<bool> Handle(CheckIfBlobExistsQuery request, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_blobSettings.Value.Container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = containerClient.GetBlobClient($"{_blobSettings.Value.Directory}/{request.BlobName}");
        var exists = await blobClient.ExistsAsync(cancellationToken);
        return exists.Value;
    }
}
