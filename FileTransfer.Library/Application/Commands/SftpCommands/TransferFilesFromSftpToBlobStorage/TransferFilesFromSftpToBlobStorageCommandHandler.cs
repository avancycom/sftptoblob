using FileTransfer.Library.Application.Commands.BlobStorageCommands.UploadBlob;
using FileTransfer.Library.Common.Helpers;
using FileTransfer.Library.Common.Models;
using FileTransfer.Library.Common.Settings.SftpServerSettings;
using FluentFTP;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace FileTransfer.Library.Application.Commands.SftpCommands.TransferFilesFromSftpToBlobStorage;

internal sealed class TransferFilesFromSftpToBlobStorageCommandHandler : IRequestHandler<TransferFilesFromSftpToBlobStorageCommand>
{
    private readonly ISender _mediator;
    private readonly IOptions<SftpServerSettings> _sftpServerSettings;
    private readonly ILogger<TransferFilesFromSftpToBlobStorageCommandHandler> _logger;

    public TransferFilesFromSftpToBlobStorageCommandHandler(
        ISender mediator,
        IOptions<SftpServerSettings> sftpServerSettings,
        ILogger<TransferFilesFromSftpToBlobStorageCommandHandler> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _sftpServerSettings = sftpServerSettings ?? throw new ArgumentNullException(nameof(sftpServerSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        try
        {
            var connection = new ConnectionInfo(
                _sftpServerSettings.Value.Host,
                _sftpServerSettings.Value.Port,
                _sftpServerSettings.Value.Username,
                new PasswordAuthenticationMethod(_sftpServerSettings.Value.Username, _sftpServerSettings.Value.Password));

            using SftpClient sftpClient = new(connection);
            sftpClient.Connect();

            if (!sftpClient.IsConnected)
            {
                _logger.LogError("Failed to establish SFTP connection to '{server}' server.", _sftpServerSettings.Value.Host);
                return;
            }

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
        catch (Exception ex)
        {
            IpInfo ipInfo = await LocationHelper.GetLocationInfoAsync();
            _logger.LogError("Exception: {message}. FTP Server IP: {serverIp}, User IP: {userIp}, User Region: {userRegion}", ex.Message, _sftpServerSettings.Value.Host, ipInfo.Ip, ipInfo.Region);
            throw;
        }
    }

    private async Task FtpHandler(CancellationToken cancellationToken)
    {
        try
        {
            using FtpClient ftpClient = new(
                _sftpServerSettings.Value.Host,
                _sftpServerSettings.Value.Username,
                _sftpServerSettings.Value.Password,
                _sftpServerSettings.Value.Port);

            ftpClient.Connect();

            if (!ftpClient.IsConnected)
            {
                _logger.LogError("Failed to establish FTP connection to '{server}' server.", _sftpServerSettings.Value.Host);
                return;
            }

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
        catch (Exception ex)
        {
            IpInfo ipInfo = await LocationHelper.GetLocationInfoAsync();
            _logger.LogError("Exception: {message}. FTP Server IP: {serverIp}, User IP: {userIp}, User Region: {userRegion}", ex.Message, _sftpServerSettings.Value.Host, ipInfo.Ip, ipInfo.Region);
            throw;
        }
    }
}