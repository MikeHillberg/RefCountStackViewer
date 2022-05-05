using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

// ba w 4 address  ".echo;dd address l1;k 1000;g"


namespace RefCountStackViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.ShowDialog();

            if (string.IsNullOrEmpty(ofd.FileName))
                return;

            var reader = File.OpenText(ofd.FileName);

            //var filename = @"e:\dumps\people3\trace_c.txt";
            //var reader = new StreamReader(filename);

            //var reader = new StringReader(SampleData.DumpOutput);

            IsOpen = Visibility.Collapsed;

            RefCountUpdates = RefCountUpdate.Parse(reader);
            if (RefCountUpdates != null)
            {
                TotalRefCountUpdates = RefCountUpdates.Count;
                IsOpen = Visibility.Visible;
            }
        }

        public ObservableCollection<RefCountInfo> RefCountUpdates
        {
            get { return (ObservableCollection<RefCountInfo>)GetValue(RefCountUpdatesProperty); }
            set { SetValue(RefCountUpdatesProperty, value); }
        }
        public static readonly DependencyProperty RefCountUpdatesProperty =
            DependencyProperty.Register("RefCountUpdates", typeof(ObservableCollection<RefCountInfo>), typeof(MainWindow), new PropertyMetadata(null));

        public int TotalRefCountUpdates
        {
            get { return (int)GetValue(TotalRefCountUpdatesProperty); }
            set { SetValue(TotalRefCountUpdatesProperty, value); }
        }
        public static readonly DependencyProperty TotalRefCountUpdatesProperty =
            DependencyProperty.Register("TotalRefCountUpdates", typeof(int), typeof(MainWindow), new PropertyMetadata(0));



        public Visibility StackFramesListDetailVisibility
        {
            get { return (Visibility)GetValue(StackFramesListDetailVisibilityProperty); }
            set { SetValue(StackFramesListDetailVisibilityProperty, value); }
        }
        public static readonly DependencyProperty StackFramesListDetailVisibilityProperty =
            DependencyProperty.Register("StackFramesListDetailVisibility", typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Visible));


        public Visibility StackGroupDetailVisibility
        {
            get { return (Visibility)GetValue(StackGroupDetailVisibilityProperty); }
            set { SetValue(StackGroupDetailVisibilityProperty, value); }
        }
        public static readonly DependencyProperty StackGroupDetailVisibilityProperty =
            DependencyProperty.Register("StackGroupDetailVisibility", typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Collapsed));


        public Visibility IsOpen
        {
            get { return (Visibility)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Collapsed));





        object _masterSelectedItem = null;
        public object MasterSelectedItem
        {
            get { return _masterSelectedItem; }
            set
            {
                _masterSelectedItem = value;

                StackFramesListDetailVisibility = Visibility.Collapsed;
                StackGroupDetailVisibility = Visibility.Collapsed;

                DetailRoot.Visibility = Visibility.Visible;
                if (value == null)
                {
                    Detail = null;
                    DetailRoot.Visibility = Visibility.Collapsed;
                }
                else if (value is RefCountUpdate)
                {
                    Detail = (value as RefCountUpdate);
                    StackFramesListDetailVisibility = Visibility.Visible;
                }
                else
                {
                    Detail = value;
                    StackGroupDetailVisibility = Visibility.Visible;
                }
            }
        }


        public object Detail
        {
            get { return (object)GetValue(DetailProperty); }
            set { SetValue(DetailProperty, value); }
        }
        public static readonly DependencyProperty DetailProperty =
            DependencyProperty.Register("Detail", typeof(object), typeof(MainWindow), new PropertyMetadata(null));



        private void FindEnd(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("");
        }

        //private void ExpandCollapse_Click(object sender, RoutedEventArgs e)
        //{
        //    if (StackFramesListDetailVisibility == Visibility.Visible)
        //        Collapse();
        //    else
        //        Expand();
        //}

        private void Expand()
        {
            var refCountGroup = Master.SelectedItem as RefCountGroup;
            if (refCountGroup == null)
                return;

            var owner = refCountGroup.Owner;
            var index = owner.IndexOf(refCountGroup);

            owner.RemoveAt(index);
            foreach (var refCountUpdate in refCountGroup.RefCountUpdates)
            {
                owner.Insert(index++, refCountUpdate);
                refCountUpdate.Owner = owner;
            }

        }

        private void Collapse(RefCountInfo item, StackFrame collapseStackFrame)
        {
            int lineNumber = -1;
            try
            {

                var toCollapse = new ObservableCollection<RefCountInfo>();

                var commonStackFramesFound = false;
                var commonStackFrames = new List<StackFrame>()
                {
                    new StackFrame() { Method = "..." }
                };

                var selectedIndex = item.Owner.IndexOf(item);
                var masterIndex = selectedIndex;

                for (; ; masterIndex--)
                {
                    if (masterIndex < 0)
                    {
                        masterIndex = 0;
                        break;
                    }

                    var found = false;
                    var refCountInfo = item.Owner[masterIndex] as RefCountInfo;

                    lineNumber = refCountInfo.LineNumber;

                    foreach (var stackFrame in refCountInfo.StackFrames)
                    {
                        if (StackFrame.Matches(collapseStackFrame, stackFrame))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Debug.Assert(masterIndex <= selectedIndex);
                        masterIndex++;
                        break;
                    }
                }

                var startIndex = masterIndex;

                for (; masterIndex < item.Owner.Count; masterIndex++)
                {
                    var found = false;

                    var refCountInfo = item.Owner[masterIndex] as RefCountInfo;
                    lineNumber = refCountInfo.LineNumber;
                    foreach (var stackFrame in refCountInfo.StackFrames) // bugbug
                    {
                        if (!found)
                        {
                            if (StackFrame.Matches(collapseStackFrame, stackFrame))
                            {
                                toCollapse.Add(refCountInfo);
                                found = true;

                                if (!commonStackFramesFound)
                                    commonStackFrames.Add(stackFrame);
                            }
                        }
                        else if (!commonStackFramesFound)
                        {
                            commonStackFrames.Add(stackFrame);
                        }
                    }

                    if (!found)
                        break;

                    commonStackFramesFound = true;
                }

                if (toCollapse.Count == 0)
                    return;


                var refCountGroup = new RefCountGroup()
                {
                    RefCountUpdates = toCollapse,
                    RefCount = toCollapse[toCollapse.Count - 1].RefCount,
                    TimeTravelPosition = toCollapse[toCollapse.Count - 1].TimeTravelPosition,
                    Description = collapseStackFrame.Method,
                    StackFrames = commonStackFrames,
                    Owner = item.Owner
                };

                for (int i = 0; i < toCollapse.Count; i++)
                {
                    item.Owner.RemoveAt(startIndex);
                }

                item.Owner.Insert(startIndex, refCountGroup);

                foreach (var tc in toCollapse)
                {
                    tc.Owner = refCountGroup.RefCountUpdates;
                }
            }
            catch (Exception)
            {
                MessageBox.Show($"Failed on the stack starting at line {lineNumber}");
                return;
            }


        }

        private StackFrame GetCurrentlySelectedStackFrame()
        {
            StackFrame selectedStackFrame;
            if (StackFramesListDetailVisibility == Visibility.Visible)
            {
                var selectedStackFrames = _stackFramesList.ItemsSource as List<StackFrame>;

                if (_stackFramesList.SelectedIndex == -1)
                    return null;

                selectedStackFrame = selectedStackFrames[_stackFramesList.SelectedIndex];
            }
            else
            {
                var selectedStackFrames = _stackGroupList.ItemsSource as List<StackFrame>;

                if (_stackGroupList.SelectedIndex == -1)
                    return null;

                selectedStackFrame = selectedStackFrames[_stackGroupList.SelectedIndex];
            }

            return selectedStackFrame;
        }

        private void CollapseEvery(object sender, RoutedEventArgs e)
        {
            var selectedStackFrame = GetCurrentlySelectedStackFrame();
            if (selectedStackFrame == null)
                return;

            var method = selectedStackFrame.Method;
            Debug.Assert(method == method.Trim());

            int lineNumber = -1;

                try
                {
                    int index = 0;
                    for (index = 0; index < Master.Items.Count; index++)
                    {
                        var found = false;
                        var refCountInfo = Master.Items[index] as RefCountInfo;
                        lineNumber = refCountInfo.LineNumber;
                        for (var stackFrameIndex = 0; stackFrameIndex < refCountInfo.StackFrames.Count; stackFrameIndex++)
                        {
                            var stackFrame = refCountInfo.StackFrames[stackFrameIndex];

                            // Skip the "..." at the front of a group
                            if (stackFrameIndex == 0 && refCountInfo is RefCountGroup)
                            {
                                Debug.Assert(stackFrame.Method == "...");
                                continue;
                            }

                            // Skip the first frame or we'll go into an infinite loop
                            else if (stackFrameIndex == 1 && refCountInfo is RefCountGroup)
                            {
                                continue;
                            }

                            if (stackFrame.Method == method)
                            {
                                found = true;
                                Collapse(refCountInfo, stackFrame);
                                break;
                            }
                        }

                        //if (found)
                        //    break;
                    }

                }
                catch (Exception)
                {
                    MessageBox.Show($"Failed on the stack starting at line {lineNumber}");
                    return;
                }


        }

        private void Collapse(object sender, RoutedEventArgs e)
        {
            var collapseStackFrame = GetCurrentlySelectedStackFrame();
            if (collapseStackFrame == null)
                return;

            Collapse(Master.SelectedItem as RefCountInfo, collapseStackFrame);
        }
        private void Expand(object sender, RoutedEventArgs e)
        {
            Expand();
        }

        private void Master_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            MasterSelectedItem = Master.SelectedItem;
        }
    }
}
