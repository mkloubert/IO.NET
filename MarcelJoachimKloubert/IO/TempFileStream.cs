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

namespace MarcelJoachimKloubert.IO
{
    /// <summary>
    /// Handles a temporary file that is deleted when its closed.
    /// </summary>
    public class TempFileStream : FileStream
    {
        #region Constructors (1)

        /// <summary>
        /// Initializes a new instance of the <see cref="TempFileStream" /> class.
        /// </summary>
        /// <param name="options">The options to use.</param>
        /// <param name="share">Defines how to share the file with other applications.</param>
        /// <param name="bufferSize">The buffer size to use.</param>
        public TempFileStream(FileOptions options = FileOptions.None, FileShare share = FileShare.None, int bufferSize = 4096)
            : base(path: Path.GetTempFileName(),
                   mode: FileMode.Open, access: FileAccess.ReadWrite,
                   options: options, share: share, bufferSize: bufferSize)
        {
        }

        #endregion Constructors (1)

        #region Methods (3)

        /// <inheriteddoc />
        public override void Close()
        {
            base.Close();

            DeleteMe(new FileInfo(Name));
        }

        /// <summary>
        /// Deletes that file.
        /// </summary>
        /// <param name="file">The object that describes the current file.</param>
        protected virtual void DeleteMe(FileInfo file)
        {
            if (!file.Exists)
            {
                return;
            }

            try
            {
                file.Delete();
            }
            finally
            {
                file.Refresh();
            }
        }

        /// <inheriteddoc />
        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);

                DeleteMe(new FileInfo(Name));
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
}