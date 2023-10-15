//
//  SPDX-FileName: ResoniteSteamClient.cs
//  SPDX-FileCopyrightText: Copyright (c) Jarl Gullberg
//  SPDX-License-Identifier: AGPL-3.0-or-later
//

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Crystite.Configuration;
using Crystite.Extensions;
using Microsoft.Extensions.Options;
using Remora.Results;
using SteamKit2;
using SteamKit2.CDN;
using static SteamKit2.SteamApps;
using static SteamKit2.SteamApps.LicenseListCallback;
using static SteamKit2.SteamApps.PICSProductInfoCallback;
using static SteamKit2.SteamClient;
using static SteamKit2.SteamUser;

#pragma warning disable SA1402

namespace Crystite.ResoniteInstallation;

/// <summary>
/// Acts as a Resonite-specialized client for Steam, providing simplified access to various pieces of required data.
/// </summary>
public sealed class ResoniteSteamClient : IAsyncDisposable
{
    private const uint _resoniteAppid = 2519830;

    private readonly SteamClient _steamClient;
    private readonly ILogger<ResoniteSteamClient> _log;
    private readonly HeadlessApplicationConfiguration _config;

    // instantiated sub-dependencies
    private readonly SteamUser _steamUser;
    private readonly SteamApps _steamApps;
    private readonly SteamContent _steamContent;

    [MemberNotNullWhen(true, nameof(_callbackManager))]
    [MemberNotNullWhen(true, nameof(_callbackRunnerTokenSource))]
    [MemberNotNullWhen(true, nameof(_callbackRunner))]
    [MemberNotNullWhen(true, nameof(_reconnectionSubscription))]
    private bool IsConnected { get; set; }

    [MemberNotNullWhen(true, nameof(_clientPool))]
    private bool IsAuthenticated { get; set; }

    private CallbackManager? _callbackManager;
    private CancellationTokenSource? _callbackRunnerTokenSource;
    private Task? _callbackRunner;
    private IDisposable? _reconnectionSubscription;
    private CDNClientPool? _clientPool;

    private IReadOnlyList<License> _licenses = Array.Empty<License>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResoniteSteamClient"/> class.
    /// </summary>
    /// <param name="steamClient">The Steam client.</param>
    /// <param name="log">The logging instance for this type.</param>
    /// <param name="config">The headless configuration.</param>
    public ResoniteSteamClient(SteamClient steamClient, ILogger<ResoniteSteamClient> log, IOptions<HeadlessApplicationConfiguration> config)
    {
        _steamClient = steamClient;
        _log = log;
        _config = config.Value;

        _steamUser = steamClient.GetHandler<SteamUser>() ?? throw new InvalidOperationException();
        _steamApps = steamClient.GetHandler<SteamApps>() ?? throw new InvalidOperationException();
        _steamContent = steamClient.GetHandler<SteamContent>() ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Initializes the client, connecting it and authenticating with Steam.
    /// </summary>
    /// <remarks>This method is idempotent.</remarks>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A value representing the result of the operation.</returns>
    public async Task<Result> InitializeAsync(CancellationToken ct = default)
    {
        if (!this.IsConnected)
        {
            var connect = await ConnectAsync(ct);
            if (!connect.IsSuccess)
            {
                return connect;
            }
        }

        // ReSharper disable once InvertIf
        if (!this.IsAuthenticated)
        {
            var authenticate = await LoginAsync(_config.SteamCredential, _config.SteamPassword, ct);
            if (!authenticate.IsSuccess)
            {
                return authenticate;
            }
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Retrieves the product information for Resonite.
    /// </summary>
    /// <returns>The product information.</returns>
    public async Task<Result<PICSProductInfo>> GetProductInfoAsync()
    {
        if (!this.IsConnected)
        {
            return new InvalidOperationError("The client is not connected.");
        }

        if (!this.IsAuthenticated)
        {
            return new InvalidOperationError("The client is not authenticated.");
        }

        var infoRequest = new PICSRequest(_resoniteAppid);
        var getInfo = await _steamApps.PICSGetProductInfo(infoRequest, null);
        if (getInfo.Failed)
        {
            return new NotFoundError("Failed to retrieve app information for Resonite");
        }

        var info = getInfo.Results?.Single() ?? throw new InvalidOperationException();
        if (!info.Apps.TryGetValue(_resoniteAppid, out var productInfo))
        {
            return new NotFoundError("Failed to retrieve app information for Resonite");
        }

        return productInfo;
    }

    /// <summary>
    /// Retrieves the package information for Resonite; that is, the information associated with a valid license
    /// attached to the current account.
    /// </summary>
    /// <returns>The package information.</returns>
    public async Task<Result<PICSProductInfo?>> GetPackageInfoAsync()
    {
        if (!this.IsConnected)
        {
            return new InvalidOperationError("The client is not connected.");
        }

        if (!this.IsAuthenticated)
        {
            return new InvalidOperationError("The client is not authenticated.");
        }

        var packageRequests = _licenses.Select(l => new PICSRequest(l.PackageID));
        var getInfo = await _steamApps.PICSGetProductInfo(Array.Empty<PICSRequest>(), packageRequests);
        if (getInfo.Failed)
        {
            return new NotFoundError("Failed to retrieve license package information");
        }

        var resonitePackage = getInfo.Results?.Select
        (
            r => r.Packages.Values.SingleOrDefault
            (
                v => v.KeyValues["appids"].Children.Any(c => c.AsUnsignedInteger() == _resoniteAppid)
            )
        ).SingleOrDefault(r => r is not null);

        return resonitePackage;
    }

    /// <summary>
    /// Gets a free license for Resonite, attaching it to the current account.
    /// </summary>
    /// <returns>A value representing the result of the operation.</returns>
    public async Task<Result> GetFreeLicenseAsync()
    {
        if (!this.IsConnected)
        {
            return new InvalidOperationError("The client is not connected.");
        }

        if (!this.IsAuthenticated)
        {
            return new InvalidOperationError("The client is not authenticated.");
        }

        var getLicense = await _steamApps.RequestFreeLicense(_resoniteAppid);
        if (getLicense.Result is not EResult.OK || !getLicense.GrantedApps.Contains(_resoniteAppid))
        {
            return new InvalidOperationError("Failed to get a free license for Resonite");
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Gets the available depots.
    /// </summary>
    /// <returns>The depots.</returns>
    public async Task<Result<IReadOnlyList<ResoniteDepotInfo>>> GetDepotsAsync()
    {
        if (!this.IsConnected)
        {
            return new InvalidOperationError("The client is not connected.");
        }

        if (!this.IsAuthenticated)
        {
            return new InvalidOperationError("The client is not authenticated.");
        }

        var getPackageInfo = await GetPackageInfoAsync();
        if (!getPackageInfo.IsSuccess)
        {
            return Result<IReadOnlyList<ResoniteDepotInfo>>.FromError(getPackageInfo);
        }

        if (getPackageInfo.Entity is null)
        {
            var requestLicense = await GetFreeLicenseAsync();
            if (!requestLicense.IsSuccess)
            {
                return Result<IReadOnlyList<ResoniteDepotInfo>>.FromError(requestLicense);
            }
        }

        var getProductInfo = await GetProductInfoAsync();
        if (!getProductInfo.IsDefined(out var productInfo))
        {
            return Result<IReadOnlyList<ResoniteDepotInfo>>.FromError(getProductInfo);
        }

        var depots = productInfo.GetDepots(GetCurrentSteamOS(), GetCurrentSteamArch()).ToImmutableArray();
        if (!depots.Any())
        {
            // fall back to x64 Windows depots if there aren't any available for our current system
            depots = productInfo.GetDepots("windows", "64").ToImmutableArray();
        }

        var depotInformation = new List<ResoniteDepotInfo>();
        foreach (var depot in depots)
        {
            var id = uint.Parse(depot.Name!);

            _ = depot.TryGet("manifests", out KeyValue? manifests);
            _ = manifests!.TryGet("public", out KeyValue? publicManifest);
            _ = publicManifest!.TryGet("gid", out ulong? manifestID);

            var getKey = await GetDepotKeyAsync(uint.Parse(depot.Name!));
            if (!getKey.IsDefined(out var key))
            {
                return new InvalidOperationError("Failed to get the decryption key for a depot.");
            }

            depotInformation.Add(new ResoniteDepotInfo(id, manifestID!.Value, key));
        }

        return depotInformation;
    }

    /// <summary>
    /// Gets the manifest for the given depot.
    /// </summary>
    /// <param name="depot">The depot.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The manifest.</returns>
    public async Task<Result<DepotManifest>> GetManifestAsync(ResoniteDepotInfo depot, CancellationToken ct = default)
    {
        if (!this.IsConnected)
        {
            return new InvalidOperationError("The client is not connected.");
        }

        if (!this.IsAuthenticated)
        {
            return new InvalidOperationError("The client is not authenticated.");
        }

        DepotManifest? manifest = null;
        ulong requestCode = 0;
        var requestCodeExpiration = DateTimeOffset.MinValue;

        do
        {
            var now = DateTimeOffset.UtcNow;

            // grab a connection first since it might take a little while
            await using var borrow = await _clientPool.BorrowConnectionAsync(ct);

            try
            {
                if (requestCode is 0 || now >= requestCodeExpiration)
                {
                    requestCode = await _steamContent.GetManifestRequestCode
                    (
                        depot.ID,
                        _resoniteAppid,
                        depot.ManifestID,
                        "public"
                    );

                    if (requestCode is 0)
                    {
                        return new InvalidOperationError("Failed to get manifest request code");
                    }

                    requestCodeExpiration = now.Add(TimeSpan.FromMinutes(5));
                }

                manifest = await _clientPool.CDNClient.DownloadManifestAsync
                (
                    depot.ID,
                    depot.ManifestID,
                    requestCode,
                    borrow.Connection,
                    depot.Key
                );
            }
            catch (TaskCanceledException)
            {
                _log.LogInformation("Connection timeout when downloading depot manifest. Retrying");
            }
            catch (SteamKitWebRequestException e)
            {
                switch (e.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                    {
                        return new InvalidOperationError("Failed to download manifest (Unauthorized).");
                    }
                    case HttpStatusCode.NotFound:
                    {
                        return new NotFoundError("Failed to download manifest (Not Found).");
                    }
                    default:
                    {
                        _log.LogWarning(e, "Failed to download manifest. Retrying with a different connection");
                        borrow.MarkBroken();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogWarning(e, "Failed to download manifest. Retrying with a different connection");
                borrow.MarkBroken();
            }
        }
        while (manifest is null);

        return manifest;
    }

    /// <summary>
    /// Gets the encryption key for the given depot.
    /// </summary>
    /// <param name="depotID">The ID of the depot.</param>
    /// <returns>The encryption key.</returns>
    public async Task<Result<byte[]>> GetDepotKeyAsync(uint depotID)
    {
        if (!this.IsConnected)
        {
            return new InvalidOperationError("The client is not connected.");
        }

        if (!this.IsAuthenticated)
        {
            return new InvalidOperationError("The client is not authenticated.");
        }

        var getKey = await _steamApps.GetDepotDecryptionKey(depotID, _resoniteAppid);
        return getKey.Result is not EResult.OK
            ? new InvalidOperationError("Failed to get depot encryption key")
            : getKey.DepotKey;
    }

    /// <summary>
    /// Downloads a depot chunk from the Steam servers.
    /// </summary>
    /// <param name="depotID">The ID of the depot to download from.</param>
    /// <param name="depotKey">The decryption key of the depot.</param>
    /// <param name="chunk">The chunk to download.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The chunk data.</returns>
    public async Task<Result<DepotChunk>> GetDepotChunkAsync
    (
        uint depotID,
        byte[] depotKey,
        ResoniteManifestFileChunk chunk,
        CancellationToken ct = default
    )
    {
        if (!this.IsConnected)
        {
            return new InvalidOperationError("The client is not connected.");
        }

        if (!this.IsAuthenticated)
        {
            return new InvalidOperationError("The client is not authenticated.");
        }

        var chunkData = new DepotManifest.ChunkData
        {
            ChunkID = chunk.ChunkID,
            Checksum = chunk.Checksum,
            CompressedLength = chunk.CompressedLength,
            Offset = chunk.Offset,
            UncompressedLength = chunk.UncompressedLength
        };

        DepotChunk? depotChunk = null;
        do
        {
            // grab a connection first since it might take a little while
            await using var borrow = await _clientPool.BorrowConnectionAsync(ct);

            try
            {
                depotChunk = await _clientPool.CDNClient.DownloadDepotChunkAsync
                (
                    depotID,
                    chunkData,
                    borrow.Connection,
                    depotKey
                );
            }
            catch (TaskCanceledException)
            {
                _log.LogInformation("Connection timeout when downloading depot chunk. Retrying");
            }
            catch (SteamKitWebRequestException e)
            {
                switch (e.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                    {
                        return new InvalidOperationError("Failed to download chunk (Unauthorized).");
                    }
                    case HttpStatusCode.NotFound:
                    {
                        return new NotFoundError("Failed to download chunk (Not Found).");
                    }
                    default:
                    {
                        _log.LogWarning(e, "Failed to download chunk. Retrying with a different connection");
                        borrow.MarkBroken();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogWarning(e, "Failed to download chunk. Retrying with a different connection");
                borrow.MarkBroken();
            }
        }
        while (depotChunk is null);

        return depotChunk;
    }

    /// <summary>
    /// Gets the contents of a remote file.
    /// </summary>
    /// <param name="depotID">The ID of the depot to download from.</param>
    /// <param name="depotKey">The decryption key of the depot.</param>
    /// <param name="file">The file to download.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The contents of the file.</returns>
    public async Task<Result<byte[]>> GetFileAsync
    (
        uint depotID,
        byte[] depotKey,
        ResoniteManifestFile file,
        CancellationToken ct = default
    )
    {
        await using var ms = new MemoryStream((int)file.Size);
        foreach (var chunk in file.Chunks)
        {
            var getChunk = await GetDepotChunkAsync(depotID, depotKey, chunk, ct);
            if (!getChunk.IsDefined(out var chunkData))
            {
                return Result<byte[]>.FromError(getChunk);
            }

            ms.Seek((long)chunk.Offset, SeekOrigin.Begin);
            await ms.WriteAsync(chunkData.Data, ct);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Connects to the Steam servers, enabling further communication.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A value representing the result of the operation.</returns>
    private async Task<Result> ConnectAsync(CancellationToken ct = default)
    {
        if (this.IsConnected)
        {
            return new InvalidOperationError("The client is already connected.");
        }

        _callbackManager = new CallbackManager(_steamClient);
        _callbackRunnerTokenSource = new();
        _callbackRunner = Task.Run
        (
            () =>
            {
                while (!_callbackRunnerTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        _callbackManager.RunWaitCallbacks();
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, "Steam callback failed");
                    }
                }
            },
            ct
        );

        void OnDisconnected(DisconnectedCallback disconnectedCallback)
        {
            if (_callbackRunnerTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (disconnectedCallback.UserInitiated)
            {
                return;
            }

            _log.LogInformation("Connection lost; reconnecting to Steam");
            _steamClient.Connect();
        }

        var successfullyConnected = new TaskCompletionSource();
        using var connected = _callbackManager.Subscribe<ConnectedCallback>(_ => successfullyConnected.TrySetResult());

        _reconnectionSubscription = _callbackManager.Subscribe<DisconnectedCallback>(OnDisconnected);

        _log.LogInformation("Connecting to Steam");
        _steamClient.Connect();

        await successfullyConnected.Task;

        _log.LogInformation("Successfully connected");
        this.IsConnected = true;
        return Result.FromSuccess();
    }

    /// <summary>
    /// Authenticates with the Steam servers using the provided credentials.
    /// </summary>
    /// <param name="username">The username to authenticate with.</param>
    /// <param name="password">The password to authenticate with.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A value representing the result of the operation.</returns>
    private async Task<Result> LoginAsync(string? username, string? password, CancellationToken ct = default)
    {
        if (!this.IsConnected)
        {
            return new InvalidOperationError("The client is not connected.");
        }

        if (this.IsAuthenticated)
        {
            return new InvalidOperationError("The client is already logged in.");
        }

        if (username is null ^ password is null)
        {
            return new ArgumentInvalidError
            (
                username is null ? nameof(username) : nameof(password),
                "Both username and password must be provided"
            );
        }

        var loggedIn = new TaskCompletionSource<LoggedOnCallback>();
        ct.Register(loggedIn.SetCanceled);

        var licensesReceived = new TaskCompletionSource<LicenseListCallback>();
        ct.Register(licensesReceived.SetCanceled);

        using var login = _callbackManager.Subscribe<LoggedOnCallback>(c => loggedIn.SetResult(c));
        using var licenses = _callbackManager.Subscribe<LicenseListCallback>(c => licensesReceived.SetResult(c));

        if (username is null)
        {
            _log.LogInformation("Logging anonymously into Steam");
            _steamUser.LogOnAnonymous();
        }
        else
        {
            var loginDetails = new LogOnDetails
            {
                Username = username,
                Password = password,
                LoginID = (uint)Environment.ProcessId
            };

            _log.LogInformation("Authenticating with Steam");
            _steamUser.LogOn(loginDetails);
        }

        var logonResult = await loggedIn.Task;
        if (logonResult.Result is not EResult.OK)
        {
            return new InvalidOperationError($"Failed to log into steam: {logonResult.Result}");
        }

        var licensesResult = await licensesReceived.Task;
        if (licensesResult.Result is not EResult.OK)
        {
            return new InvalidOperationError($"Failed to get available product licenses: {licensesResult.Result}");
        }

        _log.LogInformation("Successfully authenticated");

        _clientPool = new CDNClientPool(_steamClient);
        var initializePool = await _clientPool.InitializePoolAsync();
        if (!initializePool.IsSuccess)
        {
            return initializePool;
        }

        _licenses = licensesResult.LicenseList;

        this.IsAuthenticated = true;
        return Result.FromSuccess();
    }

    /// <summary>
    /// Disconnects from the Steam servers, preventing further communication.
    /// </summary>
    /// <returns>A value representing the result of the operation.</returns>
    private async Task<Result> DisconnectAsync()
    {
        if (!this.IsConnected)
        {
            return new InvalidOperationError("The client is not connected.");
        }

        _log.LogInformation("Logging out of Steam");

        var disconnect = new TaskCompletionSource<DisconnectedCallback>();
        using var subscription = _callbackManager.Subscribe<DisconnectedCallback>(c => disconnect.TrySetResult(c));

        _steamUser.LogOff();
        _ = await disconnect.Task;

        _log.LogInformation("Disconnecting from Steam");

        _callbackRunnerTokenSource.Cancel();

        try
        {
            await _callbackRunner;
        }
        catch (Exception e)
        {
            _log.LogError(e, "Callback runner failed");
        }

        _reconnectionSubscription.Dispose();
        _reconnectionSubscription = null;

        _callbackRunner = null;
        _callbackRunnerTokenSource = null;
        _callbackManager = null;

        _log.LogInformation("Disconnected from Steam");

        this.IsConnected = false;
        return Result.FromSuccess();
    }

    private static string GetCurrentSteamOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "windows";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macos";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }

        return "unknown";
    }

    private static string GetCurrentSteamArch()
    {
        return Environment.Is64BitOperatingSystem ? "64" : "32";
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _ = await DisconnectAsync();
    }
}

/// <summary>
/// Handles a managed pool of CDN clients used for downloading depots.
/// </summary>
internal class CDNClientPool
{
    private const uint _resoniteAppid = 2519830;

    private readonly SteamClient _steamClient;
    private readonly SteamContent _steamContent;

    private readonly Channel<Server> _connections = Channel.CreateBounded<Server>(new BoundedChannelOptions(8)
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false,
        FullMode = BoundedChannelFullMode.Wait
    });

    private readonly ConcurrentDictionary<int, Server> _borrowedConnections = new();

    /// <summary>
    /// Gets the CDN client managed by the pool.
    /// </summary>
    public Client CDNClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CDNClientPool"/> class.
    /// </summary>
    /// <param name="steamClient">The Steam client.</param>
    public CDNClientPool(SteamClient steamClient)
    {
        _steamClient = steamClient;
        this.CDNClient = new Client(_steamClient);
        _steamContent = _steamClient.GetHandler<SteamContent>() ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Initializes the pool.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Result> InitializePoolAsync()
    {
        var bootstrapServers = await _steamContent.GetServersForSteamPipe(_steamClient.CellID);
        var eligibleServers = bootstrapServers.Where
        (
            server =>
            {
                var isEligibleForApp = server.AllowedAppIds.Length == 0 ||
                                       server.AllowedAppIds.Contains(_resoniteAppid);

                return isEligibleForApp && server.Type is "SteamCache" or "CDN";
            }
        )
        .Take(8)
        .ToImmutableArray();

        if (!eligibleServers.Any())
        {
            return new InvalidOperationError("Failed to get any eligible CDN servers");
        }

        foreach (var server in eligibleServers)
        {
            await _connections.Writer.WriteAsync(server);
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Borrows a connection from the pool.
    /// </summary>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The borrowed connection.</returns>
    public async ValueTask<IBorrowedConnection> BorrowConnectionAsync(CancellationToken ct = default)
    {
        var connection = await _connections.Reader.ReadAsync(ct);
        if (!_borrowedConnections.TryAdd(connection.GetHashCode(), connection))
        {
            throw new InvalidOperationException("Failed to mark the connection as borrowed. Concurrency bug?");
        }

        return new BorrowedConnection(this, connection);
    }

    /// <summary>
    /// Returns a connection to the pool.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An asynchronous task representing the operation.</returns>
    private ValueTask ReturnConnectionAsync(Server connection, CancellationToken ct = default)
    {
        if (!_borrowedConnections.TryRemove(connection.GetHashCode(), out _))
        {
            throw new InvalidOperationException("The connection has not been borrowed from this pool");
        }

        return _connections.Writer.WriteAsync(connection, ct);
    }

    /// <summary>
    /// Returns a broken connection to the pool, replacing it with a new one.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <returns>An asynchronous task representing the operation.</returns>
    private async ValueTask ReturnBrokenConnectionAsync(Server connection)
    {
        if (!_borrowedConnections.TryGetValue(connection.GetHashCode(), out _))
        {
            throw new InvalidOperationException("The connection has not been borrowed from this pool");
        }

        if (!_borrowedConnections.TryRemove(connection.GetHashCode(), out _))
        {
            throw new InvalidOperationException("Failed to mark the connection as returned. Concurrency bug?");
        }

        var bootstrapServers = await _steamContent.GetServersForSteamPipe(_steamClient.CellID);
        var eligibleServer = bootstrapServers.Where
        (
            server =>
            {
                var isEligibleForApp = server.AllowedAppIds.Length == 0 ||
                                       server.AllowedAppIds.Contains(_resoniteAppid);

                return isEligibleForApp && server.Type is "SteamCache" or "CDN";
            }
        ).FirstOrDefault();

        if (eligibleServer is null)
        {
            throw new InvalidOperationException("Server pool exhausted");
        }

        await _connections.Writer.WriteAsync(eligibleServer);
    }

    /// <summary>
    /// Represents a borrowed CDN connection.
    /// </summary>
    private sealed class BorrowedConnection : IBorrowedConnection
    {
        private readonly CDNClientPool _pool;

        private bool _isDisposed;
        private bool _isBroken;

        /// <inheritdoc/>
        public Server Connection { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BorrowedConnection"/> class.
        /// </summary>
        /// <param name="pool">The connection pool.</param>
        /// <param name="connection">The connection.</param>
        public BorrowedConnection(CDNClientPool pool, Server connection)
        {
            _pool = pool;
            this.Connection = connection;
        }

        /// <inheritdoc/>
        public void MarkBroken()
        {
            _isBroken = true;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_isBroken)
            {
                await _pool.ReturnBrokenConnectionAsync(this.Connection);
                return;
            }

            await _pool.ReturnConnectionAsync(this.Connection);
        }
    }
}

/// <summary>
/// Defines the public API of a borrowed CDN connection.
/// </summary>
public interface IBorrowedConnection : IAsyncDisposable
{
    /// <summary>
    /// Gets the borrowed connection.
    /// </summary>
    Server Connection { get; }

    /// <summary>
    /// Marks the connection as broken.
    /// </summary>
    void MarkBroken();
}
