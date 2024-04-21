using MediatR;

namespace FileTransfer.Library.Application.Commands.SftpCommands.TransferFilesFromSftpToBlobStorage;

public sealed record TransferFilesFromSftpToBlobStorageCommand() : IRequest;