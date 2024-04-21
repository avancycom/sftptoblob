using MediatR;

namespace FileTransfer.Library.Application.Commands.BlobStorageCommands.UploadBlob;

internal sealed record UploadBlobCommand(Stream Stream, string FileName) : IRequest;