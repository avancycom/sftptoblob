using MediatR;

namespace FileTransfer.Library.Application.Commands.BlobStorageCommands.CheckIfBlobExists;

public sealed record CheckIfBlobExistsQuery(string BlobName) : IRequest<bool>;
