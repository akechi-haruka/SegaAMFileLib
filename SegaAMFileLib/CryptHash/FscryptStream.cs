using Haruka.Arcade.SegaAMFileLib.Misc;
using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

/// <summary>
/// A stream to read from a fscrypt container.
/// </summary>
public class FscryptStream : Stream {
    /// <summary>
    /// The constant size of a "page" inside a fscrypt container.
    /// </summary>
    public const int PAGE_SIZE = 4096;

    private static readonly ILogger LOG = Log.GetOrCreate("AppFsReader");

    private readonly Stream parentStream;
    private readonly byte[] key;
    private readonly byte[] iv;
    private readonly long relativePositionToParentStream;
    private byte[] pageBuffer;
    private int pageBufferPosition;
    private int pageBufferSize;
    private long position;
    private bool writeMode;

    internal FscryptStream(Stream parentStream, long length, byte[] key, byte[] iv) {
        this.parentStream = parentStream;
        this.key = key;
        this.iv = iv;
        relativePositionToParentStream = parentStream.Position;
        Length = length;
        LOG.LogDebug("Created a stream of " + length + " bytes (base stream position = " + relativePositionToParentStream + ")");
        LOG.LogDebug("Encryption key: " + Hex.To(key));
        LOG.LogDebug("Encryption IV: " + Hex.To(iv));
    }

    /// <inheritdoc/>
    public override void Flush() {
        if (writeMode) {
            WriteCurrentBuffer();
            parentStream.Flush();
        }
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) {
        if (LOG.IsEnabled(LogLevel.Trace)) {
            LOG.LogTrace("Read " + Length + " bytes from " + position);
        }

        if (count == 0 || position >= Length) {
            return 0;
        }

        // initialize and fill buffer if this is our first read
        if (pageBuffer == null) {
            pageBuffer = new byte[PAGE_SIZE];
            FillPageBuffer();
        }

        int remaining = count;
        int read = 0;
        do {
            // read from page buffer
            int pageBufferLength = Math.Min(pageBufferSize - pageBufferPosition, remaining);
            Array.Copy(pageBuffer, pageBufferPosition, buffer, offset + read, pageBufferLength);
            remaining -= pageBufferLength;
            pageBufferPosition += pageBufferLength;
            read += pageBufferLength;

            if (remaining <= 0) {
                break;
            }

            if (pageBufferPosition >= pageBufferLength) {
                FillPageBuffer();
            }
        } while (true);

        return read;
    }

    private void FillPageBuffer() {
        // fill page buffer by a page or remaining length
        pageBufferSize = (int)Math.Min(PAGE_SIZE, Length - position);
        pageBufferPosition = 0;
        if (LOG.IsEnabled(LogLevel.Trace)) {
            LOG.LogTrace("Fill page buffer from " + position + " by " + pageBufferSize + " bytes");
        }

        parentStream.ReadExactly(pageBuffer, 0, pageBufferSize);

        // decrypt page buffer
        byte[] pageIv = new byte[16];
        FscryptUtils.CalculatePageIv((ulong)position, iv, ref pageIv);
        if (LOG.IsEnabled(LogLevel.Trace)) {
            LOG.LogTrace("New page IV for " + position + " is " + Hex.To(pageIv));
        }

        pageBuffer = Aes128Cbc.Decrypt(pageBuffer, key, pageIv);

        position += pageBufferSize;
    }

    private void FillPageBufferAfterSeek() {
        long blockStart = position / PAGE_SIZE * PAGE_SIZE;
        int blockOffset = (int)(position - blockStart);
        if (LOG.IsEnabled(LogLevel.Trace)) {
            LOG.LogTrace("Seek to " + position + ", adjust block position to " + blockStart + "(+" + blockOffset + ")");
        }

        position = blockStart;

        // if Seek before first read happened, create buffer
        if (pageBuffer == null) {
            LOG.LogTrace("Seek before buffer existed");
            pageBuffer = new byte[PAGE_SIZE];
        }

        FillPageBuffer();
        pageBufferPosition = blockOffset;
    }

    /// <inheritdoc/>
    public override void Close() {
        Flush();
        parentStream.Close();
        base.Close();
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) {
        if (LOG.IsEnabled(LogLevel.Trace)) {
            LOG.LogTrace("Seek " + offset + " from " + origin);
        }

        if (origin == SeekOrigin.Begin) {
            position = offset;
            parentStream.Seek(offset + relativePositionToParentStream, origin);
        } else {
            position += offset;
            parentStream.Seek(offset, origin);
        }

        FillPageBufferAfterSeek();

        return position;
    }

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <param name="value">Ignored.</param>
    /// <exception cref="NotSupportedException">always</exception>
    public override void SetLength(long value) {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) {
        // if write before first buffer happened, create buffer
        if (pageBuffer == null) {
            LOG.LogTrace("Write before buffer existed");
            writeMode = true;
            pageBuffer = new byte[PAGE_SIZE];
        }

        for (int i = offset; i < count; i += PAGE_SIZE) {
            int bytes = Math.Min(PAGE_SIZE, count - i - offset);
            Array.Copy(buffer, i, pageBuffer, 0, bytes);

            // encrypt page
            byte[] pageIv = new byte[16];
            FscryptUtils.CalculatePageIv((ulong)position, iv, ref pageIv);
            if (LOG.IsEnabled(LogLevel.Trace)) {
                LOG.LogTrace("New page IV for " + position + " is " + Hex.To(pageIv));
            }

            pageBuffer = Aes128Cbc.Encrypt(pageBuffer, key, pageIv);

            pageBufferPosition = 0;
            pageBufferSize = bytes;

            WriteCurrentBuffer();
        }
    }

    private void WriteCurrentBuffer() {
        if (LOG.IsEnabled(LogLevel.Trace)) {
            LOG.LogTrace("Write " + pageBufferSize + " bytes to " + position);
        }

        parentStream.Write(pageBuffer, pageBufferPosition, pageBufferSize);
        position += pageBufferSize;
        pageBufferPosition = 0;
        pageBufferSize = 0;
    }

    /// Returns true
    public override bool CanRead {
        get { return true; }
    }

    /// Returns <see cref="CanSeek"/> of the parent stream.
    public override bool CanSeek {
        get { return parentStream.CanSeek; }
    }

    /// Returns true
    public override bool CanWrite {
        get { return true; }
    }

    /// <inheritdoc/>
    public override long Length { get; }

    /// <inheritdoc/>
    public override long Position {
        get { return position; }
        set {
            if (value >= 0 && value <= Length) {
                if (LOG.IsEnabled(LogLevel.Trace)) {
                    LOG.LogTrace("Set position to " + value);
                }

                parentStream.Position = value + relativePositionToParentStream;
                position = value;
                FillPageBufferAfterSeek();
            } else {
                throw new IndexOutOfRangeException("Stream position invalid: " + value + ", must be between 0 and " + Length);
            }
        }
    }
}