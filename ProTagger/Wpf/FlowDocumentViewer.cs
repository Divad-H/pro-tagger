using System.Windows.Controls;
using System.Windows.Input;

namespace ProTagger.Wpf
{
    class FlowDocumentViewer : FlowDocumentScrollViewer
    {
        public FlowDocumentViewer()
            => VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {}
    }
}
