using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using IronPython.Runtime.Exceptions;
using Microsoft.Scripting.Hosting;

namespace PyRevitBaseClasses
{
    /// <summary>
    /// A stream to write output to...
    /// This can be passed into the python interpreter to render all output to.
    /// Only a minimal subset is actually implemented - this is all we really
    /// expect to use.
    /// </summary>
    public class ScriptOutputStream: Stream
    {
        private readonly ScriptOutput _gui;
        private readonly ScriptEngine _engine;
        private int _bomCharsLeft; // we want to get rid of pesky UTF8-BOM-Chars on write
        private readonly Queue<MemoryStream> _completedLines; // one memorystream per line of input
        private MemoryStream _inputBuffer;
        private readonly string _err_msg_html_element;
        private readonly string _default_element;

        public ScriptOutputStream(ScriptOutput gui, ScriptEngine engine)
        {
            _gui = gui;
            _engine = engine;

            _gui.txtStdOut.Focus();

            _completedLines = new Queue<MemoryStream>();
            _inputBuffer = new MemoryStream();

            _bomCharsLeft = 3; //0xef, 0xbb, 0xbf for UTF-8 (see http://en.wikipedia.org/wiki/Byte_order_mark#Representations_of_byte_order_marks_by_encoding)

            var config = new ExternalConfig();
            _default_element = config.defaultelement;
            _err_msg_html_element = config.errordiv;

        }

        public void WriteError(string error_msg)
        {
            var err_div = _gui.txtStdOut.Document.CreateElement(_err_msg_html_element);
            err_div.InnerHtml = error_msg.Replace("\n", "<br/>");

            Write(Encoding.ASCII.GetBytes(" " + err_div.OuterHtml), 0, error_msg.Length);
        }

        /// <summary>
        /// Append the text in the buffer to gui.txtStdOut
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (this)
            {
                if (_gui.IsDisposed)
                {
                    return;
                }

                if (!_gui.Visible)
                {
                    _gui.Show();
                }

                while (_bomCharsLeft > 0 && count > 0)
                {
                    _bomCharsLeft--;
                    count--;
                    offset++;
                }

                var actualBuffer = new byte[count];
                Array.Copy(buffer, offset, actualBuffer, 0, count);
                var text = Encoding.UTF8.GetString(actualBuffer);
                //Debug.WriteLine(text);
                _gui.BeginInvoke((Action)delegate()
                {
                    var div = _gui.txtStdOut.Document.CreateElement(_default_element);
                    if (text.EndsWith("\n"))
                        text = text.Remove(text.Length - 1);
                    text = text.Replace("\n", "<br/>");
                    div.InnerHtml = text;
                    _gui.txtStdOut.Document.Body.AppendChild(div);
                });
                Application.DoEvents();
            }
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read from the _inputBuffer, block until a new line has been entered...
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            while (_completedLines.Count < 1)
            {
                if (_gui.Visible == false)
                {
                    throw new EndOfStreamException();
                }
                // wait for user to complete a line
                Application.DoEvents();
                Thread.Sleep(10);
            }
            var line = _completedLines.Dequeue();
            return line.Read(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return !_gui.IsDisposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return _gui.txtStdOut.DocumentText.Length; }
        }

        public override long Position
        {
            get { return 0; }
            set { }
        }
    }
}
