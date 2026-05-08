using Haruka.Common;
using Microsoft.Extensions.Logging;

namespace Haruka.Arcade.SegaAMFileLib.CryptHash;

public class AppFsStream : Stream {
    private const int PAGE_SIZE = 4096;
    private static readonly ILogger LOG = Log.GetOrCreate("AppFsReader");

    private readonly Stream encryptedStream;
    private readonly byte[] key;
    private readonly byte[] iv;
    private readonly long relativePositionToInnerStream;
    private byte[] pageBuffer;
    private int pageBufferPosition;
    private int pageBufferSize;
    private long position;

    internal AppFsStream(Stream encryptedStream, long length, byte[] key, byte[] iv) {
        this.encryptedStream = encryptedStream;
        this.key = key;
        this.iv = iv;
        relativePositionToInnerStream = encryptedStream.Position;
        Length = length;
        LOG.LogTrace("Created a stream of " + length + " bytes (base stream position = " + relativePositionToInnerStream + ")");
    }

    public override void Flush() {
    }

    public override int Read(byte[] buffer, int offset, int count) {
        LOG.LogTrace("Read " + Length + " bytes from " + position);
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
        LOG.LogTrace("Fill page buffer from " + position + " by " + pageBufferSize + " bytes");

        encryptedStream.ReadExactly(pageBuffer, 0, pageBufferSize);

        // decrypt page buffer
        byte[] pageIv = new byte[16];
        AppFsEncryption.CalculatePageIv((ulong)position, iv, ref pageIv);
        pageBuffer = Aes128Cbc.Decrypt(pageBuffer, key, pageIv);

        position += pageBufferSize;
    }

    private void FillPageBufferAfterSeek() {
        long blockStart = position / PAGE_SIZE * PAGE_SIZE;
        int blockOffset = (int)(position - blockStart);

        LOG.LogTrace("Seek to " + position + ", adjust block position to " + blockStart + "(+" + blockOffset + ")");

        position = blockStart;

        // if Seek before first read happened, create buffer
        if (pageBuffer == null) {
            LOG.LogTrace("Seek before buffer existed");
            pageBuffer = new byte[PAGE_SIZE];
        }

        FillPageBuffer();
        pageBufferPosition = blockOffset;
    }

    public override void Close() {
        encryptedStream.Close();
        base.Close();
    }

    public override long Seek(long offset, SeekOrigin origin) {
        LOG.LogTrace("Seek " + offset + " from " + origin);
        if (origin == SeekOrigin.Begin) {
            position = offset;
            encryptedStream.Seek(offset + relativePositionToInnerStream, origin);
        } else {
            position += offset;
            encryptedStream.Seek(offset, origin);
        }

        FillPageBufferAfterSeek();

        return position;
    }

    public override void SetLength(long value) {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count) {
        throw new NotSupportedException();
    }

    public override bool CanRead {
        get { return true; }
    }

    public override bool CanSeek {
        get { return encryptedStream.CanSeek; }
    }

    public override bool CanWrite {
        get { return false; }
    }

    public override long Length { get; }

    public override long Position {
        get { return position; }
        set {
            if (value >= 0 && value <= Length) {
                LOG.LogTrace("Set position to " + value);
                encryptedStream.Position = value + relativePositionToInnerStream;
                position = value;
                FillPageBufferAfterSeek();
            } else {
                throw new IndexOutOfRangeException("Stream position invalid: " + value + ", must be between 0 and " + Length);
            }
        }
    }
}