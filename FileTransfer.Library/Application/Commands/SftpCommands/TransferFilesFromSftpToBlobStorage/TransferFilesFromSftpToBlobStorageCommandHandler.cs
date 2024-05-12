using FileTransfer.Library.Application.Commands.BlobStorageCommands.UploadBlob;
using FileTransfer.Library.Common.Settings.SftpServerSettings;
using FluentFTP;
using MediatR;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace FileTransfer.Library.Application.Commands.SftpCommands.TransferFilesFromSftpToBlobStorage;

internal sealed class TransferFilesFromSftpToBlobStorageCommandHandler : IRequestHandler<TransferFilesFromSftpToBlobStorageCommand>
{
    private readonly ISender _mediator;
    private readonly IOptions<SftpServerSettings> _sftpServerSettings;

    public TransferFilesFromSftpToBlobStorageCommandHandler(
        ISender mediator,
        IOptions<SftpServerSettings> sftpServerSettings)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _sftpServerSettings = sftpServerSettings ?? throw new ArgumentNullException(nameof(sftpServerSettings));
    }

    public async Task Handle(TransferFilesFromSftpToBlobStorageCommand request, CancellationToken cancellationToken)
    {

        switch (_sftpServerSettings.Value.FileProtocol)
        {
            case "sftp":
                await SftpHandler(cancellationToken);
                break;
            case "ftp":
                await FtpHandler(cancellationToken);
                break;
        }
    }

    private async Task SftpHandler(CancellationToken cancellationToken = default)
    {
        var connection = new ConnectionInfo(
            _sftpServerSettings.Value.Host,
            _sftpServerSettings.Value.Port,
            _sftpServerSettings.Value.Username,
            new PasswordAuthenticationMethod(_sftpServerSettings.Value.Username, _sftpServerSettings.Value.Password));

        using SftpClient sftpClient = new(connection);

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

    private async Task FtpHandler(CancellationToken cancellationToken)
    {
        using FtpClient ftpClient = new(
            _sftpServerSettings.Value.Host,
            _sftpServerSettings.Value.Username,
            _sftpServerSettings.Value.Password,
            _sftpServerSettings.Value.Port);

        ftpClient.Connect();

        var files = ftpClient.GetListing(_sftpServerSettings.Value.Directory);

        foreach (var file in files)
        {
            if (file.Type == FtpObjectType.Directory)
                continue;

            using (Stream remoteStream = ftpClient.OpenRead(file.FullName))
            {
                await _mediator.Send(new UploadBlobCommand(remoteStream, file.Name), cancellationToken);
            }

            ftpClient.DeleteFile(file.FullName);
        }

        ftpClient.Disconnect();

    }
}