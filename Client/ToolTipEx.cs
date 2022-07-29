using System;
using System.Drawing;
using System.Windows.Forms;

namespace ExampleClient
{
    /// <summary>
    /// Immediately shows a tooltip at the bottom of a control when mouse hovers the control.
    /// Tooltip is always shown even if the control is disabled. Tooltip is hidden when mouse
    /// leaves the control.
    /// </summary>
    internal class ToolTipEx : IDisposable
    {
        private bool _disposedValue;
        private Control _parentControl;
        private Control _controlWithToolTip;
        private ToolTip _toolTip;
        private bool _toolTipShowing;
        private string _toolTipText;

        /// <summary>
        /// Creates an instance of <see cref="ToolTipEx"/>
        /// </summary>
        /// <param name="parentControl">Parent of the control with tooltip</param>
        /// <param name="controlWithToolTip">The control with the tooltip to show</param>
        /// <param name="toolTipText">The text of the tooltip</param>
        public ToolTipEx(Control parentControl, Control controlWithToolTip, string toolTipText)
        {
            _toolTip = new ToolTip();
            _toolTipText = toolTipText;

            _parentControl = parentControl;
            _parentControl.MouseMove += OnParentControlMouseMove;

            _controlWithToolTip = controlWithToolTip;
        }

        private void OnParentControlMouseMove(object sender, MouseEventArgs e)
        {
            var location = new Point(e.X, e.Y);
            if (_parentControl.GetChildAtPoint(location) == _controlWithToolTip)
            {
                if (!_toolTipShowing)
                {
                    _toolTipShowing = true;

                    // Shows the tooltip below the onwer control
                    _toolTip.Show(_toolTipText, _controlWithToolTip, 0, _controlWithToolTip.Height);
                }
            }
            else
            {
                _toolTipShowing = false;
                _toolTip.Hide(_controlWithToolTip);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _parentControl.MouseMove -= OnParentControlMouseMove;
                    _parentControl = null;

                    _controlWithToolTip = null;

                    _toolTip.Dispose();
                    _toolTip = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
