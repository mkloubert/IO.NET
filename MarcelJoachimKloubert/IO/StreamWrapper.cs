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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting;
using System.Threading;

namespace MarcelJoachimKloubert.IO
{
    /// <summary>
    /// A wrapper for a <see cref="Stream" />.
    /// </summary>
    /// <typeparam name="TStream">Type of the stream to wrap.</typeparam>
    public class StreamWrapper<TStream> : Stream
        where TStream : global::System.IO.Stream
    {
        #region Fields (2)

        private readonly TStream _BASE_STREAM;
        private readonly object _SYNC_ROOT;

        #endregion Fields (2)

        #region Constructors (1)

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWrapper{TStream}" /> class.
        /// </summary>
        /// <param name="baseStream">The base stream.</param>
        /// <param name="syncRoot">The custom object for thread safe operations.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="baseStream" /> is <see langword="null" />.
        /// </exception>
        public StreamWrapper(TStream baseStream, object syncRoot = null)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException("baseStream");
            }

            _SYNC_ROOT = syncRoot ?? new object();
            _BASE_STREAM = baseStream;
        }

        #endregion Constructors (1)

        #region Properties (9)

        /// <summary>
        /// Gets the wrapped stream.
        /// </summary>
        public TStream BaseStream
        {
            get { return _BASE_STREAM; }
        }

        /// <inheriteddoc />
        public override bool CanRead
        {
            get { return _BASE_STREAM.CanRead; }
        }

        /// <inheriteddoc />
        public override bool CanSeek
        {
            get { return _BASE_STREAM.CanSeek; }
        }

        /// <inheriteddoc />
        public override bool CanTimeout
        {
            get { return _BASE_STREAM.CanTimeout; }
        }

        /// <inheriteddoc />
        public override bool CanWrite
        {
            get { return _BASE_STREAM.CanWrite; }
        }

        /// <inheriteddoc />
        public override long Length
        {
            get { return _BASE_STREAM.Length; }
        }

        /// <inheriteddoc />
        public override long Position
        {
            get { return _BASE_STREAM.Position; }

            set { _BASE_STREAM.Position = value; }
        }

        /// <inheriteddoc />
        public override int ReadTimeout
        {
            get { return _BASE_STREAM.ReadTimeout; }

            set { _BASE_STREAM.ReadTimeout = value; }
        }

        /// <summary>
        /// Gets the object that is used for thread safe operations.
        /// </summary>
        public object SyncRoot
        {
            get { return _SYNC_ROOT; }
        }

        /// <inheriteddoc />
        public override int WriteTimeout
        {
            get { return _BASE_STREAM.WriteTimeout; }

            set { _BASE_STREAM.WriteTimeout = value; }
        }

        #endregion Properties (9)

        #region Methods (23)

        /// <inheriteddoc />
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _BASE_STREAM.BeginRead(buffer, offset, count, callback, state);
        }

        /// <inheriteddoc />
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _BASE_STREAM.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <inheriteddoc />
        public override void Close()
        {
            _BASE_STREAM.Close();
        }

        /// <inheriteddoc />
        public override ObjRef CreateObjRef(Type requestedType)
        {
            return _BASE_STREAM.CreateObjRef(requestedType);
        }

        /// <inheriteddoc />
        [Obsolete]
        protected override WaitHandle CreateWaitHandle()
        {
            return InvokeProtectedMethod((wrapper) => wrapper.CreateWaitHandle());
        }

        /// <inheriteddoc />
        protected override void Dispose(bool disposing)
        {
            InvokeProtectedMethod((wrapper) => wrapper.Dispose(disposing));
        }

        /// <inheriteddoc />
        public override int EndRead(IAsyncResult asyncResult)
        {
            return _BASE_STREAM.EndRead(asyncResult);
        }

        /// <inheriteddoc />
        public override void EndWrite(IAsyncResult asyncResult)
        {
            _BASE_STREAM.EndWrite(asyncResult);
        }

        /// <inheriteddoc />
        public override bool Equals(object obj)
        {
            return _BASE_STREAM.Equals(obj);
        }

        /// <inheriteddoc />
        public override void Flush()
        {
            _BASE_STREAM.Flush();
        }

        /// <summary>
        /// Extracts the arguments values from a sequence of expressions.
        /// </summary>
        /// <param name="args">The arguments as expressions.</param>
        /// <returns>
        /// The extracted values or <see langword="null" /> if <paramref name="args" /> is also <see langword="null" />.
        /// </returns>
        protected static object[] GetArgumentValues(IEnumerable<Expression> args)
        {
            if (args == null)
            {
                return null;
            }

            return args.Select(x =>
                {
                    var argAsObj = Expression.Convert(x, typeof(object));

                    return Expression.Lambda<Func<object>>(argAsObj, null)
                                     .Compile()();
                }).ToArray();
        }

        /// <inheriteddoc />
        public override int GetHashCode()
        {
            return _BASE_STREAM.GetHashCode();
        }

        /// <inheriteddoc />
        public override object InitializeLifetimeService()
        {
            return _BASE_STREAM.InitializeLifetimeService();
        }

        /// <summary>
        /// Invokes a protected method of <see cref="StreamWrapper{TStream}.BaseStream" /> that is also part
        /// of that object.
        /// </summary>
        /// <param name="expr">The expression with the method to call.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expr" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="NullReferenceException">
        /// Body of <paramref name="expr" /> contains NO <see cref="MethodCallExpression" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Method was not found in object of <see cref="StreamWrapper{TStream}.BaseStream" />.
        /// </exception>
        protected void InvokeProtectedMethod(Expression<Action<StreamWrapper<TStream>>> expr)
        {
            if (expr == null)
            {
                throw new ArgumentNullException("expr");
            }

            var methodCall = (MethodCallExpression)expr.Body;
            var method = methodCall.Method;
            var methodParams = method.GetParameters();
            var methodGenericParams = method.GetGenericArguments();

            var methodArgs = GetArgumentValues(methodCall.Arguments);
            if (methodArgs != null &&
                methodArgs.Length < 1)
            {
                methodArgs = null;
            }

            _BASE_STREAM.GetType()
                        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                        .First(x => x.Name == method.Name &&
                                    x.GetGenericArguments().SequenceEqual(methodGenericParams) &&
                                    x.GetParameters().SequenceEqual(methodParams))
                        .Invoke(obj: _BASE_STREAM,
                                parameters: methodArgs);
        }

        /// <summary>
        /// Invokes a protected method of <see cref="StreamWrapper{TStream}.BaseStream" /> that is also part
        /// of that object.
        /// </summary>
        /// <param name="expr">The expression with the method to call.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expr" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="NullReferenceException">
        /// Body of <paramref name="expr" /> contains NO <see cref="MethodCallExpression" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Method was not found in object of <see cref="StreamWrapper{TStream}.BaseStream" />.
        /// </exception>
        protected TResult InvokeProtectedMethod<TResult>(Expression<Func<StreamWrapper<TStream>, TResult>> expr)
        {
            if (expr == null)
            {
                throw new ArgumentNullException("expr");
            }

            var methodCall = (MethodCallExpression)expr.Body;
            var method = methodCall.Method;
            var methodParams = method.GetParameters();
            var methodGenericParams = method.GetGenericArguments();

            var methodArgs = GetArgumentValues(methodCall.Arguments);
            if (methodArgs != null &&
                methodArgs.Length < 1)
            {
                methodArgs = null;
            }

            return (TResult)_BASE_STREAM.GetType()
                                        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                                        .First(x => x.Name == method.Name &&
                                                    x.GetGenericArguments().SequenceEqual(methodGenericParams) &&
                                                    x.GetParameters().SequenceEqual(methodParams))
                                        .Invoke(obj: _BASE_STREAM,
                                                parameters: methodArgs);
        }

        /// <inheriteddoc />
        protected override void ObjectInvariant()
        {
            InvokeProtectedMethod((wrapper) => wrapper.ObjectInvariant());
        }

        /// <inheriteddoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _BASE_STREAM.Read(buffer, offset, count);
        }

        /// <inheriteddoc />
        public override int ReadByte()
        {
            return _BASE_STREAM.ReadByte();
        }

        /// <inheriteddoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _BASE_STREAM.Seek(offset, origin);
        }

        /// <inheriteddoc />
        public override void SetLength(long value)
        {
            _BASE_STREAM.SetLength(value);
        }

        /// <inheriteddoc />
        public override string ToString()
        {
            return _BASE_STREAM.ToString();
        }

        /// <inheriteddoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            _BASE_STREAM.Write(buffer, offset, count);
        }

        /// <inheriteddoc />
        public override void WriteByte(byte value)
        {
            _BASE_STREAM.WriteByte(value);
        }

        #endregion Methods (23)
    }
}