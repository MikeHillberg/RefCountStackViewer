using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RefCountStackViewer
{
    /// <summary>
    /// Interaction logic for StackFrameView.xaml
    /// </summary>
    public partial class StackFrameView : UserControl
    {
        public StackFrameView()
        {
            InitializeComponent();
            _root.DataContext = this;
        }

        public StackFrame StackFrame
        {
            get { return (StackFrame)GetValue(StackFrameProperty); }
            set { SetValue(StackFrameProperty, value); }
        }
        public static readonly DependencyProperty StackFrameProperty =
            DependencyProperty.Register("StackFrame", typeof(StackFrame), typeof(StackFrameView), new PropertyMetadata(null));


    }
}
