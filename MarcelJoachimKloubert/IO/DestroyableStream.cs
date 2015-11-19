/**********************************************************************************************************************
 * IO.NET (https://github.com/mkloubert/IO.NET)                                                                       *
 *                                                                                                                    *
 * Copyright (c) 2015, Marcel Joachim Kloubert <marcel.kloubert@gmx.net>                                              *
 * All rights reserved.                                                                                               *
 *                                                                                                                    *
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the   *
 * following conditions are met:                                                                                      *
 *                                                                                                                    *
 * 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the          *
 *    following disclaimer.                                                                                           *
 *                                                                                                                    *
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the       *
 *    following disclaimer in the documentation and/or other materials provided with the distribution.                *
 *                                                                                                                    *
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote    *
 *    products derived from this software without specific prior written permission.                                  *
 *                                                                                                                    *
 *                                                                                                                    *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, *
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE  *
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, *
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR    *
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,  *
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE   *
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.                                           *
 *                                                                                                                    *
 **********************************************************************************************************************/

using System;
using System.IO;
using System.Linq;

namespace MarcelJoachimKloubert.IO
{
    #region CLASS: DestroyableStream<TStream>

    /// <summary>
    /// A stream that will be destroyed / shreddered after it has been closed.
    /// </summary>
    /// <typeparam name="TStream">Type of the wrapped stream.</typeparam>
    public class DestroyableStream<TStream> : StreamWrapper<TStream>
        where TStream : global::System.IO.Stream
    {
        #region Fields (3)

        /// <summary>
        /// Stores the block size.
        /// </summary>
        protected readonly int _BLOCK_SIZE;

        /// <summary>
        /// Stores the number of write operations.
        /// </summary>
        protected readonly int _COUNT;

        /// <summary>
        /// Stores if stream should be flushed after each write operation or not.
        /// </summary>
        protected readonly bool _FLUSH_AFTER_WRITE;

        #endregion Fields (3)

        #region Constructors (1)

        /// <summary>
        /// Initializes a new instance of the <see cref="DestroyableStream{TStream}" /> class.
        /// </summary>
        /// <param name="baseStream">The base stream.</param>
        /// <param name="count">The number of write operations.</param>
        /// <param name="blockSize">The blocksize to use.</param>
        /// <param name="flushAfterWrite">Flush after each write operation or not.</param>
        /// <param name="syncRoot">The custom object for thread safe operations.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="baseStream" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count" /> is less than 0 and/or <paramref name="blockSize" /> is less than 1.
        /// </exception>
        public DestroyableStream(TStream baseStream,
                                 int count = 1, int blockSize = 8192, bool flushAfterWrite = true,
                                 object syncRoot = null)
            : base(baseStream: baseStream,
                   syncRoot: syncRoot)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (blockSize < 1)
            {
                throw new ArgumentOutOfRangeException("blockSize");
            }

            _BLOCK_SIZE = blockSize;
            _COUNT = count;
            _FLUSH_AFTER_WRITE = flushAfterWrite;
        }

        #endregion Constructors (1)

        #region Methods (2)
        
        /// <summary>
        /// Destroys the underlying stream.
        /// </summary>
        protected virtual void DestroyMe()
        {
            if (!CanSeek || !CanWrite)
            {
                return;
            }

            var startPosition = 0;
            var len = Length;

            for (var i = 0; i < _COUNT; i++)
            {
                Position = startPosition;

                byte byteToWrite = 0;
                switch (i % 3)
                {
                    case 0:
                        byteToWrite = 255;
                        break;

                    case 2:
                        byteToWrite = 151;
                        break;
                }

                var block = Enumerable.Repeat(byteToWrite, _BLOCK_SIZE)
                                      .ToArray();

                var blockCount = (long)Math.Floor((double)len / (double)block.Length);
                for (long ii = 0; ii < blockCount; ii++)
                {
                    Write(block, 0, block.Length);

                    if (_FLUSH_AFTER_WRITE)
                    {
                        Flush();
                    }
                }

                var lastBlockSize = (int)(len % (long)_BLOCK_SIZE);
                if (lastBlockSize > 0)
                {
                    Write(block, 0, lastBlockSize);

                    if (_FLUSH_AFTER_WRITE)
                    {
                        Flush();
                    }
                }
            }

            if (BaseStream is FileStream)
            {
                var stream = (FileStream)((object)BaseStream);
                var file = new FileInfo(stream.Name);

                if (file.Exists)
                {
                    file.Delete();
                }
            }
        }

        /// <inheriteddoc />
        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);

                DestroyMe();
            }
            catch (Exception)
            {
                if (disposing)
                {
                    throw;
                }
            }
        }

        #endregion Methods (3)
    }

    #endregion CLASS: DestroyableStream<TStream>

    #region CLASS: DestroyableStream

    /// <summary>
    /// A stream that will be destroyed / shreddered after it has been closed.
    /// </summary>
    public class DestroyableStream : DestroyableStream<Stream>
    {
        #region Constructors (1)

        /// <summary>
        /// Initializes a new instance of the <see cref="DestroyableStream" /> class.
        /// </summary>
        /// <param name="baseStream">The base stream.</param>
        /// <param name="count">The number of write operations.</param>
        /// <param name="blockSize">The blocksize to use.</param>
        /// <param name="flushAfterWrite">Flush after each write operation or not.</param>
        /// <param name="syncRoot">The custom object for thread safe operations.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="baseStream" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count" /> is less than 0 and/or <paramref name="blockSize" /> is less than 1.
        /// </exception>
        public DestroyableStream(Stream baseStream,
                                 int count = 1, int blockSize = 8192, bool flushAfterWrite = true,
                                 object syncRoot = null)
            : base(baseStream: baseStream,
                   count: count, blockSize: blockSize, flushAfterWrite: flushAfterWrite, 
                   syncRoot: syncRoot)
        {
        }

        #endregion Constructors (1)
    }

    #endregion CLASS: DestroyableStream
}