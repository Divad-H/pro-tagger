using System.Windows.Controls;
using System.Windows.Input;

namespace ProTagger.Wpf
{
    class MouseWheelIgnoringScrollViewer : ScrollViewer
    {
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {}
    }
}
