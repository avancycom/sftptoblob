using FileTransfer.Library.Application.Commands.SftpCommands.TransferFilesFromSftpToBlobStorage;
using MediatR;
using Microsoft.Azure.Functions.Worker;

namespace FileTransfer.FromSFTPToBlobStorage;

public class FromSFTPToBlobStorageFunction(ISender mediator)
{
    private readonly ISender _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    [Function("from-sftp-to-blob-storage")]
    public async Task TransferFilesFromSFTPToBlobStorage(
        [TimerTrigger("%Timer%")] TimerInfo myTimer)
    {
        await _mediator.Send(new TransferFilesFromSftpToBlobStorageCommand());
    }
}
