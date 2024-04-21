using FileTransfer.Library.Application.Commands.BlobStorageCommands.UploadBlob;
using FileTransfer.Library.Common.Settings.SftpServerSettings;
using MediatR;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace FileTransfer.Library.Application.Commands.SftpCommands.TransferFilesFromSftpToBlobStorage;

internal sealed class TransferFilesFromSftpToBlobStorageCommandHandler : IRequestHandler<TransferFilesFromSftpToBlobStorageCommand>
{
    private readonly ISender _mediator;
    private readonly IOptions<SftpServerSettings> _sftpServerSettings;
    private readonly ConnectionInfo _connectionInfo;

    public TransferFilesFromSftpToBlobStorageCommandHandler(
        ISender mediator,
        IOptions<SftpServerSettings> sftpServerSettings)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _sftpServerSettings = sftpServerSettings ?? throw new ArgumentNullException(nameof(sftpServerSettings));

        _connectionInfo = new ConnectionInfo(
            _sftpServerSettings.Value.Host,
            _sftpServerSettings.Value.Port,
            _sftpServerSettings.Value.Username,
            new PasswordAuthenticationMethod(
            _sftpServerSettings.Value.Username,
            _sftpServerSettings.Value.Password));
    }

    public async Task Handle(TransferFilesFromSftpToBlobStorageCommand request, CancellationToken cancellationToken)
    {
        using SftpClient sftpClient = new(_connectionInfo);

        sftpClient.Connect();

        var files = sftpClient.ListDirectory(_sftpServerSettings.Value.Directory);

        foreach (var file in files)
        {
            if (file.IsDirectory)
                continue;

            using (Stream remoteStream = sftpClient.OpenRead(file.FullName))
            {
                await _mediator.Send(new UploadBlobCommand(remoteStream, file.Name), cancellationToken);
            }

            sftpClient.DeleteFile(file.FullName);
        }

        sftpClient.Disconnect();
    }
}