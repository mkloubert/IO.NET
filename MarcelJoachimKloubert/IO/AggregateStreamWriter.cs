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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MarcelJoachimKloubert.IO
{
    /// <summary>
    /// A stream that writes data to a list of streams.
    /// </summary>
    public class AggregateStreamWriter : Stream
    {
        #region Fields (3)

        private readonly bool _OWNS_STREAMS;

        /// <summary>
        /// Stores the underlying streams.
        /// </summary>
        protected readonly ICollection<Stream> _STREAMS;

        private readonly object _SYNC_ROOT;

        #endregion Fields (3)

        #region Constructors (1)

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateStreamWriter" /> class.
        /// </summary>
        /// <param name="ownsStreams">
        /// Underlying stream should also been closed / disposed or not.
        /// </param>
        /// <param name="syncRoot">The custom object that should be used for thread safe opertations.</param>
        public AggregateStreamWriter(bool ownsStreams = false, object syncRoot = null)
        {
            _OWNS_STREAMS = ownsStreams;

            _SYNC_ROOT = syncRoot ?? new object();
            _STREAMS = CreateStreamCollection() ?? new List<Stream>();
        }

        #endregion Constructors (1)

        #region Properties (6)

        /// <inheriteddoc />
        public override bool CanRead
        {
            get { return false; }
        }

        /// <inheriteddoc />
        public override bool CanSeek
        {
            get
            {
                return InvokeForStreams((stream) => stream.CanSeek).Distinct()
                                                                   .Cast<bool?>()
                                                                   .SingleOrDefault() ?? true;
            }
        }

        /// <inheriteddoc />
        public override bool CanWrite
        {
            get
            {
                return InvokeForStreams((stream) => stream.CanWrite).Distinct()
                                                                    .Cast<bool?>()
                                                                    .SingleOrDefault() ?? true;
            }
        }

        /// <inheriteddoc />
        public override long Length
        {
            get
            {
                return InvokeForStreams((stream) => stream.Length).Distinct()
                                                                  .Single();
            }
        }

        /// <inheriteddoc />
        public override long Position
        {
            get
            {
                return InvokeForStreams((stream) => stream.Position).Distinct()
                                                                    .Single();
            }

            set
            {
                InvokeForStreams((stream, state) => stream.Position = state.Value,
                                 new
                                 {
                                     Value = value,
                                 });
            }
        }

        /// <summary>
        /// Gets the object for thread safe operations.
        /// </summary>
        public object SyncRoot
        {
            get { return _SYNC_ROOT; }
        }

        #endregion Properties (6)

        #region Methods (15)

        /// <summary>
        /// Adds a stream.
        /// </summary>
        /// <param name="stream">The stream to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream" /> is <see langword="null" />.
        /// </exception>
        public void AddStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            _STREAMS.Add(stream);
        }

        /// <inheriteddoc />
        public override void Close()
        {
            if (!_OWNS_STREAMS)
            {
                return;
            }

            InvokeForStreams(stream => stream.Close());
        }

        /// <summary>
        /// Creates the collection for storing streams that are used by that object.
        /// </summary>
        /// <returns>The created collection.</returns>
        protected virtual ICollection<Stream> CreateStreamCollection()
        {
            return null;  // default
        }

        /// <inheriteddoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && _OWNS_STREAMS)
            {
                InvokeForStreams((stream) => stream.Dispose());
            }
        }

        /// <inheriteddoc />
        public override void Flush()
        {
            InvokeForStreams(stream => stream.Flush());
        }

        /// <summary>
        /// Invokes an action for all underlying stream.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="AggregateException">
        /// At least one invocation failed.
        /// </exception>
        protected void InvokeForStreams(Action<Stream> action)
        {
            InvokeForStreams(action: (stream, a) => a(stream),
                             actionState: action);
        }

        /// <summary>
        /// Invokes an action for all underlying streams.
        /// </summary>
        /// <typeparam name="T">Type of the second argument for <paramref name="action" />.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="actionState">The second argument for <paramref name="action" />.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="AggregateException">
        /// At least one invocation failed.
        /// </exception>
        protected void InvokeForStreams<T>(Action<Stream, T> action, T actionState)
        {
            InvokeForStreams(action: action,
                             actionStateFactory: (stream) => actionState);
        }

        /// <summary>
        /// Invokes an action for all underlying streams.
        /// </summary>
        /// <typeparam name="T">Type of the second argument for <paramref name="action" />.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="actionStateFactory">The function that returns the second argument for <paramref name="action" />.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action" /> and/or <paramref name="actionStateFactory" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="AggregateException">
        /// At least one invocation failed.
        /// </exception>
        protected void InvokeForStreams<T>(Action<Stream, T> action, Func<Stream, T> actionStateFactory)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (actionStateFactory == null)
            {
                throw new ArgumentNullException("actionStateFactory");
            }

            InvokeForStreams(
                func: (stream, state) =>
                    {
                        state.Action(stream,
                                     state.StateFactory(stream));

                        return (object)null;
                    },
                funcState: new
                    {
                        Action = action,
                        StateFactory = actionStateFactory,
                    });
        }

        /// <summary>
        /// Invokes a function for all underlying streams.
        /// </summary>
        /// <typeparam name="TResult">Type of the results.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <returns>The results of all invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="func" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="AggregateException">
        /// At least one invocation failed.
        /// </exception>
        protected IEnumerable<TResult> InvokeForStreams<TResult>(Func<Stream, TResult> func)
        {
            return InvokeForStreams(func: (stream, f) => f(stream),
                                    funcState: func);
        }

        /// <summary>
        /// Invokes a function for all underlying streams.
        /// </summary>
        /// <typeparam name="T">Type of the second argument for <paramref name="func" />.</typeparam>
        /// <typeparam name="TResult">Type of the results.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <param name="funcState">The second argument for <paramref name="func" />.</param>
        /// <returns>The results of all invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="func" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="AggregateException">
        /// At least one invocation failed.
        /// </exception>
        protected IEnumerable<TResult> InvokeForStreams<T, TResult>(Func<Stream, T, TResult> func, T funcState)
        {
            return InvokeForStreams(func: func,
                                    funcStateFactory: (stream) => funcState);
        }

        /// <summary>
        /// Invokes a function for all underlying streams.
        /// </summary>
        /// <typeparam name="T">Type of the second argument for <paramref name="func" />.</typeparam>
        /// <typeparam name="TResult">Type of the results.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <param name="funcStateFactory">The function that returns the second argument for <paramref name="func" />.</param>
        /// <returns>The results of all invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="func" /> and/or <paramref name="funcStateFactory" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="AggregateException">
        /// At least one invocation failed.
        /// </exception>
        protected virtual IEnumerable<TResult> InvokeForStreams<T, TResult>(Func<Stream, T, TResult> func, Func<Stream, T> funcStateFactory)
        {
            if (func == null)
            {
                throw new ArgumentNullException("func");
            }

            if (funcStateFactory == null)
            {
                throw new ArgumentNullException("funcStateFactory");
            }

            var result = new List<TResult>();

            var exceptions = new List<Exception>();

            try
            {
                using (var e = _STREAMS.GetEnumerator())
                {
                    while (e.MoveNext())
                    {
                        try
                        {
                            var stream = e.Current;

                            result.Add(func(stream,
                                            funcStateFactory(stream)));
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }

            return result;
        }

        /// <inheriteddoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheriteddoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
            {
                throw new NotSupportedException();
            }

            return InvokeForStreams((stream, state) => stream.Seek(state.Offset, state.Origin),
                                    new
                                    {
                                        Offset = offset,
                                        Origin = origin,
                                    }).Distinct()
                                      .Single();
        }

        /// <inheriteddoc />
        public override void SetLength(long value)
        {
            if (!CanSeek)
            {
                throw new NotSupportedException();
            }

            InvokeForStreams((stream, state) => stream.SetLength(state.Value),
                             new
                             {
                                 Value = value,
                             });
        }

        /// <inheriteddoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
            {
                throw new NotSupportedException();
            }

            InvokeForStreams((stream, state) => stream.Write(state.Buffer, state.Offset, state.Count),
                             new
                             {
                                 Buffer = buffer,
                                 Offset = offset,
                                 Count = count,
                             });
        }

        #endregion Methods (15)
    }
}