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
using System.Text;
using System.Threading.Tasks;

namespace MarcelJoachimKloubert.IO
{
    /// <summary>
    /// Writes to a list of underlying <see cref="TextWriter" /> objects.
    /// </summary>
    public class AggregateTextWriter : TextWriter
    {
        #region Fields (3)

        private readonly bool _OWNS_WRITERS;
        private readonly object _SYNC_ROOT;

        /// <summary>
        /// Stores the underlying writers.
        /// </summary>
        protected readonly ICollection<TextWriter> _WRITERS;

        #endregion Fields (3)

        #region Constructors (1)

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateTextWriter" /> class.
        /// </summary>
        /// <param name="ownsWriters">Also close underlying writers or not.</param>
        /// <param name="syncRoot">The custom object for thread safe operations.</param>
        public AggregateTextWriter(bool ownsWriters = false, object syncRoot = null)
        {
            _SYNC_ROOT = syncRoot ?? new object();
            _OWNS_WRITERS = ownsWriters;
            _WRITERS = CreateWriterStorage() ?? new List<TextWriter>();
        }

        #endregion Constructors (1)

        #region Methods (45)

        /// <summary>
        /// Adds a new writer.
        /// </summary>
        /// <param name="writer">The writer to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer" /> is <see langword="null" />.
        /// </exception>
        public void AddWriter(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            _WRITERS.Add(writer);
        }

        /// <summary>
        /// Returns the collection that stores the underlying writers.
        /// </summary>
        /// <returns>The writer collection.</returns>
        protected virtual ICollection<TextWriter> CreateWriterStorage()
        {
            return null;
        }

        /// <inheriteddoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && _OWNS_WRITERS)
            {
                InvokeForWriters((writer) => writer.Dispose());
            }
        }

        /// <inheriteddoc />
        public override void Flush()
        {
            InvokeForWriters(w => w.Flush());
        }

        /// <summary>
        /// Invokes an action for all underlying writers.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="AggregateException">
        /// At least one invocation failed.
        /// </exception>
        protected void InvokeForWriters(Action<TextWriter> action)
        {
            InvokeForWriters(action: (writer, a) => a(writer),
                             actionState: action);
        }

        /// <summary>
        /// Invokes an action for all underlying writers.
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
        protected void InvokeForWriters<T>(Action<TextWriter, T> action, T actionState)
        {
            InvokeForWriters(action: action,
                             actionStateFactory: (writer) => actionState);
        }

        /// <summary>
        /// Invokes an action for all underlying writers.
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
        protected void InvokeForWriters<T>(Action<TextWriter, T> action, Func<TextWriter, T> actionStateFactory)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (actionStateFactory == null)
            {
                throw new ArgumentNullException("actionStateFactory");
            }

            InvokeForWriters(
                func: (writer, state) =>
                {
                    state.Action(writer,
                                 state.StateFactory(writer));

                    return (object)null;
                },
                funcState: new
                {
                    Action = action,
                    StateFactory = actionStateFactory,
                });
        }

        /// <summary>
        /// Invokes a function for all underlying writers.
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
        protected IEnumerable<TResult> InvokeForWriters<TResult>(Func<TextWriter, TResult> func)
        {
            return InvokeForWriters(func: (writer, f) => f(writer),
                                    funcState: func);
        }

        /// <summary>
        /// Invokes a function for all underlying writers.
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
        protected IEnumerable<TResult> InvokeForWriters<T, TResult>(Func<TextWriter, T, TResult> func, T funcState)
        {
            return InvokeForWriters(func: func,
                                    funcStateFactory: (writer) => funcState);
        }

        /// <summary>
        /// Invokes a function for all underlying writers.
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
        protected virtual IEnumerable<TResult> InvokeForWriters<T, TResult>(Func<TextWriter, T, TResult> func, Func<TextWriter, T> funcStateFactory)
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
                using (var e = _WRITERS.GetEnumerator())
                {
                    while (e.MoveNext())
                    {
                        try
                        {
                            var writer = e.Current;

                            result.Add(func(writer,
                                            funcStateFactory(writer)));
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
        public override void Write(char value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(char[] buffer)
        {
            InvokeForWriters((w, b) => w.Write(b),
                             buffer);
        }

        /// <inheriteddoc />
        public override void Write(char[] buffer, int index, int count)
        {
            InvokeForWriters((w, s) => w.Write(s.Buffer, s.Index, s.Count),
                             new
                             {
                                 Buffer = buffer,
                                 Index = index,
                                 Count = count,
                             });
        }

        /// <inheriteddoc />
        public override void Write(bool value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(int value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(uint value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(long value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(ulong value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(float value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(double value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(decimal value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(string value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(object value)
        {
            InvokeForWriters((w, v) => w.Write(v),
                             value);
        }

        /// <inheriteddoc />
        public override void Write(string format, object arg0)
        {
            InvokeForWriters((w, s) => w.Write(s.Format, s.Argument),
                             new
                             {
                                 Argument = arg0,
                                 Format = format,
                             });
        }

        /// <inheriteddoc />
        public override void Write(string format, object arg0, object arg1)
        {
            InvokeForWriters((w, s) => w.Write(s.Format, s.Argument0, s.Argument1),
                             new
                             {
                                 Argument0 = arg0,
                                 Argument1 = arg1,
                                 Format = format,
                             });
        }

        /// <inheriteddoc />
        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            InvokeForWriters((w, s) => w.Write(s.Format, s.Argument0, s.Argument1, s.Argument2),
                             new
                             {
                                 Argument0 = arg0,
                                 Argument1 = arg1,
                                 Argument2 = arg2,
                                 Format = format,
                             });
        }

        /// <inheriteddoc />
        public override void Write(string format, params object[] arg)
        {
            InvokeForWriters((w, s) => w.Write(s.Format, s.Arguments),
                             new
                             {
                                 Arguments = arg,
                                 Format = format,
                             });
        }
        
        /// <inheriteddoc />
        public override void WriteLine()
        {
            InvokeForWriters((w) => w.WriteLine());
        }

        /// <inheriteddoc />
        public override void WriteLine(bool value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(char value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(char[] buffer)
        {
            InvokeForWriters((w, b) => w.WriteLine(b),
                             buffer);
        }

        /// <inheriteddoc />
        public override void WriteLine(char[] buffer, int index, int count)
        {
            InvokeForWriters((w, s) => w.WriteLine(s.Buffer, s.Index, s.Count),
                             new
                             {
                                 Buffer = buffer,
                                 Index = index,
                                 Count = count,
                             });
        }

        /// <inheriteddoc />
        public override void WriteLine(int value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(uint value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(long value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(ulong value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(float value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(double value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(decimal value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(string value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(object value)
        {
            InvokeForWriters((w, v) => w.WriteLine(v),
                             value);
        }

        /// <inheriteddoc />
        public override void WriteLine(string format, object arg0)
        {
            InvokeForWriters((w, s) => w.WriteLine(s.Format, s.Argument0),
                             new
                             {
                                 Argument0 = arg0,
                                 Format = format,
                             });
        }

        /// <inheriteddoc />
        public override void WriteLine(string format, object arg0, object arg1)
        {
            InvokeForWriters((w, s) => w.WriteLine(s.Format, s.Argument0, s.Argument1),
                             new
                             {
                                 Argument0 = arg0,
                                 Argument1 = arg1,
                                 Format = format,
                             });
        }

        /// <inheriteddoc />
        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            InvokeForWriters((w, s) => w.WriteLine(s.Format, s.Argument0, s.Argument1, s.Argument2),
                             new
                             {
                                 Argument0 = arg0,
                                 Argument1 = arg1,
                                 Argument2 = arg2,
                                 Format = format,
                             });
        }

        /// <inheriteddoc />
        public override void WriteLine(string format, params object[] arg)
        {
            InvokeForWriters((w, s) => w.WriteLine(s.Format, s.Arguments),
                             new
                             {
                                 Arguments = arg,
                                 Format = format,
                             });
        }

        #endregion Methods (46)

        #region Properties (4)

        /// <inheriteddoc />
        public override Encoding Encoding
        {
            get { return InvokeForWriters(w => w.Encoding).Distinct().SingleOrDefault() ?? Encoding.UTF8; }
        }

        /// <inheriteddoc />
        public override IFormatProvider FormatProvider
        {
            get { return InvokeForWriters(w => w.FormatProvider).Distinct().SingleOrDefault() ?? base.FormatProvider; }
        }

        /// <inheriteddoc />
        public override string NewLine
        {
            get { return InvokeForWriters(w => w.NewLine).Distinct().SingleOrDefault() ?? base.NewLine; }

            set
            {
                InvokeForWriters((w, v) => w.NewLine = v,
                                 value);
            }
        }

        /// <summary>
        /// Gets the object for thread safe operations.
        /// </summary>
        public object SyncRoot
        {
            get { return _SYNC_ROOT; }
        }

        #endregion Properties (4)
    }
}